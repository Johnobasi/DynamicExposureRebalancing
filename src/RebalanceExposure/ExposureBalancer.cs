namespace RebalanceExposure
{
    public class ExposureBalancer
    {
        private List<Entity> Entities;
        public ExposureBalancer(List<Entity> entities)
        {
            Entities = entities;
        }
        public void Rebalance()
        {
            decimal totalExposure = GetTotalExposure();

            // Find entities with excess exposure
            var excessEntities = Entities
                .Where(e => e.Exposure > e.Capacity)
                .Select(e => new { Entity = e, Excess = e.Exposure - e.Capacity })
                .ToList();

            // Rebalance each entity with excess exposure
            excessEntities.ForEach(e => {
                e.Entity.Exposure = e.Entity.Capacity; 
                RedistributeExcess(e.Excess, e.Entity.EntityId);
            });

            // Validate total exposure remains unchanged after distrubution
            if (GetTotalExposure() != totalExposure)
            {
                throw new InvalidOperationException("Total exposure mismatch after rebalancing.");
            }
        }

        /// <summary>
        /// Method attempts to allocate excess
        /// </summary>
        /// <param name="excessExposure"></param>
        /// <param name="sourceEntityId"></param>
        private void RedistributeExcess(decimal excessExposure, string sourceEntityId)
        {
            // Use a priority queue for entities with capacity
            var entitiesWithCapacity = new SortedSet<Entity>(
                Entities.Where(e => e.EntityId != sourceEntityId && e.Exposure < e.Capacity),
                Comparer<Entity>.Create((a, b) => b.Priority.CompareTo(a.Priority))
            );

            while (excessExposure > 0 && entitiesWithCapacity.Count > 0)
            {
                var entity = entitiesWithCapacity.First();
                entitiesWithCapacity.Remove(entity);

                decimal availableCapacity = entity.Capacity - entity.Exposure;
                decimal allocation = Math.Min(excessExposure, availableCapacity);

                entity.Exposure += allocation;
                excessExposure -= allocation;

                if (entity.Exposure < entity.Capacity)
                {
                    entitiesWithCapacity.Add(entity);
                }
            }

            if (excessExposure > 0)
            {
                throw new Exception($"Unable to redistribute {excessExposure} excess exposure.All entities are at capacity.");
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
