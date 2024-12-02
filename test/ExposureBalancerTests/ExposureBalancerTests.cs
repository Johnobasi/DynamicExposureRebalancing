using RebalanceExposure;

namespace DynamicExposure.Tests
{
    public class ExposureBalancerTests
    {
        [Fact]
        public void ExposureBalancer_NoConstraintViolations()
        {
            var entities = new List<Entity>
            {
                new Entity("A", 9, 5, 1),
                new Entity("B", 16, 10,2),
                new Entity("C", 10, 10,3),
                new Entity("D", 5,  10, 4),
                new Entity("E", 10, 15,5),
            };

            var balancer = new ExposureBalancer(entities);
            balancer.Rebalance();

            Assert.True(balancer.IsValid());
            Assert.Equal(50, balancer.GetTotalExposure());
        }

        [Fact]
        public void ExposureBalancer_ExcessExposureRedistribution()
        {
            var entities = new List<Entity>
            {
                new Entity("1", 150, 100, 1),
                new Entity("2", 30, 50, 2),
                new Entity("3", 20, 50, 3)
            };

            var balancer = new ExposureBalancer(entities);
            balancer.Rebalance();

            Assert.True(balancer.IsValid());
            Assert.Equal(200, balancer.GetTotalExposure());
            Assert.Equal(50, balancer.NeededExposureCount);
        }

        [Fact]
        public void ExposureBalancer_RedistributionNotFeasible()
        {
            var entities = new List<Entity>
            {
                new Entity("1", 150, 100, 1),
                new Entity("2", 30, 30, 2),
                new Entity("3", 20, 20, 3)
            };

            var balancer = new ExposureBalancer(entities);

            var exception = Assert.Throws<Exception>(() => balancer.Rebalance());
            Assert.Equal("Unable to redistribute excess exposure.All entities are at capacity.", exception.Message);
        }
    }
}
