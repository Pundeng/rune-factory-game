using System;
using System.Linq;
using FantasyShapez.Buildings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FantasyShapez.Food
{
    public sealed class MarketPanel : MonoBehaviour
    {
        [SerializeField] private Market market = null;
        [SerializeField] private BuildingPlacementController buildings = null;
        private Vector2 scrollPosition;
        private ProgressionSaveService saves;
        private string saveMessage;
        private string SavePath => buildings != null && buildings.IsFoodDemo
            ? ProgressionSaveService.DemoPath : ProgressionSaveService.DefaultPath;

        public bool IsPointerOverPanel
        {
            get
            {
                if (!isActiveAndEnabled || market == null || market.Inventory == null ||
                    Mouse.current == null)
                {
                    return false;
                }

                Vector2 pointer = Mouse.current.position.ReadValue();
                pointer.y = Screen.height - pointer.y;
                return GetPanelRect().Contains(pointer);
            }
        }

        private void OnGUI()
        {
            if (market == null || market.Inventory == null)
            {
                return;
            }

            Rect rect = GetPanelRect();
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 8f,
                rect.width - 24f, rect.height - 16f));
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.Label("Market");
            if (buildings != null && buildings.IsFoodDemo)
            {
                var guidanceStyle = new GUIStyle(GUI.skin.label) { wordWrap = true };
                GUILayout.Label(market.Unlocks.IsUnlocked("demo", "complete")
                    ? "DEMO COMPLETE — your food factory is running!"
                    : GetDemoGuidance(), guidanceStyle);
            }
            GUILayout.Label($"Total delivered: {market.Inventory.TotalDelivered}");
            GUILayout.Label($"Regular currency: {market.Currency}");
            if (!string.IsNullOrEmpty(market.LastDeliveryMessage))
            {
                GUILayout.Label(market.LastDeliveryMessage);
            }

            foreach (var delivered in market.Inventory.DeliveredCounts
                .OrderBy(entry => entry.Key.Kind)
                .ThenBy(entry => entry.Key.Id, StringComparer.Ordinal))
            {
                string kind = delivered.Key.Kind == FoodItemKind.RawIngredient
                    ? "raw" : "processed";
                GUILayout.Label($"{delivered.Key.Id} ({kind}): {delivered.Value}");
            }

            FoodOrderProgress order = market.ActiveOrder;
            if (market.Orders.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label($"Orders complete: {market.CompletedOrders.Count} / {market.Orders.Count}");
            }

            foreach (FoodOrder completed in market.CompletedOrders)
            {
                GUILayout.Label($"Completed: {completed.DisplayName}");
                foreach (UnlockKey unlock in completed.Unlocks)
                {
                    GUILayout.Label($"Unlocked {unlock.Category}: {unlock.Id}");
                }
            }

            if (order != null)
            {
                GUILayout.Label($"Order: {order.Order.DisplayName}");
                foreach (FoodOrderRequirement requirement in order.Order.Requirements)
                {
                    GUILayout.Label($"{requirement.Food.Id}: " +
                        $"{order.GetDeliveredCount(requirement)} / {requirement.Quantity}");
                }
            }
            else if (market.Orders.Count > 0)
            {
                GUILayout.Label("All orders complete.");
            }

            SeedShop shop = market.SeedShop;
            if (shop != null && shop.Offers.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Seed Shop");
                foreach (SeedShopOffer offer in shop.Offers)
                {
                    SeedShopOfferState state = shop.GetState(offer.CropId);
                    switch (state)
                    {
                        case SeedShopOfferState.Locked:
                            GUILayout.Label($"{offer.DisplayName}: locked " +
                                $"(requires {offer.RequiredUnlock.Category}: {offer.RequiredUnlock.Id})");
                            break;
                        case SeedShopOfferState.Available:
                            GUILayout.Label($"{offer.DisplayName}: available, " +
                                $"need {offer.Price} currency");
                            break;
                        case SeedShopOfferState.Affordable:
                            if (GUILayout.Button($"Affordable: buy {offer.DisplayName} " +
                                    $"({offer.Price} currency)"))
                            {
                                shop.TryPurchase(offer.CropId);
                            }
                            break;
                        case SeedShopOfferState.Purchased:
                            GUILayout.Label($"{offer.DisplayName}: purchased");
                            break;
                        case SeedShopOfferState.AlreadyUnlocked:
                            GUILayout.Label($"{offer.DisplayName}: already unlocked");
                            break;
                    }
                }
            }

            RegionState regions = market.Regions;
            if (regions != null && regions.Regions.Count > 0)
            {
                GUILayout.Space(6f);
                GUILayout.Label("Farmable Regions");
                foreach (FarmableRegion region in regions.Regions)
                {
                    string area = $"({region.MinimumCell.x}.." +
                        $"{region.MinimumCell.x + region.Size.x - 1}, " +
                        $"{region.MinimumCell.y}.." +
                        $"{region.MinimumCell.y + region.Size.y - 1})";
                    RegionStatus status = regions.GetStatus(region.Id);
                    switch (status)
                    {
                        case RegionStatus.Locked:
                            GUILayout.Label($"{region.DisplayName} {area}: locked " +
                                $"(requires {region.RequiredUnlockCategory}: " +
                                $"{region.RequiredUnlockId})");
                            break;
                        case RegionStatus.Restorable:
                            if (GUILayout.Button($"Restore {region.DisplayName} {area}"))
                            {
                                regions.TryRestore(region.Id);
                            }
                            break;
                        case RegionStatus.Restored:
                            GUILayout.Label($"{region.DisplayName} {area}: restored");
                            break;
                    }
                }
            }

            if (buildings != null)
            {
                saves ??= new ProgressionSaveService(market, buildings);
                GUILayout.Space(6f);
                GUILayout.Label("Factory Save");
                GUILayout.Label(SavePath);
                if (GUILayout.Button(buildings.IsFoodDemo ? "Save factory" :
                        "Save progression"))
                {
                    saveMessage = saves.TrySave(SavePath, out string error) ?
                        "Factory saved." : $"Save failed: {error}";
                }

                if (GUILayout.Button(buildings.IsFoodDemo ? "Load factory" :
                        "Load progression"))
                {
                    if (!saves.TryReadValidated(SavePath,
                            out ProgressionSaveData data, out string error))
                    {
                        saveMessage = $"Load failed: {error}";
                    }
                    else if (data.version == 1)
                    {
                        saveMessage = saves.TryLoad(SavePath,
                            out error) ? "Version 1 progression loaded." :
                            $"Load failed: {error}";
                    }
                    else
                    {
                        saveMessage = FactoryWorldLoadSession.TryBegin(data, out error) ?
                            "Reconstructing factory..." : $"Load failed: {error}";
                    }
                }

                string displayedMessage = saveMessage ?? FactoryWorldLoadSession.LastMessage;
                if (!string.IsNullOrEmpty(displayedMessage))
                {
                    GUILayout.Label(displayedMessage);
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private string GetDemoGuidance()
        {
            string orderId = market.ActiveOrder?.Order.Id;
            return orderId switch
            {
                "First Harvest" => "Start: place a Farm Plot on Starter Fields, " +
                    "choose Apple, cover it with a Harvester, and belt apples to Market. " +
                    "Use Construction buttons, R to rotate, Esc to exit build mode.",
                "Dried Apples" => "Build a Processor. In its default rotation, " +
                    "belt apples in from the west and collect Air with a Collector " +
                    "and Pipe to the south property port. Belt its north output to Market.",
                "Vegetable Base" => "Grow Tomato and Onion. Feed separate belts " +
                    "to the Mixer's two west inputs in default rotation, then belt " +
                    "its east output to Market.",
                "Cut Potatoes" when market.Regions.GetStatus("East Field") !=
                    RegionStatus.Restored => "Restore East Field in Market to unlock Potato.",
                "Cut Potatoes" => "Grow Potato in East Field. In default Cutter " +
                    "rotation, feed its rear from the south and connect a westbound " +
                    "and eastbound belt to its front sides. One potato makes two cuts.",
                "French Fries" => "Pipe Heat into a Processor and feed it Cut Potato. " +
                    "Belt French Fries to Market to complete the demo.",
                _ => "Build a food production line and deliver its output to Market."
            };
        }

        private Rect GetPanelRect()
        {
            int rowCount = market.Inventory.DeliveredCounts.Count;
            FoodOrderProgress order = market.ActiveOrder;
            int completedLineCount = market.CompletedOrders.Sum(completed =>
                1 + completed.Unlocks.Count);
            float contentHeight = 136f + rowCount * 22f + completedLineCount * 22f +
                (order == null ? 22f : 22f + order.Order.Requirements.Count * 22f);
            if (market.SeedShop?.Offers.Count > 0)
            {
                contentHeight += 28f + market.SeedShop.Offers.Count * 28f;
            }
            if (market.Regions?.Regions.Count > 0)
            {
                contentHeight += 28f + market.Regions.Regions.Count * 28f;
            }
            if (buildings != null)
            {
                contentHeight += 130f;
            }
            float height = Mathf.Min(contentHeight, Mathf.Max(16f, Screen.height * 0.5f));
            return new Rect(16f, Mathf.Max(16f, Screen.height - height - 16f),
                300f, height);
        }
    }
}
