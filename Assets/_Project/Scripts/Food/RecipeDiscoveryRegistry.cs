using System;
using System.Collections.Generic;

namespace FantasyShapez.Food
{
    public enum DiscoveredRecipeKind
    {
        Processing,
        Mixing
    }

    public sealed class DiscoveredRecipe : IEquatable<DiscoveredRecipe>
    {
        public DiscoveredRecipe(ProcessingRecipe recipe)
        {
            if (recipe?.Input == null || recipe.Output == null)
            {
                throw new ArgumentException("A complete processing recipe is required.", nameof(recipe));
            }

            Kind = DiscoveredRecipeKind.Processing;
            IngredientA = recipe.Input;
            Property = recipe.Property;
            Output = recipe.Output;
        }

        public DiscoveredRecipe(MixingRecipe recipe)
        {
            if (recipe?.IngredientA == null || recipe.IngredientB == null ||
                recipe.Output == null)
            {
                throw new ArgumentException("A complete mixing recipe is required.", nameof(recipe));
            }

            Kind = DiscoveredRecipeKind.Mixing;
            IngredientA = recipe.IngredientA;
            IngredientB = recipe.IngredientB;
            Output = recipe.Output;
        }

        public DiscoveredRecipeKind Kind { get; }
        public FoodItemData IngredientA { get; }
        public FoodItemData IngredientB { get; }
        public CookingProperty? Property { get; }
        public FoodItemData Output { get; }

        public bool Equals(DiscoveredRecipe other)
        {
            if (other == null || Kind != other.Kind || !Output.Equals(other.Output))
            {
                return false;
            }

            return Kind == DiscoveredRecipeKind.Processing
                ? IngredientA.Equals(other.IngredientA) && Property == other.Property
                : (IngredientA.Equals(other.IngredientA) &&
                   IngredientB.Equals(other.IngredientB)) ||
                  (IngredientA.Equals(other.IngredientB) &&
                   IngredientB.Equals(other.IngredientA));
        }

        public override bool Equals(object obj) => obj is DiscoveredRecipe other && Equals(other);

        public override int GetHashCode() => Kind == DiscoveredRecipeKind.Processing
            ? HashCode.Combine(Kind, IngredientA, Property, Output)
            : HashCode.Combine(Kind, IngredientA.GetHashCode() ^ IngredientB.GetHashCode(), Output);
    }

    public sealed class RecipeDiscoveryRegistry
    {
        private readonly HashSet<DiscoveredRecipe> known = new();
        private readonly List<DiscoveredRecipe> discovered = new();
        private readonly IReadOnlyList<DiscoveredRecipe> readOnlyDiscovered;

        public RecipeDiscoveryRegistry()
        {
            readOnlyDiscovered = discovered.AsReadOnly();
        }

        public IReadOnlyList<DiscoveredRecipe> DiscoveredRecipes => readOnlyDiscovered;
        public event Action<DiscoveredRecipe> Discovered;

        public bool Record(ProcessingRecipe recipe) => Record(new DiscoveredRecipe(recipe));
        public bool Record(MixingRecipe recipe) => Record(new DiscoveredRecipe(recipe));

        private bool Record(DiscoveredRecipe recipe)
        {
            if (!known.Add(recipe))
            {
                return false;
            }

            discovered.Add(recipe);
            Discovered?.Invoke(recipe);
            return true;
        }
    }
}
