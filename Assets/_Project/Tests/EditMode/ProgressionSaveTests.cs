using System;
using System.IO;
using System.Reflection;
using FantasyShapez.Food;
using FantasyShapez.Logistics;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class ProgressionSaveTests
    {
        private sealed class Session : IDisposable
        {
            public readonly FoodItemData Apple =
                new("apple", FoodItemKind.RawIngredient);
            public readonly FoodItemData DriedApple =
                new("Dried Apple", FoodItemKind.ProcessedFood);
            public readonly MarketInventory Inventory = new();
            public readonly UnlockState Unlocks = new();
            public readonly RecipeDiscoveryRegistry Discoveries = new();
            public readonly MarketReceiver Receiver;
            public readonly FoodOrderSequence Orders;
            public readonly SeedShop Shop;
            public readonly RegionState Regions;
            public readonly ProcessingRecipe Recipe;
            public readonly ProgressionSaveService Saves;

            public Session()
            {
                Receiver = new MarketReceiver(Vector2Int.zero, Inventory);
                Orders = new FoodOrderSequence(new[]
                {
                    new FoodOrder("apple-order", "Apple Order",
                        new[] { new FoodOrderRequirement(Apple, 2) },
                        new[]
                        {
                            new UnlockKey(UnlockKey.CropCategory, "Onion"),
                            new UnlockKey(UnlockKey.SeedShopCategory, "Basil"),
                            new UnlockKey(UnlockKey.RegionAccessCategory, "East")
                        }),
                    new FoodOrder("dried-order", "Dried Order",
                        new[] { new FoodOrderRequirement(DriedApple, 2) },
                        new[] { new UnlockKey("machine", "Oven") })
                }, Receiver, Unlocks);
                Shop = new SeedShop(new[]
                {
                    new SeedShopOffer("Basil", "Basil Seeds", 1,
                        new UnlockKey(UnlockKey.SeedShopCategory, "Basil"))
                }, Inventory, Unlocks);
                Regions = new RegionState(new[]
                {
                    new FarmableRegion("Starter", "Starter", Vector2Int.zero,
                        Vector2Int.one, true),
                    new FarmableRegion("East", "East", Vector2Int.right,
                        Vector2Int.one, false,
                        new UnlockKey(UnlockKey.RegionAccessCategory, "East"))
                }, Unlocks);
                Recipe = new ProcessingRecipe(Apple, CookingProperty.Air, DriedApple);
                Saves = new ProgressionSaveService(Inventory, Orders, Unlocks, Shop, Regions,
                    Discoveries, new[] { Recipe }, Array.Empty<MixingRecipe>());
            }

            public void Dispose() => Orders.Dispose();
        }

        [Test]
        public void RoundTrip_RestoresOrderProgressPurchasesRegionsAndDiscoveries()
        {
            using var source = new Session();
            source.Receiver.TryAcceptItem(source.Apple, GridDirection.East);
            source.Receiver.TryAcceptItem(source.Apple, GridDirection.East);
            Assert.That(source.Shop.TryPurchase("Basil"), Is.True);
            Assert.That(source.Regions.TryRestore("East"), Is.True);
            Assert.That(source.Discoveries.Record(source.Recipe), Is.True);
            source.Receiver.TryAcceptItem(source.DriedApple, GridDirection.East);

            string json = source.Saves.ToJson();
            using var restored = new Session();
            Assert.That(restored.Saves.TryLoadJson(json, out string error), Is.True, error);
            Assert.That(restored.Inventory.Currency, Is.EqualTo(2));
            Assert.That(restored.Inventory.TotalDelivered, Is.EqualTo(3));
            Assert.That(restored.Orders.CompletedOrders, Has.Count.EqualTo(1));
            Assert.That(restored.Orders.ActiveOrder.Order.Id, Is.EqualTo("dried-order"));
            Assert.That(restored.Orders.ActiveOrder.GetDeliveredCount(
                restored.Orders.ActiveOrder.Order.Requirements[0]), Is.EqualTo(1));
            Assert.That(restored.Unlocks.IsUnlocked(UnlockKey.CropCategory, "Onion"), Is.True);
            Assert.That(restored.Shop.GetState("Basil"), Is.EqualTo(SeedShopOfferState.Purchased));
            Assert.That(restored.Regions.GetStatus("East"), Is.EqualTo(RegionStatus.Restored));
            Assert.That(restored.Discoveries.DiscoveredRecipes, Has.Count.EqualTo(1));
            Assert.That(restored.Discoveries.Record(restored.Recipe), Is.False);

            int unlockCount = restored.Unlocks.Unlocked.Count;
            restored.Receiver.TryAcceptItem(restored.DriedApple, GridDirection.East);
            Assert.That(restored.Orders.CompletedOrders, Has.Count.EqualTo(2));
            Assert.That(restored.Unlocks.Unlocked.Count, Is.EqualTo(unlockCount + 1));
            restored.Receiver.TryAcceptItem(restored.DriedApple, GridDirection.East);
            Assert.That(restored.Unlocks.Unlocked.Count, Is.EqualTo(unlockCount + 1));
        }

        [Test]
        public void InvalidOrMissingSave_LeavesLiveProgressionUntouched()
        {
            using var session = new Session();
            session.Receiver.TryAcceptItem(session.Apple, GridDirection.East);
            string json = session.Saves.ToJson();
            var invalid = JsonUtility.FromJson<ProgressionSaveData>(json);
            invalid.activeProgress[0].count = 2;
            Assert.That(session.Saves.TryLoadJson(JsonUtility.ToJson(invalid), out _), Is.False);
            Assert.That(session.Saves.TryLoadJson("{}", out _), Is.False);
            Assert.That(session.Saves.TryLoadJson("not json", out _), Is.False);
            Assert.That(session.Saves.TryLoad(Path.Combine(Application.temporaryCachePath,
                $"missing-progression-{Guid.NewGuid():N}.json"), out _), Is.False);
            Assert.That(session.Orders.CompletedOrders, Is.Empty);
            Assert.That(session.Orders.ActiveOrder.GetDeliveredCount(
                session.Orders.ActiveOrder.Order.Requirements[0]), Is.EqualTo(1));
            Assert.That(session.Inventory.Currency, Is.EqualTo(1));
            Assert.That(session.Unlocks.IsUnlocked(UnlockKey.CropCategory, "Onion"), Is.False);
        }

        [Test]
        public void LoadingEarlierUnlockState_ClearsLockedCropOnExistingPlot()
        {
            using var session = new Session();
            string earlierSave = session.Saves.ToJson();
            session.Unlocks.Grant(new UnlockKey(UnlockKey.CropCategory, "Onion"));
            var plotObject = new GameObject("Saved progression plot");
            try
            {
                var plot = plotObject.AddComponent<FarmPlot>();
                var onion = new CropDefinition("Onion",
                    new FoodItemData("onion", FoodItemKind.RawIngredient), 2f, "Onion");
                typeof(FarmPlot).GetField("availableCrops",
                    BindingFlags.Instance | BindingFlags.NonPublic)
                    ?.SetValue(plot, new[] { onion });
                plot.Initialize(new Vector2Int(1300, 1300), session.Unlocks);
                plot.SelectCrop(onion);

                Assert.That(session.Saves.TryLoadJson(earlierSave, out string error),
                    Is.True, error);
                Assert.That(plot.IsCropUnlocked(onion), Is.False);
                Assert.That(plot.SelectedCrop, Is.Null);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(plotObject);
            }
        }
    }
}
