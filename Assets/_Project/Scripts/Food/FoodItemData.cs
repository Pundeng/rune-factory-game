using System;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Food
{
    public enum FoodItemKind
    {
        RawIngredient,
        ProcessedFood
    }

    [Serializable]
    public sealed class FoodItemData : ITransportItem, IEquatable<FoodItemData>
    {
        [SerializeField] private string id;
        [SerializeField] private FoodItemKind kind;

        public FoodItemData(string id, FoodItemKind kind)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("A food item ID is required.", nameof(id));
            }

            if (!Enum.IsDefined(typeof(FoodItemKind), kind))
            {
                throw new ArgumentOutOfRangeException(nameof(kind), kind, null);
            }

            this.id = id;
            this.kind = kind;
        }

        public string Id => id;

        public FoodItemKind Kind => kind;

        public bool Equals(FoodItemData other)
        {
            return other != null && id == other.id && kind == other.kind;
        }

        public override bool Equals(object obj)
        {
            return obj is FoodItemData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(id, kind);
        }

        public override string ToString()
        {
            return id;
        }
    }
}
