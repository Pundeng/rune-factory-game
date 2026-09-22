using System;
using System.Collections.Generic;
using FantasyShapez.Grid;
using FantasyShapez.Objectives;
using FantasyShapez.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FantasyShapez.Buildings
{
    public sealed class BuildingPlacementController : MonoBehaviour
    {
        [SerializeField] private GridSystem gridSystem = null;
        [SerializeField] private GridHoverHighlight hoverHighlight = null;
        [SerializeField] private BuildingPreview placementPreview = null;
        [SerializeField] private ObjectivePanel engraverUpgradePanel = null;
        [SerializeField] private Hub hub = null;
        [SerializeField] private BuildingPlacementOption[] buildingOptions =
            Array.Empty<BuildingPlacementOption>();

        private readonly GridOccupancy occupancy = new();
        private readonly Dictionary<BuildingPlacement, PlacedBuilding> buildingInstances = new();
        private readonly BeltDragPlacementPlanner beltDragPlanner = new();
        private readonly GridDragTracker placementDrag = new();
        private readonly GridDragTracker removalDrag = new();
        private BuildingRotation selectedRotation;
        private int selectedBuildingIndex;
        private bool isPlacementModeActive;

        private void Awake()
        {
            if (hub == null)
            {
                throw new MissingReferenceException(
                    "The Building Placement Controller requires the scene Hub.");
            }

            if (!occupancy.TryRegister(
                    nameof(Hub),
                    hub.InputCell,
                    hub.Footprint,
                    BuildingRotation.Degrees0,
                    out _))
            {
                throw new InvalidOperationException(
                    $"The Hub footprint at {hub.InputCell} could not be reserved.");
            }
        }

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
            HandleInteractionInput();

            if (!isPlacementModeActive)
            {
                beltDragPlanner.Reset();
                placementDrag.Reset();
                return;
            }

            Vector2Int anchorCell = hoverHighlight.HoveredCell;
            BuildingPlacementOption selectedOption = GetSelectedOption();
            if (selectedOption == null)
            {
                placementPreview.Hide();
                return;
            }

            BuildingRotation previewRotation = GetPreviewRotation(
                selectedOption,
                anchorCell);
            bool canPlace = CanPlaceBuilding(
                selectedOption,
                anchorCell,
                previewRotation);
            placementPreview.Show(
                selectedOption,
                gridSystem,
                anchorCell,
                previewRotation,
                canPlace);

            HandlePlacementInput(selectedOption, anchorCell, canPlace);
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

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                SelectBuilding(4);
            }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                isPlacementModeActive = true;
                selectedRotation = BuildingRotation.Degrees0;
                beltDragPlanner.Reset();
                placementDrag.Reset();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isPlacementModeActive = false;
                placementPreview.Hide();
                beltDragPlanner.Reset();
                placementDrag.Reset();
            }

            if (!isPlacementModeActive)
            {
                return;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                selectedRotation = selectedRotation.RotateClockwise();
            }
        }

        private void HandleRemovalInput()
        {
            if (!Mouse.current.rightButton.isPressed)
            {
                removalDrag.Reset();
                return;
            }

            if (!Mouse.current.rightButton.wasPressedThisFrame && !removalDrag.IsActive)
            {
                return;
            }

            foreach (Vector2Int cell in removalDrag.Continue(hoverHighlight.HoveredCell))
            {
                TryRemoveBuilding(cell);
            }
        }

        private void HandlePlacementInput(
            BuildingPlacementOption option,
            Vector2Int anchorCell,
            bool canPlace)
        {
            if (!Mouse.current.leftButton.isPressed)
            {
                if (option.SupportsContinuousPlacement &&
                    beltDragPlanner.TryComplete(selectedRotation, out BeltPlacementStep finalStep))
                {
                    TryPlaceBuilding(option, finalStep.Cell, finalStep.Rotation);
                }

                placementDrag.Reset();
                return;
            }

            if (!option.SupportsContinuousPlacement)
            {
                beltDragPlanner.Reset();
                placementDrag.Reset();
                if (Mouse.current.leftButton.wasPressedThisFrame && canPlace)
                {
                    PlaceBuilding(option, anchorCell, selectedRotation);
                }

                return;
            }

            if (!Mouse.current.leftButton.wasPressedThisFrame && !placementDrag.IsActive)
            {
                return;
            }

            IReadOnlyList<Vector2Int> newCells = placementDrag.Continue(anchorCell);
            foreach (BeltPlacementStep step in beltDragPlanner.Continue(newCells))
            {
                TryPlaceBuilding(option, step.Cell, step.Rotation);
            }
        }

        private void TryRemoveBuilding(Vector2Int cell)
        {
            if (!occupancy.TryGetBuilding(cell, out BuildingPlacement placement) ||
                !buildingInstances.TryGetValue(placement, out PlacedBuilding instance) ||
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

        private void HandleInteractionInput()
        {
            if (isPlacementModeActive ||
                !Mouse.current.leftButton.wasPressedThisFrame ||
                !occupancy.TryGetBuilding(hoverHighlight.HoveredCell, out BuildingPlacement placement) ||
                !buildingInstances.TryGetValue(placement, out PlacedBuilding instance))
            {
                return;
            }

            foreach (MonoBehaviour component in instance.GetComponents<MonoBehaviour>())
            {
                if (component is FantasyShapez.Production.Engraver engraver)
                {
                    engraverUpgradePanel?.ShowEngraver(engraver);
                    return;
                }

                if (component is FantasyShapez.Production.ElementInfuser infuser)
                {
                    engraverUpgradePanel?.ShowElementInfuser(infuser);
                    return;
                }
            }
        }

        private bool TryPlaceBuilding(
            BuildingPlacementOption option,
            Vector2Int anchorCell,
            BuildingRotation rotation)
        {
            return CanPlaceBuilding(option, anchorCell, rotation) &&
                PlaceBuilding(option, anchorCell, rotation);
        }

        private bool PlaceBuilding(
            BuildingPlacementOption option,
            Vector2Int anchorCell,
            BuildingRotation rotation)
        {
            BuildingDefinition definition = option.Definition;
            if (!occupancy.TryRegister(
                    definition.Id,
                    anchorCell,
                    definition.Footprint,
                    rotation,
                    out BuildingPlacement placement))
            {
                return false;
            }

            PlacedBuilding instance = CreateBuildingInstance(option, placement);
            buildingInstances.Add(placement, instance);
            return true;
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
            Vector2Int anchorCell,
            BuildingRotation rotation)
        {
            IBuildingPlacementBehavior behavior = option.PlacementBehavior;
            return behavior == null || behavior.CanPlace(
                anchorCell,
                option.Definition.Footprint,
                rotation);
        }

        private bool CanPlaceBuilding(
            BuildingPlacementOption option,
            Vector2Int anchorCell,
            BuildingRotation rotation)
        {
            BuildingDefinition definition = option.Definition;
            return occupancy.CanPlace(
                    anchorCell,
                    definition.Footprint,
                    rotation) &&
                CanSatisfyPlacementBehavior(option, anchorCell, rotation);
        }

        private BuildingRotation GetPreviewRotation(
            BuildingPlacementOption option,
            Vector2Int anchorCell)
        {
            if (!option.SupportsContinuousPlacement)
            {
                return selectedRotation;
            }

            if (placementDrag.LastCell.HasValue &&
                BeltDragPlacementPlanner.TryGetPathRotation(
                    placementDrag.LastCell.Value,
                    anchorCell,
                    out BuildingRotation pathRotation))
            {
                return pathRotation;
            }

            return beltDragPlanner.IsActive
                ? beltDragPlanner.GetPreviewRotation(selectedRotation)
                : selectedRotation;
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
            beltDragPlanner.Reset();
            placementDrag.Reset();
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

    public sealed class GridDragTracker
    {
        private readonly List<Vector2Int> newCells = new();
        private readonly HashSet<Vector2Int> visitedCells = new();
        private Vector2Int? lastCell;

        public bool IsActive => lastCell.HasValue;

        public Vector2Int? LastCell => lastCell;

        public IReadOnlyList<Vector2Int> Continue(Vector2Int currentCell)
        {
            newCells.Clear();
            if (!lastCell.HasValue)
            {
                AddIfNew(currentCell);
                lastCell = currentCell;
                return newCells;
            }

            Vector2Int nextCell = lastCell.Value;
            while (nextCell.x != currentCell.x)
            {
                nextCell.x += Math.Sign(currentCell.x - nextCell.x);
                AddIfNew(nextCell);
            }

            while (nextCell.y != currentCell.y)
            {
                nextCell.y += Math.Sign(currentCell.y - nextCell.y);
                AddIfNew(nextCell);
            }

            lastCell = currentCell;
            return newCells;
        }

        public void Reset()
        {
            lastCell = null;
            visitedCells.Clear();
            newCells.Clear();
        }

        private void AddIfNew(Vector2Int cell)
        {
            if (visitedCells.Add(cell))
            {
                newCells.Add(cell);
            }
        }
    }

    public readonly struct BeltPlacementStep
    {
        public BeltPlacementStep(Vector2Int cell, BuildingRotation rotation)
        {
            Cell = cell;
            Rotation = rotation;
        }

        public Vector2Int Cell { get; }

        public BuildingRotation Rotation { get; }
    }

    public sealed class BeltDragPlacementPlanner
    {
        private readonly List<BeltPlacementStep> completedSteps = new();
        private bool hasPathRotation;
        private Vector2Int? pendingCell;

        public bool IsActive => pendingCell.HasValue;

        public BuildingRotation LastPathRotation { get; private set; }

        public BuildingRotation GetPreviewRotation(BuildingRotation fallbackRotation)
        {
            return hasPathRotation ? LastPathRotation : fallbackRotation;
        }

        public IReadOnlyList<BeltPlacementStep> Continue(IReadOnlyList<Vector2Int> pathCells)
        {
            completedSteps.Clear();
            foreach (Vector2Int cell in pathCells)
            {
                if (!pendingCell.HasValue)
                {
                    pendingCell = cell;
                    continue;
                }

                if (!TryGetPathRotation(
                        pendingCell.Value,
                        cell,
                        out BuildingRotation rotation))
                {
                    continue;
                }

                completedSteps.Add(new BeltPlacementStep(pendingCell.Value, rotation));
                pendingCell = cell;
                LastPathRotation = rotation;
                hasPathRotation = true;
            }

            return completedSteps;
        }

        public bool TryComplete(
            BuildingRotation singlePlacementRotation,
            out BeltPlacementStep finalStep)
        {
            if (!pendingCell.HasValue)
            {
                finalStep = default;
                return false;
            }

            BuildingRotation rotation = hasPathRotation
                ? LastPathRotation
                : singlePlacementRotation;
            finalStep = new BeltPlacementStep(pendingCell.Value, rotation);
            Reset();
            return true;
        }

        public void Reset()
        {
            completedSteps.Clear();
            hasPathRotation = false;
            pendingCell = null;
            LastPathRotation = BuildingRotation.Degrees0;
        }

        public static bool TryGetPathRotation(
            Vector2Int source,
            Vector2Int destination,
            out BuildingRotation rotation)
        {
            Vector2Int offset = destination - source;
            if (offset == Vector2Int.up)
            {
                rotation = BuildingRotation.Degrees0;
                return true;
            }

            if (offset == Vector2Int.right)
            {
                rotation = BuildingRotation.Degrees90;
                return true;
            }

            if (offset == Vector2Int.down)
            {
                rotation = BuildingRotation.Degrees180;
                return true;
            }

            if (offset == Vector2Int.left)
            {
                rotation = BuildingRotation.Degrees270;
                return true;
            }

            rotation = default;
            return false;
        }
    }
}
