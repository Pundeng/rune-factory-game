using System;
using System.Collections.Generic;

namespace FantasyShapez.Food
{
    public sealed class MarketInventory
    {
        private readonly Dictionary<FoodItemData, int> deliveredCounts = new();

        public IReadOnlyDictionary<FoodItemData, int> DeliveredCounts => deliveredCounts;

        public int TotalDelivered { get; private set; }

        public long Currency { get; private set; }

        public int GetDeliveredCount(FoodItemData food)
        {
            if (food == null)
            {
                throw new ArgumentNullException(nameof(food));
            }

            return deliveredCounts.TryGetValue(food, out int count) ? count : 0;
        }

        public int RecordDelivery(FoodItemData food)
        {
            if (food == null)
            {
                throw new ArgumentNullException(nameof(food));
            }

            if (!food.IsValid)
            {
                throw new ArgumentException("The delivered food is invalid.", nameof(food));
            }

            int count = GetDeliveredCount(food) + 1;
            deliveredCounts[food] = count;
            TotalDelivered++;
            Currency += food.SellValue;
            return count;
        }
    }
}
