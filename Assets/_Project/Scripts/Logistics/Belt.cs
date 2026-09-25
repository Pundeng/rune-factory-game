using FantasyShapez.Buildings;
using FantasyShapez.Grid;
using FantasyShapez.Food;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class Belt : MonoBehaviour, IBuildingRemovalRule, IBuildingMoveState
    {
        [SerializeField] private GridDirection direction;
        [SerializeField] private bool hasItem;
        [SerializeField, Range(0f, 1f)] private float itemProgress;
        [SerializeField] private string runeDebug;

        private static Sprite placeholderSprite;
        private BeltTransportCoordinator coordinator;
        private BeltCell cell;
        private GameObject itemVisual;

        // Rebuilding intentionally discards any item currently carried by this Belt.
        public bool CanRemove => true;

        public bool CanMove => cell != null && !cell.HasItem;

        public SavedBelt CaptureWorldState()
        {
            if (cell == null)
            {
                throw new System.InvalidOperationException("Belt is not initialized.");
            }

            TransportedRune carried = cell.Item;
            return new SavedBelt
            {
                item = SavedFood.FromTransport(carried?.Item),
                entryDirection = carried?.EntryDirection ?? default,
                progress = carried?.Progress ?? 0f
            };
        }

        public void RestoreWorldState(SavedBelt saved)
        {
            if (saved.item != null)
            {
                cell.RestoreItem(saved.item.ToFood(), saved.entryDirection,
                    saved.progress);
            }
        }

        public void DetachForMove()
        {
            coordinator?.UnregisterBelt(cell);
        }

        public void ReattachAfterFailedMove()
        {
            cell = coordinator.RegisterBelt(this, cell.Cell, direction);
        }

        public void Initialize(
            Vector2Int gridCell,
            GridDirection beltDirection,
            BeltTransportCoordinator transportCoordinator)
        {
            coordinator = transportCoordinator;
            direction = beltDirection;
            cell = coordinator.RegisterBelt(this, gridCell, beltDirection);
            CreateDirectionArrow();
            CreateItemVisual();
        }

        public void RefreshItemVisual(GridSystem gridSystem, BeltCell beltCell)
        {
            hasItem = beltCell.HasItem;
            itemProgress = beltCell.Item?.Progress ?? 0f;
            runeDebug = beltCell.Item?.Item?.ToString() ?? string.Empty;

            itemVisual.SetActive(hasItem);

            if (!hasItem)
            {
                return;
            }

            Vector2 startOffset = -(Vector2)beltCell.Item.EntryDirection.ToOffset() * 0.5f;
            Vector2 endOffset = (Vector2)beltCell.Direction.ToOffset() * 0.5f;
            Vector2 localOffset = itemProgress < 0.5f
                ? Vector2.Lerp(startOffset, Vector2.zero, itemProgress * 2f)
                : Vector2.Lerp(Vector2.zero, endOffset, (itemProgress - 0.5f) * 2f);
            itemVisual.transform.position =
                gridSystem.GridToWorld(beltCell.Cell) + (Vector3)(localOffset * gridSystem.CellSize);
        }

        private void CreateDirectionArrow()
        {
            CreateArrowPart("Arrow Shaft", new Vector2(0f, -0.02f), new Vector2(0.1f, 0.5f), 0f);
            CreateArrowPart("Arrow Left", new Vector2(-0.1f, 0.18f), new Vector2(0.1f, 0.3f), -45f);
            CreateArrowPart("Arrow Right", new Vector2(0.1f, 0.18f), new Vector2(0.1f, 0.3f), 45f);
        }

        private void CreateArrowPart(string partName, Vector2 position, Vector2 scale, float angle)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(position.x, position.y, -0.02f);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 15;
        }

        private void CreateItemVisual()
        {
            itemVisual = new GameObject("Transported Item");
            itemVisual.transform.SetParent(transform, false);
            itemVisual.transform.localScale = Vector3.one * 0.22f;
            SpriteRenderer renderer = itemVisual.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = new Color(0.9f, 0.35f, 1f, 1f);
            renderer.sortingOrder = 25;
            itemVisual.SetActive(false);
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (placeholderSprite == null)
            {
                placeholderSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                placeholderSprite.name = "Runtime Belt Placeholder";
            }

            return placeholderSprite;
        }

        private void OnDestroy()
        {
            coordinator?.UnregisterBelt(cell);
        }
    }
}
