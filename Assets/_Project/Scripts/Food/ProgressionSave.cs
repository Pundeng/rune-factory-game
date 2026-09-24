using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FantasyShapez.Buildings;
using UnityEngine;

namespace FantasyShapez.Food
{
    [Serializable]
    public sealed class ProgressionSaveData
    {
        public int version = 1;
        public long currency;
        public SavedDelivery[] deliveries;
        public string[] completedOrderIds;
        public string activeOrderId;
        public SavedDelivery[] activeProgress;
        public SavedUnlock[] unlocks;
        public string[] purchasedCropIds;
        public SavedRecipe[] discoveries;
    }

    [Serializable]
    public sealed class SavedDelivery
    {
        public string id;
        public FoodItemKind kind;
        public int count;
    }

    [Serializable]
    public sealed class SavedUnlock
    {
        public string category;
        public string id;
    }

    [Serializable]
    public sealed class SavedRecipe
    {
        public DiscoveredRecipeKind kind;
        public string ingredientAId;
        public FoodItemKind ingredientAKind;
        public string ingredientBId;
        public FoodItemKind ingredientBKind;
        public CookingProperty property;
        public string outputId;
        public FoodItemKind outputKind;
    }

    // Saves progression only. Placed buildings, their contents, and transport stay in the scene.
    public sealed class ProgressionSaveService
    {
        private readonly MarketInventory inventory;
        private readonly FoodOrderSequence orders;
        private readonly UnlockState unlocks;
        private readonly SeedShop shop;
        private readonly RegionState regions;
        private readonly RecipeDiscoveryRegistry discoveries;
        private readonly IReadOnlyList<ProcessingRecipe> processorRecipes;
        private readonly IReadOnlyList<MixingRecipe> mixerRecipes;

        public ProgressionSaveService(Market market, BuildingPlacementController buildings)
            : this(market?.Inventory, market?.OrderSequence, market?.Unlocks,
                market?.SeedShop, market?.Regions, buildings?.RecipeDiscoveries,
                buildings?.ProcessorRecipes, buildings?.MixerRecipes)
        {
        }

        public ProgressionSaveService(MarketInventory inventory, FoodOrderSequence orders,
            UnlockState unlocks, SeedShop shop, RegionState regions,
            RecipeDiscoveryRegistry discoveries,
            IReadOnlyList<ProcessingRecipe> processorRecipes,
            IReadOnlyList<MixingRecipe> mixerRecipes)
        {
            this.inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            this.orders = orders ?? throw new ArgumentNullException(nameof(orders));
            this.unlocks = unlocks ?? throw new ArgumentNullException(nameof(unlocks));
            this.shop = shop ?? throw new ArgumentNullException(nameof(shop));
            this.regions = regions ?? throw new ArgumentNullException(nameof(regions));
            this.discoveries = discoveries ?? throw new ArgumentNullException(nameof(discoveries));
            this.processorRecipes = processorRecipes ??
                throw new ArgumentNullException(nameof(processorRecipes));
            this.mixerRecipes = mixerRecipes ?? throw new ArgumentNullException(nameof(mixerRecipes));
        }

        public static string DefaultPath =>
            Path.Combine(Application.persistentDataPath, "cozy-food-factory-progress.json");

        public string ToJson() => JsonUtility.ToJson(Capture(), true);

        public bool TryLoadJson(string json, out string error)
        {
            ProgressionSaveData data;
            try
            {
                data = JsonUtility.FromJson<ProgressionSaveData>(json);
                ApplyValidated(data);
                error = null;
                return true;
            }
            catch (Exception exception) when (exception is ArgumentException or
                InvalidOperationException or NullReferenceException)
            {
                error = exception.Message;
                return false;
            }
        }

        public bool TrySave(string path, out string error)
        {
            string temporaryPath = path + ".tmp";
            try
            {
                File.WriteAllText(temporaryPath, ToJson(), new UTF8Encoding(false));
                if (File.Exists(path))
                {
                    File.Replace(temporaryPath, path, null);
                }
                else
                {
                    File.Move(temporaryPath, path);
                }

                error = null;
                return true;
            }
            catch (Exception exception) when (exception is IOException or
                UnauthorizedAccessException or ArgumentException)
            {
                error = exception.Message;
                return false;
            }
            finally
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
        }

        public bool TryLoad(string path, out string error)
        {
            try
            {
                if (!File.Exists(path))
                {
                    error = "No progression save exists at the displayed path.";
                    return false;
                }

                return TryLoadJson(File.ReadAllText(path), out error);
            }
            catch (Exception exception) when (exception is IOException or
                UnauthorizedAccessException or ArgumentException)
            {
                error = exception.Message;
                return false;
            }
        }

        private ProgressionSaveData Capture()
        {
            FoodOrderProgress active = orders.ActiveOrder;
            return new ProgressionSaveData
            {
                currency = inventory.Currency,
                deliveries = inventory.DeliveredCounts.Select(entry => new SavedDelivery
                {
                    id = entry.Key.Id, kind = entry.Key.Kind, count = entry.Value
                }).ToArray(),
                completedOrderIds = orders.CompletedOrders.Select(order => order.Id).ToArray(),
                activeOrderId = active?.Order.Id,
                activeProgress = active == null ? Array.Empty<SavedDelivery>() :
                    active.Order.Requirements.Select(requirement => new SavedDelivery
                    {
                        id = requirement.Food.Id,
                        kind = requirement.Food.Kind,
                        count = active.GetDeliveredCount(requirement)
                    }).ToArray(),
                unlocks = unlocks.Unlocked.Select(key => new SavedUnlock
                {
                    category = key.Category, id = key.Id
                }).ToArray(),
                purchasedCropIds = shop.PurchasedCropIds.ToArray(),
                discoveries = discoveries.DiscoveredRecipes.Select(recipe => new SavedRecipe
                {
                    kind = recipe.Kind,
                    ingredientAId = recipe.IngredientA.Id,
                    ingredientAKind = recipe.IngredientA.Kind,
                    ingredientBId = recipe.IngredientB?.Id,
                    ingredientBKind = recipe.IngredientB?.Kind ?? default,
                    property = recipe.Property ?? default,
                    outputId = recipe.Output.Id,
                    outputKind = recipe.Output.Kind
                }).ToArray()
            };
        }

        private void ApplyValidated(ProgressionSaveData data)
        {
            if (data == null || data.version != 1 || data.currency < 0 ||
                data.deliveries == null || data.completedOrderIds == null ||
                data.activeProgress == null || data.unlocks == null ||
                data.purchasedCropIds == null || data.discoveries == null)
            {
                throw new ArgumentException("Missing or unsupported progression save data.");
            }

            var deliveries = new Dictionary<FoodItemData, int>();
            foreach (SavedDelivery saved in data.deliveries)
            {
                FoodItemData food = ReadFood(saved?.id, saved?.kind ?? (FoodItemKind)(-1));
                if (saved.count <= 0 || !deliveries.TryAdd(food, saved.count))
                {
                    throw new ArgumentException("Invalid saved delivery counts.");
                }
            }

            long total = deliveries.Values.Sum(count => (long)count);
            if (total > int.MaxValue)
            {
                throw new ArgumentException("Too many saved deliveries.");
            }

            IReadOnlyList<FoodOrder> authoredOrders = orders.Orders;
            if (data.completedOrderIds.Length > authoredOrders.Count)
            {
                throw new ArgumentException("Unknown completed order.");
            }

            for (int index = 0; index < data.completedOrderIds.Length; index++)
            {
                if (data.completedOrderIds[index] != authoredOrders[index].Id)
                {
                    throw new ArgumentException("Completed orders are out of sequence.");
                }
            }

            int completedCount = data.completedOrderIds.Length;
            bool hasActive = completedCount < authoredOrders.Count;
            if (hasActive && data.activeOrderId != authoredOrders[completedCount].Id ||
                !hasActive && (!string.IsNullOrEmpty(data.activeOrderId) ||
                    data.activeProgress.Length != 0))
            {
                throw new ArgumentException("Invalid active order ID.");
            }

            Dictionary<FoodItemData, int> progress = null;
            if (hasActive)
            {
                progress = new Dictionary<FoodItemData, int>();
                FoodOrder active = authoredOrders[completedCount];
                if (data.activeProgress.Length != active.Requirements.Count)
                {
                    throw new ArgumentException("Incomplete saved order progress.");
                }

                foreach (SavedDelivery saved in data.activeProgress)
                {
                    FoodItemData food = ReadFood(saved?.id, saved?.kind ?? (FoodItemKind)(-1));
                    if (!progress.TryAdd(food, saved.count))
                    {
                        throw new ArgumentException("Duplicate order progress entry.");
                    }
                }

                foreach (FoodOrderRequirement requirement in active.Requirements)
                {
                    if (!progress.TryGetValue(requirement.Food, out int count) ||
                        count < 0 || count >= requirement.Quantity ||
                        count > (deliveries.TryGetValue(requirement.Food, out int delivered)
                            ? delivered : 0))
                    {
                        throw new ArgumentException("Invalid saved order progress.");
                    }
                }
            }

            var unlocks = new List<UnlockKey>();
            var uniqueUnlocks = new HashSet<UnlockKey>();
            foreach (SavedUnlock saved in data.unlocks)
            {
                var key = new UnlockKey(saved?.category, saved?.id);
                if (!uniqueUnlocks.Add(key))
                {
                    throw new ArgumentException("Duplicate saved unlock.");
                }

                unlocks.Add(key);
            }

            foreach (FoodOrder order in authoredOrders.Take(completedCount))
            {
                if (order.Unlocks.Any(key => !uniqueUnlocks.Contains(key)))
                {
                    throw new ArgumentException("A completed order is missing its reward.");
                }
            }

            var purchases = new HashSet<string>(StringComparer.Ordinal);
            foreach (string cropId in data.purchasedCropIds)
            {
                if (string.IsNullOrWhiteSpace(cropId) || !purchases.Add(cropId) ||
                    !shop.Offers.Any(offer => offer.CropId == cropId) ||
                    !uniqueUnlocks.Contains(new UnlockKey(UnlockKey.CropCategory, cropId)))
                {
                    throw new ArgumentException("Invalid saved Seed Shop purchase.");
                }
            }

            foreach (FarmableRegion region in regions.Regions)
            {
                var restored = new UnlockKey(UnlockKey.RegionCategory, region.Id);
                if (region.InitiallyRestored && !uniqueUnlocks.Contains(restored) ||
                    uniqueUnlocks.Contains(restored) && region.HasRequirement &&
                    !uniqueUnlocks.Contains(new UnlockKey(region.RequiredUnlockCategory,
                        region.RequiredUnlockId)))
                {
                    throw new ArgumentException("Invalid saved region state.");
                }
            }

            var discoveries = new List<DiscoveredRecipe>();
            var uniqueRecipes = new HashSet<DiscoveredRecipe>();
            foreach (SavedRecipe saved in data.discoveries)
            {
                DiscoveredRecipe recipe = ResolveRecipe(saved);
                if (recipe == null || !uniqueRecipes.Add(recipe))
                {
                    throw new ArgumentException("Unknown or duplicate saved discovery.");
                }

                discoveries.Add(recipe);
            }

            // All inputs are checked before changing the live session.
            inventory.Restore(data.currency, deliveries);
            this.unlocks.Restore(unlocks);
            shop.RestorePurchases(purchases);
            orders.Restore(completedCount, progress);
            this.discoveries.Restore(discoveries);
        }

        private DiscoveredRecipe ResolveRecipe(SavedRecipe saved)
        {
            if (saved == null || string.IsNullOrWhiteSpace(saved.ingredientAId) ||
                string.IsNullOrWhiteSpace(saved.outputId) ||
                !Enum.IsDefined(typeof(FoodItemKind), saved.ingredientAKind) ||
                !Enum.IsDefined(typeof(FoodItemKind), saved.outputKind))
            {
                return null;
            }

            if (saved.kind == DiscoveredRecipeKind.Processing)
            {
                ProcessingRecipe match = processorRecipes.FirstOrDefault(recipe =>
                    recipe.Input?.Id == saved.ingredientAId &&
                    recipe.Input.Kind == saved.ingredientAKind &&
                    recipe.Output?.Id == saved.outputId &&
                    recipe.Output.Kind == saved.outputKind &&
                    recipe.Property == saved.property);
                return match == null ? null : new DiscoveredRecipe(match);
            }

            if (saved.kind == DiscoveredRecipeKind.Mixing &&
                !string.IsNullOrWhiteSpace(saved.ingredientBId) &&
                Enum.IsDefined(typeof(FoodItemKind), saved.ingredientBKind))
            {
                MixingRecipe match = mixerRecipes.FirstOrDefault(recipe =>
                    recipe.Output?.Id == saved.outputId &&
                    recipe.Output.Kind == saved.outputKind &&
                    (recipe.IngredientA?.Id == saved.ingredientAId &&
                     recipe.IngredientA.Kind == saved.ingredientAKind &&
                     recipe.IngredientB?.Id == saved.ingredientBId &&
                     recipe.IngredientB.Kind == saved.ingredientBKind ||
                     recipe.IngredientA?.Id == saved.ingredientBId &&
                     recipe.IngredientA.Kind == saved.ingredientBKind &&
                     recipe.IngredientB?.Id == saved.ingredientAId &&
                     recipe.IngredientB.Kind == saved.ingredientAKind));
                return match == null ? null : new DiscoveredRecipe(match);
            }

            return null;
        }

        private static FoodItemData ReadFood(string id, FoodItemKind kind)
        {
            if (string.IsNullOrWhiteSpace(id) ||
                !Enum.IsDefined(typeof(FoodItemKind), kind))
            {
                throw new ArgumentException("Invalid saved food ID.");
            }

            return new FoodItemData(id, kind);
        }
    }
}
