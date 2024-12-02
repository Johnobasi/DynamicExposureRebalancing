using FluentAssertions;
using RebalanceExposure;

public class ExposureBalancerTests
{
    [Fact]
    public void ExposureBalancer_NoConstraintViolations()
    {
        // Arrange
        var entities = new List<Entity>
        {
            new Entity("A", 40, 50, 1),
            new Entity("B", 30, 60, 2),
            new Entity("C", 20, 40, 3),
            new Entity("D", 10, 20, 4)
        };

        // Act
        var balancer = new ExposureBalancer(entities);
        balancer.Rebalance();

        // Assert
        balancer.IsValid().Should().BeTrue("No constraint violations expected.");
    }

    [Fact]
    public void ExposureBalancer_ExcessExposureRedistribution()
    {
        // Arrange
        var entities = new List<Entity>
        {
            new Entity("A", 80, 50, 1), // Exceeds capacity
            new Entity("B", 30, 60, 2),
            new Entity("C", 20, 40, 3),
            new Entity("D", 10, 20, 4)  // Should absorb excess first
        };

        // Act
        var balancer = new ExposureBalancer(entities);
        balancer.Rebalance();

        // Assert
        balancer.IsValid().Should().BeTrue("Entities should be valid after redistribution.");
        entities.Find(e => e.EntityId == "A").Exposure.Should().Be(50); // Capped at capacity
        entities.Find(e => e.EntityId == "D").Exposure.Should().Be(20); // D absorbs 10
        entities.Find(e => e.EntityId == "C").Exposure.Should().Be(40); // C absorbs 20
        entities.Find(e => e.EntityId == "B").Exposure.Should().Be(30); // B remains the same
    }

    [Fact]
    public void ExposureBalancer_RedistributionNotFeasible()
    {
        // Arrange
        var entities = new List<Entity>
        {
            new Entity("A", 60, 50, 1), // Exceeds capacity
            new Entity("B", 60, 60, 2), // At capacity
            new Entity("C", 40, 40, 3), // At capacity
            new Entity("D", 20, 20, 4)  // At capacity
        };

        ////
        // Act
        var balancer = new ExposureBalancer(entities);
        var exception = Assert.Throws<Exception>(() => balancer.Rebalance());

        // Assert
        exception.Message.Should().Be("Unable to redistribute 10 excess exposure.All entities are at capacity.");
        entities[0].Exposure.Should().Be(50);
        entities[1].Exposure.Should().Be(60);
        entities[2].Exposure.Should().Be(40);
        entities[3].Exposure.Should().Be(20);
    }

    [Fact]
    public void ExposureBalancer_ZeroExcessRedistribution()
    {
        // Arrange
        var entities = new List<Entity>
        {
            new Entity("A", 50, 50, 1), // At capacity
            new Entity("B", 0, 60, 2),  // No exposure
            new Entity("C", 20, 40, 3),
            new Entity("D", 10, 20, 4)
        };

        // Act
        var balancer = new ExposureBalancer(entities);
        balancer.Rebalance();

        // Assert
        balancer.IsValid().Should().BeTrue("Entities should be valid after redistribution.");
        entities.Find(e => e.EntityId == "A").Exposure.Should().Be(50); // A remains at capacity
        entities.Find(e => e.EntityId == "B").Exposure.Should().Be(0);  // B remains unaffected
    }
}
