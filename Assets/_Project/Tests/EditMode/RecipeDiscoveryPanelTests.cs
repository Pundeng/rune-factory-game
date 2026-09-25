using FantasyShapez.Food;
using FantasyShapez.UI;
using NUnit.Framework;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class RecipeDiscoveryPanelTests
    {
        [Test]
        public void FirstDiscoveryShowsPopupAndBookUpdatesWithoutRepeatPopup()
        {
            var registry = new RecipeDiscoveryRegistry();
            var popups = new RecipeDiscoveryPopupQueue();
            registry.Discovered += popups.Show;
            var apple = new FoodItemData("apple", FoodItemKind.RawIngredient);
            var driedApple = new FoodItemData("Dried Apple", FoodItemKind.ProcessedFood);
            var recipe = new ProcessingRecipe(apple, CookingProperty.Air, driedApple);

            Assert.That(registry.Record(recipe), Is.True);
            Assert.That(popups.Current.Output, Is.EqualTo(driedApple));
            Assert.That(RecipeDiscoveryPanel.FormatRequirements(popups.Current),
                Is.EqualTo("Ingredient: apple; Property: Air"));
            Assert.That(registry.DiscoveredRecipes.Count, Is.EqualTo(1));

            popups.Dismiss();
            Assert.That(registry.Record(new ProcessingRecipe(apple,
                CookingProperty.Air, driedApple)), Is.False);
            Assert.That(popups.Current, Is.Null);
            Assert.That(registry.DiscoveredRecipes.Count, Is.EqualTo(1));

            var tomato = new FoodItemData("tomato", FoodItemKind.RawIngredient);
            var onion = new FoodItemData("onion", FoodItemKind.RawIngredient);
            var vegetableBase = new FoodItemData("Vegetable Base",
                FoodItemKind.ProcessedFood);
            Assert.That(registry.Record(new MixingRecipe(tomato, onion, vegetableBase)),
                Is.True);
            Assert.That(popups.Current.Output, Is.EqualTo(vegetableBase));
            Assert.That(RecipeDiscoveryPanel.FormatRequirements(popups.Current),
                Is.EqualTo("Ingredients: tomato + onion"));
            Assert.That(registry.DiscoveredRecipes.Count, Is.EqualTo(2));
        }

        [Test]
        public void DiscoveriesQueueUntilPlayerDismissesEachPopup()
        {
            var registry = new RecipeDiscoveryRegistry();
            var popups = new RecipeDiscoveryPopupQueue();
            registry.Discovered += popups.Show;
            var apple = new FoodItemData("apple", FoodItemKind.RawIngredient);
            var first = new FoodItemData("Dried Apple", FoodItemKind.ProcessedFood);
            var second = new FoodItemData("Baked Apple", FoodItemKind.ProcessedFood);

            registry.Record(new ProcessingRecipe(apple, CookingProperty.Air, first));
            registry.Record(new ProcessingRecipe(apple, CookingProperty.Heat, second));
            Assert.That(popups.Current.Output, Is.EqualTo(first));
            popups.Dismiss();
            Assert.That(popups.Current.Output, Is.EqualTo(second));
            popups.Dismiss();
            Assert.That(popups.Current, Is.Null);
        }
    }
}
