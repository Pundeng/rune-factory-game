using System;
using System.Reflection;
using FantasyShapez.Food;
using FantasyShapez.Logistics;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class FoodOrderTests
    {
        private static readonly FoodItemData Apple =
            new("apple", FoodItemKind.RawIngredient);
        private static readonly FoodItemData DriedApple =
            new("Dried Apple", FoodItemKind.ProcessedFood);

        [Test]
        public void Order_TracksOnlyDeliveriesAfterActivationAndUnlocksOnce()
        {
            var receiver = new MarketReceiver(Vector2Int.zero, new MarketInventory());
            Assert.That(receiver.TryAcceptItem(Apple, GridDirection.East), Is.True);
            var appleRequirement = new FoodOrderRequirement(Apple, 2);
            var driedRequirement = new FoodOrderRequirement(DriedApple, 1);
            var order = new FoodOrder("first", "First Order",
                new[] { appleRequirement, driedRequirement }, "Onion");
            using var progress = new FoodOrderProgress(order, receiver);
            int completions = 0;
            progress.Completed += _ => completions++;

            Assert.That(progress.GetDeliveredCount(appleRequirement), Is.Zero);
            Assert.That(progress.IsComplete, Is.False);
            Assert.That(progress.UnlockedContentId, Is.Null);
            Assert.That(receiver.TryAcceptItem(Apple, GridDirection.East), Is.True);
            Assert.That(progress.GetDeliveredCount(appleRequirement), Is.EqualTo(1));
            Assert.That(receiver.TryAcceptItem(DriedApple, GridDirection.East), Is.True);
            Assert.That(progress.GetDeliveredCount(driedRequirement), Is.EqualTo(1));
            Assert.That(progress.IsComplete, Is.False);

            Assert.That(receiver.TryAcceptItem(Apple, GridDirection.East), Is.True);
            Assert.That(progress.GetDeliveredCount(appleRequirement), Is.EqualTo(2));
            Assert.That(progress.IsComplete, Is.True);
            Assert.That(progress.UnlockedContentId, Is.EqualTo("Onion"));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(receiver.TryAcceptItem(Apple, GridDirection.East), Is.True);
            Assert.That(progress.GetDeliveredCount(appleRequirement), Is.EqualTo(2));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(receiver.Inventory.GetDeliveredCount(Apple), Is.EqualTo(4));
            Assert.That(receiver.Inventory.Currency, Is.EqualTo(5));
        }

        [Test]
        public void OnionCrop_BecomesSelectableAfterOrderCompletion()
        {
            var receiver = new MarketReceiver(Vector2Int.zero, new MarketInventory());
            var order = new FoodOrder("onion", "Unlock Onion",
                new[] { new FoodOrderRequirement(Apple, 1) }, "Onion");
            using var progress = new FoodOrderProgress(order, receiver);
            var apple = new CropDefinition("Apple", Apple, 2f);
            var onion = new CropDefinition("Onion",
                new FoodItemData("onion", FoodItemKind.RawIngredient), 2f, "Onion");
            var plotObject = new GameObject("Order gated plot");
            try
            {
                var plot = plotObject.AddComponent<FarmPlot>();
                typeof(FarmPlot).GetField("availableCrops",
                    BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.SetValue(plot, new[] { apple, onion });
                plot.Initialize(new Vector2Int(1234, 5678), progress);

                Assert.That(plot.IsCropUnlocked(apple), Is.True);
                Assert.That(plot.IsCropUnlocked(onion), Is.False);
                Assert.Throws<InvalidOperationException>(() => plot.SelectCrop(onion));
                Assert.That(receiver.TryAcceptItem(Apple, GridDirection.East), Is.True);
                Assert.That(plot.IsCropUnlocked(onion), Is.True);
                Assert.DoesNotThrow(() => plot.SelectCrop(onion));
                Assert.That(plot.SelectedCrop, Is.SameAs(onion));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(plotObject);
            }
        }

        [Test]
        public void Order_RejectsDuplicateFoodRequirements()
        {
            Assert.Throws<InvalidOperationException>(() => new FoodOrder("duplicate",
                "Duplicate", new[]
                {
                    new FoodOrderRequirement(Apple, 1),
                    new FoodOrderRequirement(new FoodItemData("apple",
                        FoodItemKind.RawIngredient), 2)
                }, "Onion"));
        }
    }
}
