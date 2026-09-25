using System;
using System.Collections.Generic;
using System.Linq;
using FantasyShapez.Food;
using FantasyShapez.Grid;
using FantasyShapez.Logistics;
using FantasyShapez.Objectives;
using FantasyShapez.Production;
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
        [SerializeField] private Market market = null;
        [SerializeField] private BuildingPlacementOption[] buildingOptions =
            Array.Empty<BuildingPlacementOption>();
        [SerializeField] private PropertySourceSetup[] propertySources =
            Array.Empty<PropertySourceSetup>();
        [SerializeField] private BeltTransportCoordinator processorTransportCoordinator = null;
        [SerializeField, Min(0.01f)] private float processorDuration = 1f;
        [SerializeField] private ProcessingRecipe[] processorRecipes =
            Array.Empty<ProcessingRecipe>();
        [SerializeField] private MixingRecipe[] mixerRecipes =
            Array.Empty<MixingRecipe>();

        private readonly GridOccupancy occupancy = new();
        private readonly RecipeDiscoveryRegistry recipeDiscoveries = new();
        private readonly Dictionary<BuildingPlacement, PlacedBuilding> buildingInstances = new();
        private readonly BeltDragPlacementPlanner beltDragPlanner = new();
        private readonly GridDragTracker placementDrag = new();
        private readonly GridDragTracker removalDrag = new();
        private readonly BuildingSelection selection = new();
        private readonly Dictionary<BuildingPlacement, GameObject> selectionHighlights = new();
        private Vector2Int? selectionStartCell;
        private GameObject selectionArea;
        private BuildingRotation selectedRotation;
        private int selectedBuildingIndex;
        private bool isPlacementModeActive;
        private Engraver.RecipeConfiguration? copiedEngraverRecipe;
        private ElementInfuser.RecipeConfiguration? copiedInfuserRecipe;
        private BuildingGroupCopy copiedGroup;
        private BuildingGroupCopy activeGroup;
        private readonly List<BuildingPlacement> moveSources = new();
        private readonly HashSet<BuildingPlacement> moveSourceSet = new();
        private readonly List<BuildingPreview> groupPreviews = new();
        private bool isGroupPasteModeActive;
        private bool pasteAwaitingMouseRelease;
        private PropertySupplyPlayMode propertySupply;
        private RecipeDiscoveryPanel recipeDiscoveryPanel;
        private MarketPanel marketPanel;

        public PropertySupplyPlayMode PropertySupply => propertySupply;
        public Market Market => market;
        public IReadOnlyList<DiscoveredRecipe> DiscoveredRecipes =>
            recipeDiscoveries.DiscoveredRecipes;
        public RecipeDiscoveryRegistry RecipeDiscoveries => recipeDiscoveries;
        public IReadOnlyList<ProcessingRecipe> ProcessorRecipes => processorRecipes;
        public IReadOnlyList<MixingRecipe> MixerRecipes => mixerRecipes;

        public FactoryWorldData CaptureWorldSnapshot()
        {
            if (propertySupply == null)
            {
                throw new InvalidOperationException("Factory world is not initialized.");
            }

            if (hub.AccelerationRuneCount > 0 || hub.Progress?.HasProgress == true)
            {
                throw new InvalidOperationException(
                    "Factory snapshot cannot save active legacy RuneData Hub progress.");
            }

            var saved = new List<SavedBuilding>(buildingInstances.Count);
            foreach (KeyValuePair<BuildingPlacement, PlacedBuilding> entry in buildingInstances)
            {
                BuildingPlacement placement = entry.Key;
                PlacedBuilding instance = entry.Value;
                var building = new SavedBuilding
                {
                    definitionId = placement.DefinitionId,
                    x = placement.AnchorCell.x,
                    y = placement.AnchorCell.y,
                    rotation = placement.Rotation
                };
                switch (placement.DefinitionId)
                {
                    case nameof(FarmPlot):
                        building.farmPlot = instance.GetComponent<FarmPlot>()?.CaptureWorldState();
                        break;
                    case nameof(Harvester):
                        building.harvester = instance.GetComponent<Harvester>()?.CaptureWorldState();
                        break;
                    case nameof(Belt):
                        building.belt = instance.GetComponent<Belt>()?.CaptureWorldState();
                        break;
                    case nameof(Processor):
                        building.processor = instance.GetComponent<Processor>()?.CaptureWorldState();
                        break;
                    case nameof(BasicMixer):
                        building.mixer = instance.GetComponent<BasicMixer>()?.CaptureWorldState();
                        break;
                    default:
                        throw new InvalidOperationException(
                            $"Factory snapshot cannot save legacy building {placement.DefinitionId} " +
                            "or its active RuneData state.");
                }

                saved.Add(building);
            }

            return new FactoryWorldData
            {
                buildings = saved.OrderBy(item => item.definitionId, StringComparer.Ordinal)
                    .ThenBy(item => item.x).ThenBy(item => item.y).ToArray(),
                connections = propertySupply.CaptureWorldConnections()
            };
        }

        public void ValidateWorldSnapshot(FactoryWorldData world,
            IReadOnlyList<SavedUnlock> savedUnlocks) =>
            FactoryWorldSnapshotValidator.ValidateAgainstScene(world, buildingOptions,
                propertySources, market.Regions, savedUnlocks, processorRecipes,
                mixerRecipes, market.InputCell, hub.InputCell);

        public void RestoreWorldSnapshot(FactoryWorldData world,
            IReadOnlyList<SavedUnlock> savedUnlocks)
        {
            ValidateWorldSnapshot(world, savedUnlocks);
            if (buildingInstances.Count != 0)
            {
                throw new InvalidOperationException("Factory reconstruction requires a fresh scene.");
            }

            // Overlay placement requires the underlying Farm Plot to exist first.
            IEnumerable<SavedBuilding> ordered =
                FactoryWorldSnapshotValidator.ReconstructionOrder(world);
            foreach (SavedBuilding saved in ordered)
            {
                BuildingPlacementOption option = buildingOptions.FirstOrDefault(candidate =>
                    candidate?.Definition?.Id == saved.definitionId);
                Vector2Int anchor = new(saved.x, saved.y);
                if (option == null || !CanPlaceBuilding(option, anchor, saved.rotation) ||
                    !PlaceBuilding(option, anchor, saved.rotation, null, null,
                        out BuildingPlacement placement))
                {
                    throw new InvalidOperationException(
                        $"Cannot restore {saved.definitionId} at {anchor}.");
                }

                PlacedBuilding instance = buildingInstances[placement];
                if (saved.farmPlot != null)
                    instance.GetComponent<FarmPlot>().RestoreWorldState(saved.farmPlot);
                else if (saved.harvester != null)
                    instance.GetComponent<Harvester>().RestoreWorldState(saved.harvester);
                else if (saved.processor != null)
                    instance.GetComponent<Processor>().RestoreWorldState(saved.processor);
                else if (saved.mixer != null)
                    instance.GetComponent<BasicMixer>().RestoreWorldState(saved.mixer);
                else if (saved.belt != null)
                    instance.GetComponent<Belt>().RestoreWorldState(saved.belt);
            }

            propertySupply.RestoreWorldConnections(world.connections);
        }
        public event Action<DiscoveredRecipe> RecipeDiscovered
        {
            add => recipeDiscoveries.Discovered += value;
            remove => recipeDiscoveries.Discovered -= value;
        }

        private void Awake()
        {
            recipeDiscoveryPanel = GetComponent<RecipeDiscoveryPanel>();
            marketPanel = market?.GetComponent<MarketPanel>();
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

            if (market != null && !occupancy.TryRegister(
                    nameof(Market),
                    market.InputCell,
                    market.Footprint,
                    BuildingRotation.Degrees0,
                    out _))
            {
                throw new InvalidOperationException(
                    $"The Market footprint at {market.InputCell} could not be reserved.");
            }

            propertySupply = new PropertySupplyPlayMode(gridSystem, hoverHighlight,
                occupancy, transform, propertySources);
            foreach (BuildingPlacementOption option in buildingOptions)
            {
                if (option?.Definition?.Id == nameof(Processor))
                {
                    var processorBehavior = gameObject.AddComponent<ProcessorPlacementBehavior>();
                    processorBehavior.Configure(processorTransportCoordinator, processorRecipes,
                        processorDuration, recipeDiscoveries);
                    option.SetRuntimePlacementBehavior(processorBehavior);
                }
                else if (option?.Definition?.Id == nameof(BasicMixer))
                {
                    var mixerBehavior = gameObject.AddComponent<BasicMixerPlacementBehavior>();
                    mixerBehavior.Configure(processorTransportCoordinator, mixerRecipes,
                        recipeDiscoveries);
                    option.SetRuntimePlacementBehavior(mixerBehavior);
                }
            }
        }

        private void OnGUI()
        {
            propertySupply?.DrawGUI();
            if (propertySupply?.IsActive == true &&
                (isPlacementModeActive || isGroupPasteModeActive))
            {
                isPlacementModeActive = false;
                placementPreview.Hide();
                ExitGroupPasteMode();
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

            if (recipeDiscoveryPanel != null && recipeDiscoveryPanel.BlocksWorldInput)
            {
                return;
            }

            if (marketPanel != null && marketPanel.IsPointerOverPanel)
            {
                return;
            }

            if (engraverUpgradePanel != null && engraverUpgradePanel.IsPointerOverPanel)
            {
                return;
            }

            if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                propertySupply?.TogglePanel();
            }

            if (propertySupply != null &&
                (propertySupply.IsActive || propertySupply.IsPointerOverPanel()))
            {
                if (propertySupply.IsActive)
                {
                    propertySupply.HandleInput();
                }

                return;
            }

            bool wasGroupPasteModeActive = isGroupPasteModeActive;
            HandleModeInput();
            if (isGroupPasteModeActive)
            {
                HandleGroupPasteInput();
                return;
            }

            if (wasGroupPasteModeActive)
            {
                return;
            }

            if (HandleSelectionInput())
            {
                return;
            }

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
            if (selectedOption.PlacementBehavior is HarvesterPlacementBehavior)
            {
                anchorCell = HarvesterPlacementBehavior.GetAnchorForFarmCell(
                    anchorCell, selectedOption.Definition.Footprint, previewRotation);
            }

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
            if (Keyboard.current.ctrlKey.isPressed &&
                Keyboard.current.cKey.wasPressedThisFrame)
            {
                CopySelection();
            }

            if (Keyboard.current.ctrlKey.isPressed &&
                Keyboard.current.xKey.wasPressedThisFrame)
            {
                CutSelection();
            }

            if (Keyboard.current.ctrlKey.isPressed &&
                Keyboard.current.vKey.wasPressedThisFrame)
            {
                EnterGroupPasteMode(copiedGroup);
            }

            if (isGroupPasteModeActive)
            {
                if (Keyboard.current.escapeKey.wasPressedThisFrame)
                {
                    ExitGroupPasteMode();
                }

                if (isGroupPasteModeActive && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    activeGroup = activeGroup.RotateClockwise();
                }

                if (isGroupPasteModeActive && !Keyboard.current.ctrlKey.isPressed &&
                    Keyboard.current.hKey.wasPressedThisFrame)
                {
                    if (activeGroup.TryMirrorHorizontal(out BuildingGroupCopy mirrored))
                    {
                        activeGroup = mirrored;
                    }
                    else
                    {
                        Debug.LogWarning("This group has a building that cannot be mirrored horizontally.", this);
                    }
                }

                if (isGroupPasteModeActive && !Keyboard.current.ctrlKey.isPressed &&
                    Keyboard.current.vKey.wasPressedThisFrame)
                {
                    if (activeGroup.TryMirrorVertical(out BuildingGroupCopy mirrored))
                    {
                        activeGroup = mirrored;
                    }
                    else
                    {
                        Debug.LogWarning("This group has a building that cannot be mirrored vertically.", this);
                    }
                }

                return;
            }

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

            if (Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                SelectBuilding(5);
            }

            if (Keyboard.current.digit7Key.wasPressedThisFrame)
            {
                SelectBuilding(6);
            }

            if (Keyboard.current.digit8Key.wasPressedThisFrame)
            {
                SelectBuilding(7);
            }

            if (!Keyboard.current.ctrlKey.isPressed &&
                Keyboard.current.cKey.wasPressedThisFrame)
            {
                TryCopyHoveredMachine();
            }

            if (Keyboard.current.bKey.wasPressedThisFrame)
            {
                isPlacementModeActive = true;
                selectedRotation = BuildingRotation.Degrees0;
                ClearCopiedRecipe();
                beltDragPlanner.Reset();
                placementDrag.Reset();
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isPlacementModeActive = false;
                selectionStartCell = null;
                selection.Clear();
                RefreshSelectionHighlights();
                HideSelectionArea();
                ClearCopiedRecipe();
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

        private void CopySelection()
        {
            if (!TryCaptureSelection(out BuildingGroupCopy group))
            {
                return;
            }

            copiedGroup = group;
            ExitGroupPasteMode();
        }

        private void CutSelection()
        {
            var sources = new List<BuildingPlacement>(selection.SelectedPlacements);
            if (sources.Count == 0 || !CanMoveSources(sources) ||
                !TryCaptureSelection(out BuildingGroupCopy group))
            {
                return;
            }

            EnterGroupPasteMode(group, sources);
        }

        private bool TryCaptureSelection(out BuildingGroupCopy group)
        {
            group = null;
            if (selection.SelectedPlacements.Count == 0)
            {
                return false;
            }

            var sourceItems = new List<BuildingGroupCopyItem>();
            foreach (BuildingPlacement placement in selection.SelectedPlacements)
            {
                if (!buildingInstances.TryGetValue(placement, out PlacedBuilding instance) ||
                    instance.GetComponent<Harvester>() != null ||
                    !TryGetCopyOption(placement, out BuildingPlacementOption option))
                {
                    return false;
                }

                sourceItems.Add(new BuildingGroupCopyItem(
                    option,
                    placement.AnchorCell,
                    placement.Rotation,
                    instance.GetComponent<Engraver>()?.CaptureRecipeConfiguration(),
                    instance.GetComponent<ElementInfuser>()?.CaptureRecipeConfiguration()));
            }

            group = new BuildingGroupCopy(sourceItems);
            return true;
        }

        private bool TryGetCopyOption(
            BuildingPlacement placement,
            out BuildingPlacementOption option)
        {
            foreach (BuildingPlacementOption candidate in buildingOptions)
            {
                if (candidate?.Definition?.Id == placement.DefinitionId &&
                    candidate.Definition.InstancePrefab != null)
                {
                    option = candidate;
                    return true;
                }
            }

            option = null;
            return false;
        }

        private bool CanMoveSources(IReadOnlyList<BuildingPlacement> sources)
        {
            foreach (BuildingPlacement source in sources)
            {
                if (!occupancy.TryGetBuilding(source.AnchorCell,
                        out BuildingPlacement registered) ||
                    !ReferenceEquals(source, registered) ||
                    !buildingInstances.TryGetValue(source, out PlacedBuilding instance) ||
                    !CanRemove(instance.gameObject) ||
                    GetMoveState(instance.gameObject)?.CanMove != true)
                {
                    return false;
                }
            }

            return true;
        }

        private static IBuildingMoveState GetMoveState(GameObject buildingObject)
        {
            foreach (MonoBehaviour component in buildingObject.GetComponents<MonoBehaviour>())
            {
                if (component is IBuildingMoveState moveState)
                {
                    return moveState;
                }
            }

            return null;
        }

        private void EnterGroupPasteMode(
            BuildingGroupCopy group,
            IReadOnlyList<BuildingPlacement> sources = null)
        {
            if (group == null)
            {
                return;
            }

            ExitGroupPasteMode();
            activeGroup = group;
            if (sources != null)
            {
                moveSources.AddRange(sources);
                moveSourceSet.UnionWith(sources);
            }
            isPlacementModeActive = false;
            selectionStartCell = null;
            HideSelectionArea();
            beltDragPlanner.Reset();
            placementDrag.Reset();
            placementPreview.Hide();
            foreach (BuildingGroupCopyItem item in activeGroup.Items)
            {
                var previewObject = new GameObject("Group Paste Preview");
                previewObject.transform.SetParent(transform, false);
                groupPreviews.Add(previewObject.AddComponent<BuildingPreview>());
            }

            isGroupPasteModeActive = true;
            pasteAwaitingMouseRelease = true;
        }

        private void ExitGroupPasteMode()
        {
            isGroupPasteModeActive = false;
            pasteAwaitingMouseRelease = false;
            activeGroup = null;
            moveSources.Clear();
            moveSourceSet.Clear();
            foreach (BuildingPreview preview in groupPreviews)
            {
                if (preview != null)
                {
                    Destroy(preview.gameObject);
                }
            }

            groupPreviews.Clear();
        }

        private void HandleGroupPasteInput()
        {
            Vector2Int anchorCell = hoverHighlight.HoveredCell;
            bool canPlaceGroup =
                (moveSources.Count == 0 || CanMoveSources(moveSources)) &&
                activeGroup.CanPlace(anchorCell, occupancy,
                    moveSources.Count == 0 ? null : moveSourceSet);
            for (int index = 0; index < activeGroup.Items.Count; index++)
            {
                BuildingGroupCopyItem item = activeGroup.Items[index];
                groupPreviews[index].Show(
                    item.Option,
                    gridSystem,
                    anchorCell + item.Offset,
                    item.Rotation,
                    canPlaceGroup);
            }

            if (pasteAwaitingMouseRelease)
            {
                pasteAwaitingMouseRelease = Mouse.current.leftButton.isPressed;
                return;
            }

            if (!canPlaceGroup || !Mouse.current.leftButton.wasPressedThisFrame)
            {
                return;
            }

            var placedItems = new List<BuildingPlacement>();
            bool placementFailed = false;
            try
            {
                foreach (BuildingPlacement source in moveSources)
                {
                    GetMoveState(buildingInstances[source].gameObject).DetachForMove();
                    occupancy.Remove(source);
                }

                foreach (BuildingGroupCopyItem item in activeGroup.Items)
                {
                    if (!PlaceBuilding(item.Option, anchorCell + item.Offset,
                            item.Rotation, item.EngraverRecipe, item.InfuserRecipe,
                            out BuildingPlacement placement))
                    {
                        placementFailed = true;
                        break;
                    }

                    placedItems.Add(placement);
                }

                if (!placementFailed)
                {
                    for (int index = 0; index < moveSources.Count; index++)
                    {
                        RuneExtractor sourceExtractor = buildingInstances[moveSources[index]]
                            .GetComponent<RuneExtractor>();
                        if (sourceExtractor == null)
                        {
                            continue;
                        }

                        RuneExtractor movedExtractor = buildingInstances[placedItems[index]]
                            .GetComponent<RuneExtractor>();
                        if (movedExtractor == null)
                        {
                            throw new InvalidOperationException(
                                "Moved extractor prefab has no RuneExtractor component.");
                        }

                        movedExtractor.CopyMoveStateFrom(sourceExtractor);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception, this);
                placementFailed = true;
            }

            if (placementFailed)
            {
                RestoreFailedGroupPlacement(placedItems);
                return;
            }

            foreach (BuildingPlacement source in moveSources)
            {
                PlacedBuilding instance = buildingInstances[source];
                buildingInstances.Remove(source);
                selection.Remove(source);
                Destroy(instance.gameObject);
            }

            RefreshSelectionHighlights();

            ExitGroupPasteMode();
        }

        private void RestoreFailedGroupPlacement(IReadOnlyList<BuildingPlacement> placedItems)
        {
            foreach (BuildingPlacement placed in placedItems)
            {
                occupancy.Remove(placed);
                if (buildingInstances.TryGetValue(placed, out PlacedBuilding instance))
                {
                    buildingInstances.Remove(placed);
                    GetMoveState(instance.gameObject)?.DetachForMove();
                    Destroy(instance.gameObject);
                }
            }

            foreach (BuildingPlacement source in moveSources)
            {
                if (!occupancy.TryRestore(source))
                {
                    throw new InvalidOperationException(
                        $"Could not restore moved building at {source.AnchorCell}.");
                }

                GetMoveState(buildingInstances[source].gameObject).ReattachAfterFailedMove();
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

        private bool HandleSelectionInput()
        {
            if (!selectionStartCell.HasValue &&
                Keyboard.current.shiftKey.isPressed &&
                Mouse.current.leftButton.wasPressedThisFrame)
            {
                selectionStartCell = hoverHighlight.HoveredCell;
                beltDragPlanner.Reset();
                placementDrag.Reset();
                placementPreview.Hide();
            }

            if (selectionStartCell.HasValue)
            {
                Vector2Int endCell = hoverHighlight.HoveredCell;
                selection.SelectRectangle(
                    occupancy,
                    selectionStartCell.Value,
                    endCell,
                    buildingInstances.ContainsKey);
                RefreshSelectionHighlights();
                ShowSelectionArea(selectionStartCell.Value, endCell);
                if (!Mouse.current.leftButton.isPressed)
                {
                    selectionStartCell = null;
                    HideSelectionArea();
                }

                return true;
            }

            if (Keyboard.current.deleteKey.wasPressedThisFrame)
            {
                foreach (BuildingPlacement placement in
                    new List<BuildingPlacement>(selection.SelectedPlacements))
                {
                    TryRemovePlacement(placement);
                }
            }

            return false;
        }

        private void ShowSelectionArea(Vector2Int firstCell, Vector2Int lastCell)
        {
            selectionArea ??= CreateSelectionVisual(
                "Selection Area", new Color(0.25f, 0.8f, 1f, 0.16f), 60);
            PositionSelectionVisual(selectionArea, firstCell, lastCell);
            selectionArea.SetActive(true);
        }

        private void HideSelectionArea()
        {
            if (selectionArea != null)
            {
                selectionArea.SetActive(false);
            }
        }

        private void RefreshSelectionHighlights()
        {
            foreach (BuildingPlacement placement in
                new List<BuildingPlacement>(selectionHighlights.Keys))
            {
                if (selection.Contains(placement))
                {
                    continue;
                }

                Destroy(selectionHighlights[placement]);
                selectionHighlights.Remove(placement);
            }

            foreach (BuildingPlacement placement in selection.SelectedPlacements)
            {
                if (selectionHighlights.ContainsKey(placement))
                {
                    continue;
                }

                GameObject highlight = CreateSelectionVisual(
                    "Selected Building", new Color(0.2f, 0.85f, 1f, 0.38f), 70);
                foreach (Vector2Int cell in placement.OccupiedCells)
                {
                    GameObject cellHighlight = CreateSelectionVisual(
                        "Occupied Cell", new Color(0.2f, 0.85f, 1f, 0.38f), 70);
                    cellHighlight.transform.SetParent(highlight.transform, true);
                    PositionSelectionVisual(cellHighlight, cell, cell);
                }
                highlight.GetComponent<SpriteRenderer>().enabled = false;
                selectionHighlights.Add(placement, highlight);
            }
        }

        private GameObject CreateSelectionVisual(string name, Color color, int sortingOrder)
        {
            var visual = new GameObject(name);
            visual.transform.SetParent(transform, false);
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = BuildingVisualFactory.PlaceholderSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return visual;
        }

        private void PositionSelectionVisual(
            GameObject visual, Vector2Int firstCell, Vector2Int lastCell)
        {
            int minX = Math.Min(firstCell.x, lastCell.x);
            int maxX = Math.Max(firstCell.x, lastCell.x);
            int minY = Math.Min(firstCell.y, lastCell.y);
            int maxY = Math.Max(firstCell.y, lastCell.y);
            Vector3 firstCenter = gridSystem.GridToWorld(new Vector2Int(minX, minY));
            Vector3 lastCenter = gridSystem.GridToWorld(new Vector2Int(maxX, maxY));
            visual.transform.position = (firstCenter + lastCenter) * 0.5f +
                new Vector3(0f, 0f, -0.05f);
            visual.transform.localScale = new Vector3(
                (maxX - minX + 1) * gridSystem.CellSize,
                (maxY - minY + 1) * gridSystem.CellSize,
                1f);
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
                    if (PlaceBuilding(option, anchorCell, selectedRotation) &&
                        occupancy.TryGetBuilding(anchorCell, out BuildingPlacement placed) &&
                        buildingInstances.TryGetValue(placed, out PlacedBuilding instance))
                    {
                        FarmPlot farmPlot = instance.GetComponent<FarmPlot>();
                        if (farmPlot != null)
                        {
                            isPlacementModeActive = false;
                            placementPreview.Hide();
                            engraverUpgradePanel?.ShowFarmPlot(farmPlot);
                        }
                        else if (instance.TryGetComponent(out Harvester harvester))
                        {
                            isPlacementModeActive = false;
                            placementPreview.Hide();
                            engraverUpgradePanel?.ShowHarvester(harvester);
                        }
                        else if (instance.TryGetComponent(out Processor processor))
                        {
                            isPlacementModeActive = false;
                            placementPreview.Hide();
                            engraverUpgradePanel?.ShowProcessor(processor);
                        }
                        else if (instance.TryGetComponent(out BasicMixer mixer))
                        {
                            isPlacementModeActive = false;
                            placementPreview.Hide();
                            engraverUpgradePanel?.ShowMixer(mixer);
                        }
                    }
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
            if (occupancy.TryGetBuilding(cell, out BuildingPlacement placement))
            {
                TryRemovePlacement(placement);
            }
        }

        private void TryRemovePlacement(BuildingPlacement placement)
        {
            if (!buildingInstances.TryGetValue(placement, out PlacedBuilding instance) ||
                !CanRemove(instance.gameObject))
            {
                return;
            }

            if (occupancy.Remove(placement) && buildingInstances.Remove(placement))
            {
                selection.Remove(placement);
                RefreshSelectionHighlights();
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

                if (component is FantasyShapez.Food.FarmPlot farmPlot)
                {
                    engraverUpgradePanel?.ShowFarmPlot(farmPlot);
                    return;
                }

                if (component is FantasyShapez.Food.Harvester harvester)
                {
                    engraverUpgradePanel?.ShowHarvester(harvester);
                    return;
                }

                if (component is Processor processor)
                {
                    engraverUpgradePanel?.ShowProcessor(processor);
                    return;
                }

                if (component is BasicMixer mixer)
                {
                    engraverUpgradePanel?.ShowMixer(mixer);
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
            return PlaceBuilding(option, anchorCell, rotation,
                copiedEngraverRecipe, copiedInfuserRecipe, out _);
        }

        private bool PlaceBuilding(
            BuildingPlacementOption option,
            Vector2Int anchorCell,
            BuildingRotation rotation,
            Engraver.RecipeConfiguration? engraverRecipe,
            ElementInfuser.RecipeConfiguration? infuserRecipe,
            out BuildingPlacement placement)
        {
            BuildingDefinition definition = option.Definition;
            placement = null;
            bool registered;
            if (option.PlacementBehavior is HarvesterPlacementBehavior)
            {
                registered = TryGetHarvesterFarmPlacement(option, anchorCell, rotation,
                        out Vector2Int farmCell, out BuildingPlacement farmPlacement) &&
                    occupancy.TryRegisterOver(definition.Id, anchorCell, definition.Footprint,
                        rotation, farmCell, farmPlacement, out placement);
            }
            else
            {
                registered = occupancy.TryRegister(definition, anchorCell, rotation,
                    out placement);
            }

            if (!registered)
            {
                return false;
            }

            try
            {
                PlacedBuilding instance = CreateBuildingInstance(
                    option, placement, engraverRecipe, infuserRecipe);
                buildingInstances.Add(placement, instance);
                return true;
            }
            catch
            {
                occupancy.Remove(placement);
                throw;
            }
        }

        private PlacedBuilding CreateBuildingInstance(
            BuildingPlacementOption option,
            BuildingPlacement placement,
            Engraver.RecipeConfiguration? engraverRecipe,
            ElementInfuser.RecipeConfiguration? infuserRecipe)
        {
            BuildingDefinition definition = option.Definition;
            GameObject buildingObject = definition.InstancePrefab != null
                ? Instantiate(definition.InstancePrefab)
                : new GameObject();
            try
            {
                buildingObject.name = $"{placement.DefinitionId} {placement.AnchorCell}";
                Vector3 firstCellCenter = gridSystem.GridToWorld(placement.AnchorCell);
                buildingObject.transform.position = firstCellCenter + new Vector3(
                    (placement.RotatedFootprint.x - 1) * gridSystem.CellSize * 0.5f,
                    (placement.RotatedFootprint.y - 1) * gridSystem.CellSize * 0.5f,
                    0f);
                buildingObject.transform.rotation = Quaternion.Euler(0f, 0f,
                    -(int)placement.Rotation);

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
                    buildingObject.GetComponent<Harvester>() != null ? 11 : 10);
                BuildingVisualFactory.Tint(visual, definition.PlacedColor);
                if (engraverRecipe.HasValue)
                {
                    buildingObject.GetComponent<Engraver>()?.ApplyRecipeConfiguration(
                        engraverRecipe.Value);
                }
                else if (infuserRecipe.HasValue)
                {
                    buildingObject.GetComponent<ElementInfuser>()?.ApplyRecipeConfiguration(
                        infuserRecipe.Value);
                }

                option.PlacementBehavior?.InitializePlacedBuilding(buildingObject, placement);
                return instance;
            }
            catch
            {
                GetMoveState(buildingObject)?.DetachForMove();
                Destroy(buildingObject);
                throw;
            }
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
            if (option.PlacementBehavior is HarvesterPlacementBehavior)
            {
                return CanSatisfyPlacementBehavior(option, anchorCell, rotation) &&
                    TryGetHarvesterFarmPlacement(option, anchorCell, rotation,
                        out Vector2Int farmCell, out BuildingPlacement farmPlacement) &&
                    occupancy.CanPlaceOver(anchorCell, definition.Footprint, rotation,
                        farmCell, farmPlacement);
            }

            return occupancy.CanPlace(definition, anchorCell, rotation) &&
                CanSatisfyPlacementBehavior(option, anchorCell, rotation);
        }

        private bool TryGetHarvesterFarmPlacement(
            BuildingPlacementOption option,
            Vector2Int anchorCell,
            BuildingRotation rotation,
            out Vector2Int farmCell,
            out BuildingPlacement farmPlacement)
        {
            farmCell = HarvesterPlacementBehavior.GetFarmCell(
                anchorCell, option.Definition.Footprint, rotation);
            farmPlacement = null;
            return FarmPlot.GetAt(farmCell) != null &&
                occupancy.TryGetUnderlyingBuilding(farmCell, out farmPlacement) &&
                farmPlacement.DefinitionId == nameof(FarmPlot);
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
            ClearCopiedRecipe();
            isPlacementModeActive = true;
            beltDragPlanner.Reset();
            placementDrag.Reset();
        }

        private void TryCopyHoveredMachine()
        {
            if (!occupancy.TryGetBuilding(hoverHighlight.HoveredCell, out BuildingPlacement placement) ||
                !buildingInstances.TryGetValue(placement, out PlacedBuilding instance))
            {
                return;
            }

            Engraver engraver = instance.GetComponent<Engraver>();
            ElementInfuser infuser = instance.GetComponent<ElementInfuser>();
            if (engraver == null && infuser == null)
            {
                return;
            }

            for (int index = 0; index < buildingOptions.Length; index++)
            {
                BuildingPlacementOption option = buildingOptions[index];
                if (option?.Definition?.Id != placement.DefinitionId ||
                    option.Definition.InstancePrefab == null)
                {
                    continue;
                }

                if (engraver != null &&
                    option.Definition.InstancePrefab.GetComponent<Engraver>() != null)
                {
                    SelectBuilding(index);
                    copiedEngraverRecipe = engraver.CaptureRecipeConfiguration();
                }
                else if (infuser != null &&
                    option.Definition.InstancePrefab.GetComponent<ElementInfuser>() != null)
                {
                    SelectBuilding(index);
                    copiedInfuserRecipe = infuser.CaptureRecipeConfiguration();
                }
                else
                {
                    continue;
                }

                selectedRotation = placement.Rotation;
                return;
            }
        }

        private void ClearCopiedRecipe()
        {
            copiedEngraverRecipe = null;
            copiedInfuserRecipe = null;
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

    public sealed class BuildingSelection
    {
        private readonly List<BuildingPlacement> selectedPlacements = new();
        private readonly HashSet<BuildingPlacement> selectedSet = new();

        public IReadOnlyList<BuildingPlacement> SelectedPlacements => selectedPlacements;

        public bool Contains(BuildingPlacement placement) => selectedSet.Contains(placement);

        public void SelectRectangle(
            GridOccupancy occupancy,
            Vector2Int firstCell,
            Vector2Int lastCell,
            Func<BuildingPlacement, bool> canSelect)
        {
            if (occupancy == null)
            {
                throw new ArgumentNullException(nameof(occupancy));
            }

            if (canSelect == null)
            {
                throw new ArgumentNullException(nameof(canSelect));
            }

            Clear();
            int minX = Math.Min(firstCell.x, lastCell.x);
            int maxX = Math.Max(firstCell.x, lastCell.x);
            int minY = Math.Min(firstCell.y, lastCell.y);
            int maxY = Math.Max(firstCell.y, lastCell.y);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (occupancy.TryGetBuilding(
                            new Vector2Int(x, y), out BuildingPlacement placement) &&
                        !selectedSet.Contains(placement) && canSelect(placement))
                    {
                        selectedSet.Add(placement);
                        selectedPlacements.Add(placement);
                    }
                }
            }
        }

        public bool Remove(BuildingPlacement placement)
        {
            return selectedSet.Remove(placement) && selectedPlacements.Remove(placement);
        }

        public void Clear()
        {
            selectedSet.Clear();
            selectedPlacements.Clear();
        }
    }

    public readonly struct BuildingGroupCopyItem
    {
        public BuildingGroupCopyItem(
            BuildingPlacementOption option,
            Vector2Int cell,
            BuildingRotation rotation,
            Engraver.RecipeConfiguration? engraverRecipe = null,
            ElementInfuser.RecipeConfiguration? infuserRecipe = null)
        {
            Option = option ?? throw new ArgumentNullException(nameof(option));
            Offset = cell;
            Rotation = rotation;
            EngraverRecipe = engraverRecipe;
            InfuserRecipe = infuserRecipe;
        }

        public BuildingPlacementOption Option { get; }
        public Vector2Int Offset { get; }
        public BuildingRotation Rotation { get; }
        public Engraver.RecipeConfiguration? EngraverRecipe { get; }
        public ElementInfuser.RecipeConfiguration? InfuserRecipe { get; }
    }

    public sealed class BuildingGroupCopy
    {
        private readonly BuildingGroupCopyItem[] items;

        public BuildingGroupCopy(IReadOnlyList<BuildingGroupCopyItem> sourceItems)
        {
            if (sourceItems == null || sourceItems.Count == 0)
            {
                throw new ArgumentException("A group requires at least one building.",
                    nameof(sourceItems));
            }

            int minX = int.MaxValue;
            int minY = int.MaxValue;
            foreach (BuildingGroupCopyItem item in sourceItems)
            {
                minX = Math.Min(minX, item.Offset.x);
                minY = Math.Min(minY, item.Offset.y);
            }

            Vector2Int origin = new(minX, minY);
            items = new BuildingGroupCopyItem[sourceItems.Count];
            for (int index = 0; index < sourceItems.Count; index++)
            {
                BuildingGroupCopyItem source = sourceItems[index];
                items[index] = new BuildingGroupCopyItem(
                    source.Option,
                    source.Offset - origin,
                    source.Rotation,
                    source.EngraverRecipe,
                    source.InfuserRecipe);
            }
        }

        public IReadOnlyList<BuildingGroupCopyItem> Items => items;

        public BuildingGroupCopy RotateClockwise()
        {
            int groupWidth = 0;
            foreach (BuildingGroupCopyItem item in items)
            {
                Vector2Int footprint = item.Rotation.GetRotatedFootprint(
                    item.Option.Definition.Footprint);
                groupWidth = Math.Max(groupWidth, item.Offset.x + footprint.x);
            }

            var rotatedItems = new BuildingGroupCopyItem[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                BuildingGroupCopyItem item = items[index];
                Vector2Int footprint = item.Rotation.GetRotatedFootprint(
                    item.Option.Definition.Footprint);
                Vector2Int rotatedOffset = new(
                    item.Offset.y, groupWidth - item.Offset.x - footprint.x);
                rotatedItems[index] = new BuildingGroupCopyItem(
                    item.Option,
                    rotatedOffset,
                    item.Rotation.RotateClockwise(),
                    item.EngraverRecipe,
                    item.InfuserRecipe);
            }

            return new BuildingGroupCopy(rotatedItems);
        }

        public bool TryMirrorHorizontal(out BuildingGroupCopy mirrored)
        {
            return TryMirror(true, out mirrored);
        }

        public bool TryMirrorVertical(out BuildingGroupCopy mirrored)
        {
            return TryMirror(false, out mirrored);
        }

        private bool TryMirror(bool horizontal, out BuildingGroupCopy mirrored)
        {
            int groupExtent = 0;
            foreach (BuildingGroupCopyItem item in items)
            {
                Vector2Int footprint = item.Rotation.GetRotatedFootprint(
                    item.Option.Definition.Footprint);
                groupExtent = Math.Max(groupExtent,
                    horizontal ? item.Offset.x + footprint.x : item.Offset.y + footprint.y);

                if (item.Option.Definition.HasExplicitFootprint)
                {
                    mirrored = null;
                    return false;
                }

                BuildingRotation mirroredRotation = MirrorDirection(item.Rotation, horizontal);
                if (!CanMirrorPorts(item.Option, item.Rotation, mirroredRotation, horizontal))
                {
                    mirrored = null;
                    return false;
                }
            }

            var mirroredItems = new BuildingGroupCopyItem[items.Length];
            for (int index = 0; index < items.Length; index++)
            {
                BuildingGroupCopyItem item = items[index];
                Vector2Int footprint = item.Rotation.GetRotatedFootprint(
                    item.Option.Definition.Footprint);
                Vector2Int mirroredOffset = horizontal
                    ? new Vector2Int(groupExtent - item.Offset.x - footprint.x, item.Offset.y)
                    : new Vector2Int(item.Offset.x,
                        groupExtent - item.Offset.y - footprint.y);
                mirroredItems[index] = new BuildingGroupCopyItem(
                    item.Option,
                    mirroredOffset,
                    MirrorDirection(item.Rotation, horizontal),
                    item.EngraverRecipe,
                    item.InfuserRecipe);
            }

            mirrored = new BuildingGroupCopy(mirroredItems);
            return true;
        }

        private static bool CanMirrorPorts(
            BuildingPlacementOption option,
            BuildingRotation rotation,
            BuildingRotation mirroredRotation,
            bool horizontal)
        {
            IReadOnlyList<BuildingPortPreview> ports = option.PortPreviews;
            if (ports.Count == 0)
            {
                return option.SupportsContinuousPlacement;
            }

            var matchedPorts = new bool[ports.Count];
            foreach (BuildingPortPreview port in ports)
            {
                Vector2 position = RotatePosition(port.LocalPosition, rotation);
                Vector2 mirroredPosition = horizontal
                    ? new Vector2(-position.x, position.y)
                    : new Vector2(position.x, -position.y);
                BuildingRotation mirroredDirection = MirrorDirection(
                    port.ResolveDirection(rotation), horizontal);
                bool found = false;
                for (int index = 0; index < ports.Count; index++)
                {
                    BuildingPortPreview candidate = ports[index];
                    if (matchedPorts[index] || candidate.Kind != port.Kind ||
                        candidate.ResolveDirection(mirroredRotation) != mirroredDirection ||
                        (RotatePosition(candidate.LocalPosition, mirroredRotation) -
                            mirroredPosition).sqrMagnitude > 0.000001f)
                    {
                        continue;
                    }

                    matchedPorts[index] = true;
                    found = true;
                    break;
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        private static Vector2 RotatePosition(Vector2 position, BuildingRotation rotation)
        {
            return rotation switch
            {
                BuildingRotation.Degrees0 => position,
                BuildingRotation.Degrees90 => new Vector2(position.y, -position.x),
                BuildingRotation.Degrees180 => -position,
                BuildingRotation.Degrees270 => new Vector2(-position.y, position.x),
                _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
            };
        }

        private static BuildingRotation MirrorDirection(BuildingRotation rotation, bool horizontal)
        {
            return rotation switch
            {
                BuildingRotation.Degrees0 => horizontal
                    ? BuildingRotation.Degrees0 : BuildingRotation.Degrees180,
                BuildingRotation.Degrees90 => horizontal
                    ? BuildingRotation.Degrees270 : BuildingRotation.Degrees90,
                BuildingRotation.Degrees180 => horizontal
                    ? BuildingRotation.Degrees180 : BuildingRotation.Degrees0,
                BuildingRotation.Degrees270 => horizontal
                    ? BuildingRotation.Degrees90 : BuildingRotation.Degrees270,
                _ => throw new ArgumentOutOfRangeException(nameof(rotation), rotation, null)
            };
        }

        public bool CanPlace(Vector2Int anchorCell, GridOccupancy occupancy)
        {
            return CanPlace(anchorCell, occupancy, null);
        }

        public bool CanPlace(
            Vector2Int anchorCell,
            GridOccupancy occupancy,
            ISet<BuildingPlacement> ignoredPlacements)
        {
            if (occupancy == null)
            {
                throw new ArgumentNullException(nameof(occupancy));
            }

            var groupCells = new HashSet<Vector2Int>();
            foreach (BuildingGroupCopyItem item in items)
            {
                BuildingDefinition definition = item.Option.Definition;
                Vector2Int itemCell = anchorCell + item.Offset;
                if (!occupancy.CanPlace(definition, itemCell, item.Rotation,
                        ignoredPlacements) ||
                    (item.Option.PlacementBehavior != null &&
                        !item.Option.PlacementBehavior.CanPlace(
                            itemCell, definition.Footprint, item.Rotation)))
                {
                    return false;
                }

                var candidate = new BuildingPlacement(definition.Id, itemCell,
                    definition.Footprint, item.Rotation, definition.OccupiedCells);
                foreach (Vector2Int cell in candidate.OccupiedCells)
                {
                    if (!groupCells.Add(cell))
                    {
                        return false;
                    }
                }
            }

            return true;
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
