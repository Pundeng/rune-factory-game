using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Food
{
    public sealed class Market : MonoBehaviour
    {
        [SerializeField] private BeltTransportCoordinator transportCoordinator = null;
        [SerializeField] private Vector2Int inputCell = new(10, 4);
        [SerializeField] private string lastDeliveryDebug = string.Empty;
        [SerializeField] private FoodOrder firstOrder = null;

        private MarketReceiver receiver;
        private FoodOrderProgress orderProgress;

        public Vector2Int InputCell => inputCell;

        public Vector2Int Footprint => Vector2Int.one;

        public MarketInventory Inventory => receiver?.Inventory;

        public long Currency => Inventory?.Currency ?? 0;

        public FoodOrderProgress ActiveOrder => orderProgress;

        public bool IsUnlocked(string contentId) =>
            !string.IsNullOrEmpty(contentId) &&
            orderProgress?.UnlockedContentId == contentId;

        public string LastDeliveryMessage => lastDeliveryDebug;

        private void Awake()
        {
            if (transportCoordinator == null)
            {
                throw new MissingReferenceException("The Market requires a Belt Transport Coordinator.");
            }

            receiver = new MarketReceiver(inputCell, new MarketInventory());
            receiver.FoodDelivered += HandleFoodDelivered;
            if (firstOrder != null)
            {
                orderProgress = new FoodOrderProgress(firstOrder, receiver);
            }
            transportCoordinator.RegisterInputReceiver(receiver);
            CreatePlaceholderVisual();
        }

        private void HandleFoodDelivered(FoodItemData food, int count)
        {
            string kind = food.Kind == FoodItemKind.RawIngredient ? "raw" : "processed";
            lastDeliveryDebug =
                $"Delivered {food.Id} ({kind}): {count} (+{food.SellValue} currency)";
        }

        private void OnDestroy()
        {
            if (receiver == null)
            {
                return;
            }

            receiver.FoodDelivered -= HandleFoodDelivered;
            orderProgress?.Dispose();
            transportCoordinator?.UnregisterInputReceiver(receiver);
        }

        private void CreatePlaceholderVisual()
        {
            CreateVisualPart("Market Body", Vector2.zero, new Vector2(0.85f, 0.85f),
                new Color(0.2f, 0.7f, 0.55f, 1f), 8);
            CreateVisualPart("Market Core", Vector2.zero, new Vector2(0.38f, 0.38f),
                new Color(1f, 0.85f, 0.4f, 1f), 9);
            foreach (GridDirection direction in new[]
                { GridDirection.North, GridDirection.East, GridDirection.South, GridDirection.West })
            {
                Vector2 position = -(Vector2)direction.ToOffset() * 0.42f;
                Vector2 scale = direction is GridDirection.East or GridDirection.West
                    ? new Vector2(0.12f, 0.3f)
                    : new Vector2(0.3f, 0.12f);
                CreateVisualPart($"Input {direction}", position, scale,
                    BuildingPortPreviewLayouts.InputColor, 10);
            }
        }

        private void CreateVisualPart(
            string name, Vector2 position, Vector2 scale, Color color, int sortingOrder)
        {
            var part = new GameObject(name);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(position.x, position.y, 0f);
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = BuildingVisualFactory.PlaceholderSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
