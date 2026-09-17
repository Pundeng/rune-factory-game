using FantasyShapez.Grid;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    public sealed class BuildingPreview : MonoBehaviour
    {
        [SerializeField] private Color validColor = new(0.25f, 0.9f, 0.45f, 0.55f);
        [SerializeField] private Color invalidColor = new(0.95f, 0.25f, 0.25f, 0.55f);

        private BuildingDefinition currentDefinition;
        private GameObject visualRoot;

        public void Show(
            BuildingDefinition definition,
            GridSystem gridSystem,
            Vector2Int anchorCell,
            BuildingRotation rotation,
            bool isValid)
        {
            EnsureVisual(definition, gridSystem.CellSize);

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
    }
}
