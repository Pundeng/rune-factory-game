using System.Collections.Generic;
using FantasyShapez.Grid;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FantasyShapez.Buildings
{
    public sealed class BuildingPlacementController : MonoBehaviour
    {
        [SerializeField] private GridSystem gridSystem = null;
        [SerializeField] private GridHoverHighlight hoverHighlight = null;
        [SerializeField] private BuildingPreview placementPreview = null;
        [SerializeField] private BuildingDefinition prototypeBuilding = new();
        [SerializeField] private MonoBehaviour placementBehavior = null;

        private readonly GridOccupancy occupancy = new();
        private readonly Dictionary<BuildingPlacement, PlacedBuilding> buildingInstances = new();
        private BuildingRotation selectedRotation;
        private bool isPlacementModeActive;

        private void Start()
        {
            placementPreview.Hide();
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null)
            {
                return;
            }

            HandleModeInput();
            HandleRemovalInput();

            if (!isPlacementModeActive)
            {
                return;
            }

            Vector2Int anchorCell = hoverHighlight.HoveredCell;
            bool canPlace = occupancy.CanPlace(
                anchorCell,
                prototypeBuilding.Footprint,
                selectedRotation) &&
                CanSatisfyPlacementBehavior(anchorCell);
            placementPreview.Show(
                prototypeBuilding,
                gridSystem,
                anchorCell,
                selectedRotation,
                canPlace);

            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace)
            {
                PlaceBuilding(anchorCell);
            }
        }

        private void HandleModeInput()
        {
            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                isPlacementModeActive = true;
                selectedRotation = BuildingRotation.Degrees0;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isPlacementModeActive = false;
                placementPreview.Hide();
            }

            if (isPlacementModeActive && Keyboard.current.rKey.wasPressedThisFrame)
            {
                selectedRotation = selectedRotation.RotateClockwise();
            }
        }

        private void HandleRemovalInput()
        {
            if (!Mouse.current.rightButton.wasPressedThisFrame ||
                !occupancy.TryGetBuilding(hoverHighlight.HoveredCell, out BuildingPlacement placement))
            {
                return;
            }

            occupancy.Remove(placement);
            if (buildingInstances.Remove(placement, out PlacedBuilding instance))
            {
                Destroy(instance.gameObject);
            }
        }

        private void PlaceBuilding(Vector2Int anchorCell)
        {
            if (!occupancy.TryRegister(
                    prototypeBuilding.Id,
                    anchorCell,
                    prototypeBuilding.Footprint,
                    selectedRotation,
                    out BuildingPlacement placement))
            {
                return;
            }

            PlacedBuilding instance = CreateBuildingInstance(placement);
            buildingInstances.Add(placement, instance);
        }

        private PlacedBuilding CreateBuildingInstance(BuildingPlacement placement)
        {
            GameObject buildingObject = prototypeBuilding.InstancePrefab != null
                ? Instantiate(prototypeBuilding.InstancePrefab)
                : new GameObject();
            buildingObject.name = $"{placement.DefinitionId} {placement.AnchorCell}";
            Vector3 firstCellCenter = gridSystem.GridToWorld(placement.AnchorCell);
            buildingObject.transform.position = firstCellCenter + new Vector3(
                (placement.RotatedFootprint.x - 1) * gridSystem.CellSize * 0.5f,
                (placement.RotatedFootprint.y - 1) * gridSystem.CellSize * 0.5f,
                0f);
            buildingObject.transform.rotation = Quaternion.Euler(0f, 0f, (int)placement.Rotation);

            PlacedBuilding instance = buildingObject.GetComponent<PlacedBuilding>();
            if (instance == null)
            {
                instance = buildingObject.AddComponent<PlacedBuilding>();
            }

            instance.Initialize(placement);
            GameObject visual = BuildingVisualFactory.Create(
                prototypeBuilding,
                buildingObject.transform,
                gridSystem.CellSize,
                10);
            BuildingVisualFactory.Tint(visual, prototypeBuilding.PlacedColor);
            GetPlacementBehavior()?.InitializePlacedBuilding(buildingObject, placement);
            return instance;
        }

        private bool CanSatisfyPlacementBehavior(Vector2Int anchorCell)
        {
            IBuildingPlacementBehavior behavior = GetPlacementBehavior();
            return behavior == null || behavior.CanPlace(
                anchorCell,
                prototypeBuilding.Footprint,
                selectedRotation);
        }

        private IBuildingPlacementBehavior GetPlacementBehavior()
        {
            return placementBehavior as IBuildingPlacementBehavior;
        }

        private void OnValidate()
        {
            prototypeBuilding?.Validate();

            if (placementBehavior != null && placementBehavior is not IBuildingPlacementBehavior)
            {
                placementBehavior = null;
            }
        }
    }
}
