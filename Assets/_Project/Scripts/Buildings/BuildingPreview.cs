using FantasyShapez.Grid;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    public sealed class BuildingPreview : MonoBehaviour
    {
        [SerializeField] private Color validColor = new(0.25f, 0.9f, 0.45f, 0.55f);
        [SerializeField] private Color invalidColor = new(0.95f, 0.25f, 0.25f, 0.55f);

        private BuildingDefinition currentDefinition;
        private GameObject directionIndicator;
        private GameObject visualRoot;

        public void Show(
            BuildingDefinition definition,
            GridSystem gridSystem,
            Vector2Int anchorCell,
            BuildingRotation rotation,
            bool isValid)
        {
            EnsureVisual(definition, gridSystem.CellSize);
            EnsureDirectionIndicator(gridSystem.CellSize);

            Vector2Int rotatedFootprint = definition.GetRotatedFootprint(rotation);
            Vector3 firstCellCenter = gridSystem.GridToWorld(anchorCell);
            transform.position = firstCellCenter + new Vector3(
                (rotatedFootprint.x - 1) * gridSystem.CellSize * 0.5f,
                (rotatedFootprint.y - 1) * gridSystem.CellSize * 0.5f,
                -0.02f);
            transform.rotation = Quaternion.Euler(0f, 0f, -(int)rotation);
            BuildingVisualFactory.Tint(visualRoot, isValid ? validColor : invalidColor);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void EnsureVisual(BuildingDefinition definition, float cellSize)
        {
            if (visualRoot != null && ReferenceEquals(currentDefinition, definition))
            {
                return;
            }

            if (visualRoot != null)
            {
                Destroy(visualRoot);
            }

            currentDefinition = definition;
            visualRoot = BuildingVisualFactory.Create(definition, transform, cellSize, 75);
        }

        private void EnsureDirectionIndicator(float cellSize)
        {
            if (directionIndicator != null)
            {
                return;
            }

            directionIndicator = new GameObject("Direction Indicator");
            directionIndicator.transform.SetParent(transform, false);
            CreateIndicatorPart(
                "Shaft",
                new Vector2(0f, 0.12f) * cellSize,
                new Vector2(0.09f, 0.42f) * cellSize,
                0f);
            CreateIndicatorPart(
                "Left",
                new Vector2(-0.1f, 0.29f) * cellSize,
                new Vector2(0.09f, 0.26f) * cellSize,
                -45f);
            CreateIndicatorPart(
                "Right",
                new Vector2(0.1f, 0.29f) * cellSize,
                new Vector2(0.09f, 0.26f) * cellSize,
                45f);
        }

        private void CreateIndicatorPart(
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            float angle)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(directionIndicator.transform, false);
            part.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.03f);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            part.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = BuildingVisualFactory.PlaceholderSprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 80;
        }
    }
}
