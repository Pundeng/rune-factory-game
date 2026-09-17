using System;
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
        [SerializeField] private BuildingPlacementOption[] buildingOptions =
            Array.Empty<BuildingPlacementOption>();

        private readonly GridOccupancy occupancy = new();
        private readonly Dictionary<BuildingPlacement, PlacedBuilding> buildingInstances = new();
        private BuildingRotation selectedRotation;
        private int selectedBuildingIndex;
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
            BuildingPlacementOption selectedOption = GetSelectedOption();
            if (selectedOption == null)
            {
                placementPreview.Hide();
                return;
            }

            BuildingDefinition selectedBuilding = selectedOption.Definition;
            bool canPlace = occupancy.CanPlace(
                anchorCell,
                selectedBuilding.Footprint,
                selectedRotation) &&
                CanSatisfyPlacementBehavior(selectedOption, anchorCell);
            placementPreview.Show(
                selectedBuilding,
                gridSystem,
                anchorCell,
                selectedRotation,
                canPlace);

            if (Mouse.current.leftButton.wasPressedThisFrame && canPlace)
            {
                PlaceBuilding(selectedOption, anchorCell);
            }
        }

        private void HandleModeInput()
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SelectBuilding(0);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SelectBuilding(1);
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SelectBuilding(2);
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                SelectBuilding(3);
            }

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

            if (!buildingInstances.TryGetValue(placement, out PlacedBuilding instance) ||
                !CanRemove(instance.gameObject))
            {
                return;
            }

            occupancy.Remove(placement);
            if (buildingInstances.Remove(placement))
            {
                Destroy(instance.gameObject);
            }
        }

        private void PlaceBuilding(BuildingPlacementOption option, Vector2Int anchorCell)
        {
            BuildingDefinition definition = option.Definition;
            if (!occupancy.TryRegister(
                    definition.Id,
                    anchorCell,
                    definition.Footprint,
                    selectedRotation,
                    out BuildingPlacement placement))
            {
                return;
            }

            PlacedBuilding instance = CreateBuildingInstance(option, placement);
            buildingInstances.Add(placement, instance);
        }

        private PlacedBuilding CreateBuildingInstance(
            BuildingPlacementOption option,
            BuildingPlacement placement)
        {
            BuildingDefinition definition = option.Definition;
            GameObject buildingObject = definition.InstancePrefab != null
                ? Instantiate(definition.InstancePrefab)
                : new GameObject();
            buildingObject.name = $"{placement.DefinitionId} {placement.AnchorCell}";
            Vector3 firstCellCenter = gridSystem.GridToWorld(placement.AnchorCell);
            buildingObject.transform.position = firstCellCenter + new Vector3(
                (placement.RotatedFootprint.x - 1) * gridSystem.CellSize * 0.5f,
                (placement.RotatedFootprint.y - 1) * gridSystem.CellSize * 0.5f,
                0f);
            buildingObject.transform.rotation = Quaternion.Euler(0f, 0f, -(int)placement.Rotation);

            PlacedBuilding instance = buildingObject.GetComponent<PlacedBuilding>();
            if (instance == null)
            {
                instance = buildingObject.AddComponent<PlacedBuilding>();
            }

            instance.Initialize(placement);
            GameObject visual = BuildingVisualFactory.Create(
                definition,
                buildingObject.transform,
                gridSystem.CellSize,
                10);
            BuildingVisualFactory.Tint(visual, definition.PlacedColor);
            option.PlacementBehavior?.InitializePlacedBuilding(buildingObject, placement);
            return instance;
        }

        private bool CanSatisfyPlacementBehavior(
            BuildingPlacementOption option,
            Vector2Int anchorCell)
        {
            IBuildingPlacementBehavior behavior = option.PlacementBehavior;
            return behavior == null || behavior.CanPlace(
                anchorCell,
                option.Definition.Footprint,
                selectedRotation);
        }

        private void SelectBuilding(int index)
        {
            if (index < 0 || index >= buildingOptions.Length || buildingOptions[index] == null)
            {
                return;
            }

            selectedBuildingIndex = index;
            selectedRotation = BuildingRotation.Degrees0;
            isPlacementModeActive = true;
        }

        private BuildingPlacementOption GetSelectedOption()
        {
            return selectedBuildingIndex >= 0 && selectedBuildingIndex < buildingOptions.Length
                ? buildingOptions[selectedBuildingIndex]
                : null;
        }

        private static bool CanRemove(GameObject buildingObject)
        {
            foreach (MonoBehaviour component in buildingObject.GetComponents<MonoBehaviour>())
            {
                if (component is IBuildingRemovalRule removalRule && !removalRule.CanRemove)
                {
                    return false;
                }
            }

            return true;
        }

        private void OnValidate()
        {
            if (buildingOptions == null)
            {
                buildingOptions = Array.Empty<BuildingPlacementOption>();
                return;
            }

            foreach (BuildingPlacementOption option in buildingOptions)
            {
                option?.Validate();
            }
        }
    }
}
