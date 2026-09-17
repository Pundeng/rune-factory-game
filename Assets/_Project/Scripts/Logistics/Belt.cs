using FantasyShapez.Buildings;
using FantasyShapez.Grid;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class Belt : MonoBehaviour, IBuildingRemovalRule
    {
        [SerializeField] private GridDirection direction;
        [SerializeField] private bool hasItem;
        [SerializeField, Range(0f, 1f)] private float itemProgress;

        private static Sprite placeholderSprite;
        private BeltTransportCoordinator coordinator;
        private BeltCell cell;
        private GameObject runeVisual;

        // Occupied belts cannot be removed, avoiding silent rune loss during editing.
        public bool CanRemove => cell == null || !cell.HasItem;

        public void Initialize(
            Vector2Int gridCell,
            GridDirection beltDirection,
            BeltTransportCoordinator transportCoordinator)
        {
            coordinator = transportCoordinator;
            direction = beltDirection;
            cell = coordinator.RegisterBelt(this, gridCell, beltDirection);
            CreateDirectionArrow();
            CreateRuneVisual();
        }

        public void RefreshItemVisual(GridSystem gridSystem, BeltCell beltCell)
        {
            hasItem = beltCell.HasItem;
            itemProgress = beltCell.Item?.Progress ?? 0f;
            runeVisual.SetActive(hasItem);

            if (!hasItem)
            {
                return;
            }

            Vector2 startOffset = -(Vector2)beltCell.Item.EntryDirection.ToOffset() * 0.5f;
            Vector2 endOffset = (Vector2)beltCell.Direction.ToOffset() * 0.5f;
            Vector2 localOffset = itemProgress < 0.5f
                ? Vector2.Lerp(startOffset, Vector2.zero, itemProgress * 2f)
                : Vector2.Lerp(Vector2.zero, endOffset, (itemProgress - 0.5f) * 2f);
            runeVisual.transform.position =
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

        private void CreateRuneVisual()
        {
            runeVisual = new GameObject("Transported Rune");
            runeVisual.transform.SetParent(transform, false);
            runeVisual.transform.localScale = Vector3.one * 0.22f;
            SpriteRenderer renderer = runeVisual.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = new Color(0.9f, 0.35f, 1f, 1f);
            renderer.sortingOrder = 25;
            runeVisual.SetActive(false);
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
