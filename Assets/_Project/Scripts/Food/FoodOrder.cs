using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyShapez.Food
{
    [Serializable]
    public sealed class FoodOrderRequirement
    {
        [SerializeField] private FoodItemData food;
        [SerializeField, Min(1)] private int quantity = 1;

        public FoodOrderRequirement(FoodItemData food, int quantity)
        {
            this.food = food ?? throw new ArgumentNullException(nameof(food));
            if (quantity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity));
            }

            this.quantity = quantity;
        }

        public FoodItemData Food => food;
        public int Quantity => quantity;
    }

    [Serializable]
    public sealed class FoodOrder
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private FoodOrderRequirement[] requirements;
        [SerializeField] private string unlockId;

        public FoodOrder(string id, string displayName,
            FoodOrderRequirement[] requirements, string unlockId)
        {
            this.id = id;
            this.displayName = displayName;
            this.requirements = requirements;
            this.unlockId = unlockId;
            Validate();
        }

        public string Id => id;
        public string DisplayName => displayName;
        public IReadOnlyList<FoodOrderRequirement> Requirements => requirements;
        public string UnlockId => unlockId;

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(displayName) ||
                string.IsNullOrWhiteSpace(unlockId) ||
                requirements == null || requirements.Length == 0)
            {
                throw new InvalidOperationException("The food order is incomplete.");
            }

            var unique = new HashSet<FoodItemData>();
            foreach (FoodOrderRequirement requirement in requirements)
            {
                if (requirement?.Food == null || !requirement.Food.IsValid ||
                    requirement.Quantity <= 0 || !unique.Add(requirement.Food))
                {
                    throw new InvalidOperationException("The food order has an invalid requirement.");
                }
            }
        }
    }

    public sealed class FoodOrderProgress : IDisposable
    {
        private readonly MarketReceiver receiver;
        private readonly Dictionary<FoodItemData, int> startingCounts = new();

        public FoodOrderProgress(FoodOrder order, MarketReceiver receiver)
        {
            Order = order ?? throw new ArgumentNullException(nameof(order));
            Order.Validate();
            this.receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
            foreach (FoodOrderRequirement requirement in Order.Requirements)
            {
                startingCounts.Add(requirement.Food,
                    receiver.Inventory.GetDeliveredCount(requirement.Food));
            }

            receiver.FoodDelivered += OnFoodDelivered;
        }

        public FoodOrder Order { get; }
        public bool IsComplete { get; private set; }
        public string UnlockedContentId => IsComplete ? Order.UnlockId : null;
        public event Action<FoodOrder> Completed;

        public int GetDeliveredCount(FoodOrderRequirement requirement)
        {
            if (requirement == null ||
                !startingCounts.TryGetValue(requirement.Food, out int startingCount))
            {
                throw new ArgumentException("The requirement is not in this order.",
                    nameof(requirement));
            }

            int delivered = receiver.Inventory.GetDeliveredCount(requirement.Food) -
                startingCount;
            return Math.Min(requirement.Quantity, Math.Max(0, delivered));
        }

        public void Dispose()
        {
            receiver.FoodDelivered -= OnFoodDelivered;
        }

        private void OnFoodDelivered(FoodItemData food, int count)
        {
            if (IsComplete)
            {
                return;
            }

            foreach (FoodOrderRequirement requirement in Order.Requirements)
            {
                if (GetDeliveredCount(requirement) < requirement.Quantity)
                {
                    return;
                }
            }

            IsComplete = true;
            Completed?.Invoke(Order);
        }
    }
}
