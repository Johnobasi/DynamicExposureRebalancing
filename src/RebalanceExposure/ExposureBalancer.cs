namespace RebalanceExposure
{
    public class ExposureBalancer
    {
        private List<Entity> Entities;
        public ExposureBalancer(List<Entity> entities)
        {
            Entities = entities;
        }

        public decimal ExcessExposureCount { get; set; }
        public decimal NeededExposureCount { get; set; }
        public List<Entity> ExcessExposureEntities { get; set; } = new();
        public List<Entity> NeedingExposureEntities { get; set; } = new();
        public void Rebalance()
        {
            // Save the total exposure before rebalancing for validation
            var totalExposure = GetTotalExposure();

            // Identify entities with excess exposure and calculate the total excess
            var excessEntities = Entities.Where(x => x.Exposure >= x.Capacity).ToList();
            excessEntities.ForEach(x =>
            {
                var excess = x.Exposure - x.Capacity;
                if (excess >= 0)
                {
                    // Track total excess and adjust the entity's exposure to its capacity
                    ExcessExposureCount += excess;
                    ExcessExposureEntities.Add(new Entity(x.EntityId, x.Exposure - excess, x.Capacity, x.Priority));
                }
            });

            // Calculate the total exposure needed to fill the gap in underexposed entities
            NeededExposureCount = Entities.Where(x => x.Exposure < x.Capacity).Sum(x =>
            {
                return x.Capacity - x.Exposure;
            });

            //If the total excess is greater than the total need, rebalancing is not possible
            if (ExcessExposureCount > NeededExposureCount)
            {
                throw new Exception("Unable to redistribute excess exposure.All entities are at capacity.");
            }

            // Identify entities with available capacity to absorb excess exposure
            var availableCapacity = Entities
                .Where(x => x.Exposure < x.Capacity)
                .OrderByDescending(x => x.Priority)
                .ToList();

            // Distribute the excess exposure to entities with available capacity
            availableCapacity.ForEach(x =>
            {
                var needed = x.Capacity - x.Exposure;

                if (ExcessExposureCount == 0)
                    return;

                if (ExcessExposureCount <= needed)
                {
                    NeedingExposureEntities.Add(new Entity(x.EntityId, x.Exposure + ExcessExposureCount, x.Capacity, x.Priority));
                    ExcessExposureCount = 0;
                }
                else
                {
                    // Fully satisfy the capacity of this entity and reduce the excess
                    NeedingExposureEntities.Add(new Entity(x.EntityId, x.Exposure + needed, x.Capacity, x.Priority));
                    ExcessExposureCount -= needed;
                }
            });

            // Combine the balanced entities into a new list
            var balancedEntities = ExcessExposureEntities.Concat(NeedingExposureEntities);
            Entities = new List<Entity>(balancedEntities);

            // Validate that the total exposure remains unchanged after redistribution
            if (GetTotalExposure() != totalExposure)
            {
                throw new Exception("Total exposure mismatch after rebalancing.");
            }
        }

        public decimal GetTotalExposure()
        {
            return Entities.Sum(e => e.Exposure);
        }

        public bool IsValid()
        {
            return Entities.All(e => e.Exposure <= e.Capacity);
        }
    }
}
