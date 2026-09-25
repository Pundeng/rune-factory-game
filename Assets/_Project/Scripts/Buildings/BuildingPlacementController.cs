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
using UnityEngine.Rendering;

namespace FantasyShapez.Buildings
{
    public enum DemoEscapeAction
    {
        DismissModal, CloseSystem, ClosePanel, CancelTool, ClearSelection, OpenSystem
    }

    public static class DemoEscapePriority
    {
        public static DemoEscapeAction Choose(bool modal, bool system,
            bool panel, bool tool, bool selection)
        {
            if (modal) return DemoEscapeAction.DismissModal;
            if (system) return DemoEscapeAction.CloseSystem;
            if (panel) return DemoEscapeAction.ClosePanel;
            if (tool) return DemoEscapeAction.CancelTool;
            if (selection) return DemoEscapeAction.ClearSelection;
            return DemoEscapeAction.OpenSystem;
        }
    }

    public sealed class BuildingToolRotationMemory
    {
        private readonly Dictionary<string, BuildingRotation> rotations = new(
            StringComparer.Ordinal);

        public BuildingRotation Get(string buildingId) => buildingId != null &&
            rotations.TryGetValue(buildingId, out BuildingRotation rotation)
                ? rotation : BuildingRotation.Degrees0;

        public void Set(string buildingId, BuildingRotation rotation)
        {
            if (string.IsNullOrWhiteSpace(buildingId))
                throw new ArgumentException("A building ID is required.", nameof(buildingId));
            rotations[buildingId] = rotation;
        }
    }

    public sealed class BuildingPlacementController : MonoBehaviour
    {
        public enum DemoPanel { None, Build, Recipe, Help, Market, Region, Machine, Property }
        [SerializeField] private GridSystem gridSystem = null;
        [SerializeField] private GridHoverHighlight hoverHighlight = null;
        [SerializeField] private BuildingPreview placementPreview = null;
        [SerializeField] private ObjectivePanel engraverUpgradePanel = null;
        [SerializeField] private Hub hub = null;
        [SerializeField] private Market market = null;
        [SerializeField] private bool foodDemoControls;
        [SerializeField, Tooltip("Second HUD resource line; gameplay source is not finalized.")]
        private string secondaryResourceText = "0";
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
        [SerializeField] private CuttingRecipe[] cutterRecipes =
            Array.Empty<CuttingRecipe>();
        [SerializeField, Min(0.01f)] private float cutterDuration = 1f;

        private readonly GridOccupancy occupancy = new();
        private readonly RecipeDiscoveryRegistry recipeDiscoveries = new();
        private readonly Dictionary<BuildingPlacement, PlacedBuilding> buildingInstances = new();
        private readonly Dictionary<BuildingPlacement, MachineFeedbackView> feedbackViews = new();
        private readonly EventToastQueue toasts = new();
        private readonly BeltDragPlacementPlanner beltDragPlanner = new();
        private readonly GridDragTracker placementDrag = new();
        private readonly GridDragTracker removalDrag = new();
        private BuildingPlacement pendingRightClickRemoval;
        private GameObject removalOutline;
        private Material removalOutlineMaterial;
        private readonly BuildingSelection selection = new();
        private readonly Dictionary<BuildingPlacement, GameObject> selectionHighlights = new();
        private Vector2Int? selectionStartCell;
        private GameObject selectionArea;
        private BuildingRotation selectedRotation;
        private readonly BuildingToolRotationMemory rememberedRotations = new();
        private int constructionCategory;
        private bool suppressRemovalUntilRelease;
        private bool demolishToolActive;
        private bool systemMenuOpen;
        private bool devInspectorOpen;
        private DemoPanel demoPanel;
        private Vector2 buildMenuScroll;
        private static readonly string[] HotbarBuildingIds =
        {
            nameof(FarmPlot), nameof(Belt), nameof(Harvester),
            nameof(Processor), nameof(BasicMixer), nameof(Cutter)
        };
        private int selectedBuildingIndex;
        private bool isPlacementModeActive;
        private string constructionMessage;
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
        public GridSystem GridSystem => gridSystem;
        public int CountFarmPlots(FarmableRegion region) => buildingInstances.Keys.Count(
            placement => placement.DefinitionId == nameof(FarmPlot) &&
                region.Contains(placement.AnchorCell));
        public int CountFreeFarmCells(FarmableRegion region)
        {
            int free = 0;
            for (int x = region.MinimumCell.x; x < region.MinimumCell.x + region.Size.x; x++)
                for (int y = region.MinimumCell.y; y < region.MinimumCell.y + region.Size.y; y++)
                    if (occupancy.CanPlace(new Vector2Int(x, y), Vector2Int.one,
                            BuildingRotation.Degrees0)) free++;
            return free;
        }
        public bool IsFoodDemo => foodDemoControls;
        public DemoPanel OpenDemoPanel => demoPanel;
        public bool IsSystemMenuOpen => systemMenuOpen;
        public bool IsDevInspectorOpen => devInspectorOpen;
        public bool BlocksAllWorldInput => foodDemoControls &&
            (systemMenuOpen || recipeDiscoveryPanel?.HasModal == true);
        public void OpenPanel(DemoPanel panel)
        {
            if (!foodDemoControls) return;
            if (panel == DemoPanel.None) { ClosePanel(); return; }
            if (demoPanel == panel && panel == DemoPanel.Region) return;
            if (demoPanel == panel && panel != DemoPanel.Machine)
            { ClosePanel(); return; }
            ClosePanel();
            demoPanel = panel;
            if (panel == DemoPanel.Property && propertySupply?.IsVisible == false)
                propertySupply.TogglePanel();
        }
        public void ClosePanel()
        {
            if (demoPanel == DemoPanel.Property && propertySupply?.IsVisible == true)
                propertySupply.TogglePanel();
            if (demoPanel == DemoPanel.Machine) engraverUpgradePanel?.CloseConfiguration();
            if (demoPanel == DemoPanel.Region) marketPanel?.CloseRegion();
            demoPanel = DemoPanel.None;
        }
        public bool IsPointerOverInterface => Mouse.current != null &&
            (BlocksAllWorldInput ||
             foodDemoControls && IsPointerOverDemoHud() ||
             recipeDiscoveryPanel != null && recipeDiscoveryPanel.BlocksWorldInput ||
             marketPanel != null && marketPanel.IsPointerOverPanel ||
             foodDemoControls && marketPanel?.IsPointerOverWorldObjective == true ||
             marketPanel != null && marketPanel.IsPointerOverRegionPanel ||
             engraverUpgradePanel != null && engraverUpgradePanel.IsPointerOverPanel ||
             propertySupply != null && propertySupply.IsPointerOverPanel());
        public IReadOnlyList<DiscoveredRecipe> DiscoveredRecipes =>
            recipeDiscoveries.DiscoveredRecipes;
        public RecipeDiscoveryRegistry RecipeDiscoveries => recipeDiscoveries;
        public IReadOnlyList<ProcessingRecipe> ProcessorRecipes => processorRecipes;
        public IReadOnlyList<MixingRecipe> MixerRecipes => mixerRecipes;
        public IReadOnlyList<CuttingRecipe> CutterRecipes => cutterRecipes;

        public FactoryWorldData CaptureWorldSnapshot()
        {
            if (propertySupply == null)
            {
                throw new InvalidOperationException("Factory world is not initialized.");
            }

            if (hub != null &&
                (hub.AccelerationRuneCount > 0 || hub.Progress?.HasProgress == true))
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
                    case nameof(Cutter):
                        building.cutter = instance.GetComponent<Cutter>()?.CaptureWorldState();
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
            IReadOnlyList<SavedUnlock> savedUnlocks)
        {
            if (foodDemoControls && world?.connections != null &&
                world.connections.Any(connection =>
                    connection?.kind == PropertyConnectionKind.Demand))
            {
                throw new ArgumentException(
                    "Demo cannot load developer test demands.");
            }

            FactoryWorldSnapshotValidator.ValidateAgainstScene(world, buildingOptions,
                propertySources, market.Regions, savedUnlocks, processorRecipes,
                mixerRecipes, market.InputCell, hub != null ? hub.InputCell :
                    (Vector2Int?)null, cutterRecipes);
        }

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
                else if (saved.cutter != null)
                    instance.GetComponent<Cutter>().RestoreWorldState(saved.cutter);
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
            if (hub != null && !occupancy.TryRegister(
                    nameof(Hub), hub.InputCell, hub.Footprint,
                    BuildingRotation.Degrees0, out _))
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
                occupancy, transform, propertySources, !foodDemoControls);
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
                else if (option?.Definition?.Id == nameof(Cutter))
                {
                    var cutterBehavior = gameObject.AddComponent<CutterPlacementBehavior>();
                    cutterBehavior.Configure(processorTransportCoordinator, cutterRecipes,
                        cutterDuration, recipeDiscoveries);
                    option.SetRuntimePlacementBehavior(cutterBehavior);
                }
            }
        }

        private void OnGUI()
        {
            bool originalEnabled = GUI.enabled;
            if (BlocksAllWorldInput) GUI.enabled = false;
            propertySupply?.DrawGUI();
            GUI.enabled = originalEnabled;
            if (foodDemoControls)
            {
                DrawDemoHud();
                DrawToasts();
            }
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
            if (!foodDemoControls || market == null) return;
            market.OrderSequence.Completed += OnOrderCompleted;
            market.Unlocks.UnlockedContent += OnContentUnlocked;
            market.FoodDelivered += OnFoodDelivered;
            if (marketPanel != null) marketPanel.GameSaved += OnGameSaved;
        }

        private void OnOrderCompleted(FoodOrder order) =>
            toasts.Enqueue($"Order complete: {order.DisplayName}", Time.time);

        private void OnContentUnlocked(UnlockKey unlock)
        {
            string name = unlock.Id switch
            {
                nameof(BasicMixer) => "Basic Mixer",
                _ => unlock.Id.Replace('_', ' ')
            };
            string message = unlock.Category switch
            {
                UnlockKey.MachineCategory => $"New machine: {name}",
                UnlockKey.CropCategory => $"New crop: {name}",
                UnlockKey.SeedShopCategory => $"New seed shop item: {name}",
                UnlockKey.RegionAccessCategory => $"{name} is now available",
                UnlockKey.RegionCategory => $"{name} restored",
                _ => null
            };
            if (message != null) toasts.Enqueue(message, Time.time);
        }

        private void OnFoodDelivered(FoodItemData food, int count) =>
            toasts.AddCurrency(food.SellValue, Time.time);

        private void OnGameSaved() => toasts.Enqueue("Game saved", Time.time);

        private void DrawToasts()
        {
            toasts.Prune(Time.time);
            float top = devInspectorOpen ? 132f : 8f;
            for (int index = 0; index < toasts.Active.Count; index++)
                GUI.Box(new Rect(Mathf.Max(8f, Screen.width - 225f),
                    top + index * 42f, 217f, 36f), toasts.Active[index].Text);
        }

        private void Update()
        {
            if (Keyboard.current == null || Mouse.current == null)
            {
                ClearRemovalOutline();
                return;
            }

            if (!Mouse.current.rightButton.isPressed)
            {
                if (pendingRightClickRemoval != null)
                    TryRemovePlacement(pendingRightClickRemoval);
                pendingRightClickRemoval = null;
                ClearRemovalOutline();
            }

            if (!foodDemoControls && Keyboard.current.f8Key.wasPressedThisFrame)
            {
                propertySupply?.TogglePanel();
            }
            if (foodDemoControls)
            {
                if (Keyboard.current.f3Key.wasPressedThisFrame)
                    devInspectorOpen = !devInspectorOpen;
                if (Keyboard.current.escapeKey.wasPressedThisFrame && HandleDemoEscape())
                    return;
                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    if (isGroupPasteModeActive || demolishToolActive ||
                        isPlacementModeActive)
                    {
                        if (isGroupPasteModeActive) ExitGroupPasteMode();
                        else CancelDemoTool();
                        ClearRightClickRemoval();
                        return;
                    }
                }
                if (BlocksAllWorldInput)
                {
                    ClearRightClickRemoval();
                    return;
                }
                HandleDemoHotbarShortcuts();
                if (demoPanel != DemoPanel.None && demoPanel != DemoPanel.Property &&
                    Mouse.current.leftButton.wasPressedThisFrame &&
                    !IsPointerOverDemoHud() &&
                    !(demoPanel == DemoPanel.Region && marketPanel?.IsPointerOverRegionPanel == true))
                {
                    ClosePanel();
                    return;
                }
                if (Mouse.current.leftButton.wasPressedThisFrame &&
                    !isPlacementModeActive && !demolishToolActive &&
                    propertySupply?.IsActive != true &&
                    propertySupply?.IsPointerOverPanel() != true &&
                    marketPanel?.TryOpenAt(hoverHighlight.HoveredCell) == true)
                {
                    OpenPanel(DemoPanel.Market);
                    return;
                }
            }

            if (isPlacementModeActive && Mouse.current.rightButton.wasPressedThisFrame &&
                IsPointerOverInterface)
            {
                isPlacementModeActive = false;
                suppressRemovalUntilRelease = true;
                placementPreview.Hide();
                placementDrag.Reset();
                beltDragPlanner.Reset();
                return;
            }

            if (IsPointerOverInterface)
            {
                ClearRightClickRemoval();
                if (isPlacementModeActive && Keyboard.current.escapeKey.wasPressedThisFrame)
                    isPlacementModeActive = false;
                else if (isPlacementModeActive && Keyboard.current.rKey.wasPressedThisFrame)
                {
                    selectedRotation = selectedRotation.RotateClockwise();
                    RememberRotation(GetSelectedOption(), selectedRotation);
                }
                placementPreview.Hide();
                placementDrag.Reset();
                removalDrag.Reset();
                return;
            }

            if (propertySupply != null &&
                (propertySupply.IsActive || propertySupply.IsPointerOverPanel()))
            {
                ClearRightClickRemoval();
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

            if (Mouse.current.leftButton.wasPressedThisFrame && marketPanel != null &&
                (!isPlacementModeActive ||
                    GetSelectedOption()?.Definition?.Id == nameof(FarmPlot)) &&
                marketPanel.TrySelectRegionAt(hoverHighlight.HoveredCell,
                    isPlacementModeActive, occupancy.TryGetBuilding(
                        hoverHighlight.HoveredCell, out _)))
            {
                if (foodDemoControls) OpenPanel(DemoPanel.Region);
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
            placementPreview.SetReason(canPlace ? null :
                GetPlacementFailureReason(selectedOption, anchorCell, previewRotation));

            HandlePlacementInput(selectedOption, anchorCell, canPlace);
        }

        private void LateUpdate()
        {
            foreach (KeyValuePair<BuildingPlacement, MachineFeedbackView> entry in feedbackViews)
            {
                if (!buildingInstances.TryGetValue(entry.Key, out PlacedBuilding building))
                    continue;
                MachineFeedback feedback;
                if (building.TryGetComponent(out Processor processor))
                {
                    feedback = MachineFeedbackResolver.Processor(
                        processor.HasOutput &&
                        processorTransportCoordinator?.CanAcceptOutput(processor.OutputCell) != true,
                        processor.HasRecentInvalidRecipe,
                        processor.ProcessingStateMessage.Contains("property supply") ||
                        processor.ProcessingStateMessage.Contains("supplied property changed"),
                        processor.State == ProcessorState.Idle);
                }
                else if (building.TryGetComponent(out BasicMixer mixer))
                {
                    feedback = MachineFeedbackResolver.Mixer(
                        mixer.HasOutput &&
                        processorTransportCoordinator?.CanAcceptOutput(mixer.OutputCell) != true,
                        mixer.HasRecentInvalidRecipe,
                        !mixer.HasOutput && mixer.SlotA == null,
                        !mixer.HasOutput && mixer.SlotB == null);
                }
                else if (building.TryGetComponent(out Cutter cutter))
                {
                    bool outputNeeded = cutter.State != CutterState.Idle;
                    feedback = MachineFeedbackResolver.Cutter(
                        outputNeeded && processorTransportCoordinator?.CanAcceptOutput(
                            cutter.OutputACell) != true,
                        outputNeeded && processorTransportCoordinator?.CanAcceptOutput(
                            cutter.OutputBCell) != true,
                        cutter.HasRecentInvalidRecipe, cutter.State == CutterState.Idle);
                }
                else if (building.TryGetComponent(out Harvester harvester))
                {
                    feedback = MachineFeedbackResolver.Harvester(
                        harvester.HasOutput &&
                        processorTransportCoordinator?.CanAcceptOutput(harvester.OutputCell) != true,
                        harvester.ConnectedFarmPlot?.SelectedCrop == null);
                }
                else continue;
                bool hovered = occupancy.TryGetBuilding(hoverHighlight.HoveredCell,
                    out BuildingPlacement over) && over == entry.Key &&
                    !IsPointerOverInterface;
                entry.Value.SetFeedback(feedback, hovered, BlocksAllWorldInput);
            }
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

            if (!foodDemoControls && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SelectBuilding(0);
            }

            if (!foodDemoControls && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SelectBuilding(1);
            }

            if (!foodDemoControls && Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SelectBuilding(2);
            }

            if (!foodDemoControls && Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                SelectBuilding(3);
            }

            if (!foodDemoControls && Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                SelectBuilding(4);
            }

            if (!foodDemoControls && Keyboard.current.digit6Key.wasPressedThisFrame)
            {
                SelectBuilding(5);
            }

            if (!foodDemoControls && Keyboard.current.digit7Key.wasPressedThisFrame)
            {
                SelectBuilding(6);
            }

            if (!foodDemoControls && Keyboard.current.digit8Key.wasPressedThisFrame)
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
                selectedRotation = GetRememberedRotation(GetSelectedOption());
                ClearCopiedRecipe();
                beltDragPlanner.Reset();
                placementDrag.Reset();
            }

            if (!foodDemoControls && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                isPlacementModeActive = false;
                placementPreview.Hide();
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

            if (!foodDemoControls && Mouse.current.rightButton.wasPressedThisFrame)
            {
                isPlacementModeActive = false;
                suppressRemovalUntilRelease = true;
                placementPreview.Hide();
                placementDrag.Reset();
                beltDragPlanner.Reset();
                return;
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                selectedRotation = selectedRotation.RotateClockwise();
                RememberRotation(GetSelectedOption(), selectedRotation);
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
            if (foodDemoControls) demolishToolActive = false;
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
                feedbackViews.Remove(source);
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
                    feedbackViews.Remove(placed);
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
            if (foodDemoControls)
            {
                if (demolishToolActive)
                {
                    ClearRightClickRemoval();
                    if (isPlacementModeActive || !Mouse.current.leftButton.isPressed)
                    {
                        removalDrag.Reset();
                        return;
                    }
                    if (Mouse.current.leftButton.wasPressedThisFrame || removalDrag.IsActive)
                        foreach (Vector2Int cell in removalDrag.Continue(hoverHighlight.HoveredCell))
                            TryRemoveBuilding(cell);
                    return;
                }
            }
            if (suppressRemovalUntilRelease || isPlacementModeActive)
            {
                if (!Mouse.current.rightButton.isPressed)
                    suppressRemovalUntilRelease = false;
                removalDrag.Reset();
                ClearRightClickRemoval();
                return;
            }
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
                if (occupancy.TryGetBuilding(cell, out BuildingPlacement placement) &&
                    buildingInstances.TryGetValue(placement, out PlacedBuilding instance) &&
                    CanRemove(instance.gameObject) && placement != pendingRightClickRemoval)
                {
                    if (pendingRightClickRemoval != null)
                        TryRemovePlacement(pendingRightClickRemoval);
                    pendingRightClickRemoval = placement;
                    ShowRemovalOutline(placement);
                }

            if (!occupancy.TryGetBuilding(hoverHighlight.HoveredCell,
                    out BuildingPlacement hovered) || hovered != pendingRightClickRemoval)
            {
                if (pendingRightClickRemoval != null)
                    TryRemovePlacement(pendingRightClickRemoval);
                pendingRightClickRemoval = null;
                ClearRemovalOutline();
            }
        }

        private void ClearRightClickRemoval()
        {
            pendingRightClickRemoval = null;
            removalDrag.Reset();
            ClearRemovalOutline();
        }

        private void ClearRemovalOutline()
        {
            if (removalOutline != null)
            {
                Destroy(removalOutline);
                removalOutline = null;
            }
        }

        private void OnDestroy()
        {
            if (foodDemoControls && market != null)
            {
                if (market.OrderSequence != null)
                    market.OrderSequence.Completed -= OnOrderCompleted;
                market.Unlocks.UnlockedContent -= OnContentUnlocked;
                market.FoodDelivered -= OnFoodDelivered;
                if (marketPanel != null) marketPanel.GameSaved -= OnGameSaved;
            }
            ClearRemovalOutline();
            if (removalOutlineMaterial != null)
                Destroy(removalOutlineMaterial);
        }

        private void ShowRemovalOutline(BuildingPlacement placement)
        {
            ClearRemovalOutline();
            removalOutline = new GameObject("Right-click removal outline");
            removalOutline.transform.SetParent(transform, false);
            if (removalOutlineMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null)
                    removalOutlineMaterial = new Material(shader);
            }
            var occupied = new HashSet<Vector2Int>(placement.OccupiedCells);
            Vector2Int[] directions =
            {
                Vector2Int.left, Vector2Int.up, Vector2Int.right, Vector2Int.down
            };
            foreach (Vector2Int cell in placement.OccupiedCells)
            {
                Vector3 center = gridSystem.GridToWorld(cell);
                float half = gridSystem.CellSize * 0.5f;
                for (int edge = 0; edge < directions.Length; edge++)
                {
                    if (occupied.Contains(cell + directions[edge])) continue;
                    var lineObject = new GameObject("Outline edge");
                    lineObject.transform.SetParent(removalOutline.transform, false);
                    LineRenderer line = lineObject.AddComponent<LineRenderer>();
                    line.sharedMaterial = removalOutlineMaterial;
                    line.useWorldSpace = true;
                    line.positionCount = 2;
                    line.startWidth = 0.07f * gridSystem.CellSize;
                    line.endWidth = line.startWidth;
                    line.startColor = Color.red;
                    line.endColor = Color.red;
                    line.sortingOrder = 110;
                    line.shadowCastingMode = ShadowCastingMode.Off;
                    line.receiveShadows = false;
                    Vector3 a = edge switch
                    {
                        0 => new Vector3(-half, -half, -0.08f),
                        1 => new Vector3(-half, half, -0.08f),
                        2 => new Vector3(half, half, -0.08f),
                        _ => new Vector3(half, -half, -0.08f)
                    };
                    Vector3 b = edge switch
                    {
                        0 => new Vector3(-half, half, -0.08f),
                        1 => new Vector3(half, half, -0.08f),
                        2 => new Vector3(half, -half, -0.08f),
                        _ => new Vector3(-half, -half, -0.08f)
                    };
                    line.SetPosition(0, center + a);
                    line.SetPosition(1, center + b);
                }
            }
        }

        private bool HandleSelectionInput()
        {
            if (foodDemoControls && demolishToolActive) return false;
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
                feedbackViews.Remove(placement);
                selection.Remove(placement);
                RefreshSelectionHighlights();
                Destroy(instance.gameObject);
            }
        }

        private void HandleInteractionInput()
        {
            if (isPlacementModeActive || demolishToolActive ||
                !Mouse.current.leftButton.wasPressedThisFrame ||
                !occupancy.TryGetBuilding(hoverHighlight.HoveredCell, out BuildingPlacement placement) ||
                !buildingInstances.TryGetValue(placement, out PlacedBuilding instance))
            {
                return;
            }

            if (foodDemoControls && feedbackViews.TryGetValue(placement,
                    out MachineFeedbackView feedbackView) &&
                feedbackView.Feedback.HasProblem)
            {
                Vector2Int? traceCell = GetFeedbackTraceCell(placement,
                    instance, feedbackView.Feedback.Ports);
                Vector3? tracePosition = traceCell.HasValue &&
                    occupancy.TryGetBuilding(traceCell.Value, out BuildingPlacement connected) &&
                    connected != placement
                    ? gridSystem.GridToWorld(traceCell.Value) : null;
                feedbackView.Emphasize(tracePosition);
                if (instance.GetComponent<Harvester>() == null ||
                    feedbackView.Feedback.Ports != MachineFeedbackPort.Crop)
                    return;
            }

            foreach (MonoBehaviour component in instance.GetComponents<MonoBehaviour>())
            {
                if (component is FantasyShapez.Production.Engraver engraver)
                {
                    if (foodDemoControls) OpenPanel(DemoPanel.Machine);
                    engraverUpgradePanel?.ShowEngraver(engraver);
                    return;
                }

                if (component is FantasyShapez.Production.ElementInfuser infuser)
                {
                    if (foodDemoControls) OpenPanel(DemoPanel.Machine);
                    engraverUpgradePanel?.ShowElementInfuser(infuser);
                    return;
                }

                if (component is FantasyShapez.Food.FarmPlot farmPlot)
                {
                    if (foodDemoControls) OpenPanel(DemoPanel.Machine);
                    engraverUpgradePanel?.ShowFarmPlot(farmPlot);
                    return;
                }

                if (component is FantasyShapez.Food.Harvester harvester)
                {
                    if (foodDemoControls) OpenPanel(DemoPanel.Machine);
                    engraverUpgradePanel?.ShowHarvester(harvester);
                    return;
                }

                if (component is Processor processor)
                {
                    if (foodDemoControls) return;
                    engraverUpgradePanel?.ShowProcessor(processor);
                    return;
                }

                if (component is BasicMixer mixer)
                {
                    if (foodDemoControls) return;
                    engraverUpgradePanel?.ShowMixer(mixer);
                    return;
                }
                if (component is Cutter cutter)
                {
                    if (foodDemoControls) return;
                    engraverUpgradePanel?.ShowCutter(cutter);
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
                MachineFeedbackView view = instance.GetComponent<MachineFeedbackView>();
                if (view != null) feedbackViews.Add(placement, view);
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
                BuildingVisualFactory.CreatePortMarkers(buildingObject.transform,
                    option.PortPreviews, gridSystem.CellSize, placement.Rotation);
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
                if (buildingObject.GetComponent<Processor>() != null ||
                    buildingObject.GetComponent<BasicMixer>() != null ||
                    buildingObject.GetComponent<Cutter>() != null ||
                    buildingObject.GetComponent<Harvester>() != null)
                    BuildingVisualFactory.CreateFeedbackView(buildingObject.transform,
                        gridSystem.CellSize);
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

            if (IsMachineLocked(buildingOptions[index]))
            {
                constructionMessage = $"{buildingOptions[index].Definition.Id} " +
                    $"requires {GetMachineUnlockRequirement(buildingOptions[index])}.";
                return;
            }

            constructionMessage = null;

            selectedBuildingIndex = index;
            if (foodDemoControls)
            {
                ClosePanel();
                if (isGroupPasteModeActive) ExitGroupPasteMode();
                demolishToolActive = false;
                propertySupply?.ExitTool();
            }
            selectedRotation = GetRememberedRotation(buildingOptions[index]);
            ClearCopiedRecipe();
            isPlacementModeActive = true;
            beltDragPlanner.Reset();
            placementDrag.Reset();
        }

        private bool HandleDemoEscape()
        {
            DemoEscapeAction action = DemoEscapePriority.Choose(
                recipeDiscoveryPanel?.HasModal == true, systemMenuOpen,
                demoPanel != DemoPanel.None,
                isGroupPasteModeActive || demolishToolActive ||
                isPlacementModeActive || propertySupply?.IsActive == true,
                selectionStartCell.HasValue || selection.SelectedPlacements.Count > 0);
            switch (action)
            {
                case DemoEscapeAction.DismissModal:
                    recipeDiscoveryPanel.DismissModal();
                    break;
                case DemoEscapeAction.CloseSystem:
                    systemMenuOpen = false;
                    break;
                case DemoEscapeAction.ClosePanel:
                    ClosePanel();
                    break;
                case DemoEscapeAction.CancelTool:
                    if (isGroupPasteModeActive) ExitGroupPasteMode();
                    else CancelDemoTool();
                    break;
                case DemoEscapeAction.ClearSelection:
                    selectionStartCell = null;
                    selection.Clear();
                    RefreshSelectionHighlights();
                    HideSelectionArea();
                    break;
                default:
                    systemMenuOpen = true;
                    break;
            }
            return true;
        }

        private void CancelDemoTool()
        {
            demolishToolActive = false;
            isPlacementModeActive = false;
            propertySupply?.ExitTool();
            placementPreview.Hide();
            placementDrag.Reset();
            removalDrag.Reset();
            beltDragPlanner.Reset();
        }

        private void HandleDemoHotbarShortcuts()
        {
            if (Keyboard.current.ctrlKey.isPressed || isGroupPasteModeActive) return;
            int slot = Keyboard.current.digit1Key.wasPressedThisFrame ? 0 :
                Keyboard.current.digit2Key.wasPressedThisFrame ? 1 :
                Keyboard.current.digit3Key.wasPressedThisFrame ? 2 :
                Keyboard.current.digit4Key.wasPressedThisFrame ? 3 :
                Keyboard.current.digit5Key.wasPressedThisFrame ? 4 :
                Keyboard.current.digit6Key.wasPressedThisFrame ? 5 : -1;
            if (slot >= 0) SelectBuilding(FindBuildingOption(HotbarBuildingIds[slot]));
        }

        private Vector2Int? GetFeedbackTraceCell(BuildingPlacement placement,
            PlacedBuilding building, MachineFeedbackPort ports)
        {
            Vector2Int anchor = placement.AnchorCell;
            BuildingRotation rotation = placement.Rotation;
            if (building.TryGetComponent(out Processor processor))
            {
                if ((ports & MachineFeedbackPort.OutputA) != 0) return processor.OutputCell;
                if ((ports & MachineFeedbackPort.Property) != 0)
                    return ProcessorPortLayout.GetPropertyOutsideCell(anchor, rotation);
                if ((ports & MachineFeedbackPort.InputA) != 0)
                    return ProcessorPortLayout.GetFoodInputOutsideCell(anchor, rotation);
            }
            if (building.TryGetComponent(out BasicMixer mixer))
            {
                if ((ports & MachineFeedbackPort.OutputA) != 0) return mixer.OutputCell;
                if ((ports & MachineFeedbackPort.Combination) != 0)
                {
                    Vector2Int first = BasicMixerPortLayout.GetInputOutsideCell(
                        anchor, rotation, 0);
                    return occupancy.TryGetBuilding(first, out _) ? first :
                        BasicMixerPortLayout.GetInputOutsideCell(anchor, rotation, 1);
                }
                if ((ports & MachineFeedbackPort.InputA) != 0)
                    return BasicMixerPortLayout.GetInputOutsideCell(anchor, rotation, 0);
                if ((ports & MachineFeedbackPort.InputB) != 0)
                    return BasicMixerPortLayout.GetInputOutsideCell(anchor, rotation, 1);
            }
            if (building.TryGetComponent(out Cutter cutter))
            {
                if ((ports & MachineFeedbackPort.OutputA) != 0) return cutter.OutputACell;
                if ((ports & MachineFeedbackPort.OutputB) != 0) return cutter.OutputBCell;
                if ((ports & MachineFeedbackPort.InputA) != 0)
                    return cutter.InputCell + CutterPortLayout.InputFacing(rotation).ToOffset();
            }
            if (building.TryGetComponent(out Harvester harvester))
            {
                if ((ports & MachineFeedbackPort.OutputA) != 0) return harvester.OutputCell;
                if ((ports & MachineFeedbackPort.Crop) != 0) return harvester.FarmCell;
            }
            return null;
        }

        private int FindBuildingOption(string id)
        {
            for (int index = 0; index < buildingOptions.Length; index++)
                if (buildingOptions[index]?.Definition?.Id == id) return index;
            return -1;
        }

        private Rect HotbarRect => new(
            Mathf.Max(8f, (Screen.width - Mathf.Min(424f, Screen.width - 16f)) * 0.5f),
            Mathf.Max(8f, Screen.height - 56f),
            Mathf.Min(424f, Screen.width - 16f), 44f);
        private Rect UtilityRect => new(
            Mathf.Max(8f, Screen.width - 156f),
            Mathf.Max(8f, Screen.height - 106f), 148f, 42f);
        private Rect DemoPanelRect
        {
            get
            {
                float height = Mathf.Max(80f,
                    Mathf.Min(300f, Screen.height - 164f));
                return new Rect(
                    Mathf.Max(8f, (Screen.width - Mathf.Min(460f, Screen.width - 16f)) * 0.5f),
                    Mathf.Max(8f, Screen.height - height - 114f),
                    Mathf.Min(460f, Screen.width - 16f), height);
            }
        }
        private Rect SystemRect => new(
            (Screen.width - Mathf.Min(280f, Screen.width - 16f)) * 0.5f,
            (Screen.height - Mathf.Min(300f, Screen.height - 16f)) * 0.5f,
            Mathf.Min(280f, Screen.width - 16f),
            Mathf.Min(300f, Screen.height - 16f));

        private bool IsPointerOverDemoHud()
        {
            if (!foodDemoControls || Mouse.current == null) return false;
            Vector2 pointer = Mouse.current.position.ReadValue();
            pointer.y = Screen.height - pointer.y;
            return systemMenuOpen || HotbarRect.Contains(pointer) ||
                UtilityRect.Contains(pointer) ||
                (demoPanel is DemoPanel.Build or DemoPanel.Help) && DemoPanelRect.Contains(pointer) ||
                demoPanel == DemoPanel.Recipe && recipeDiscoveryPanel?.BlocksWorldInput == true ||
                demoPanel == DemoPanel.Market && marketPanel?.IsPointerOverPanel == true ||
                demoPanel == DemoPanel.Machine && engraverUpgradePanel?.IsPointerOverPanel == true ||
                demoPanel == DemoPanel.Property && propertySupply?.IsPointerOverPanel() == true ||
                demoPanel == DemoPanel.Region && marketPanel?.IsPointerOverRegionPanel == true;
        }

        private void DrawDemoHud()
        {
            if (market == null) return;
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !BlocksAllWorldInput;
            GUI.Label(new Rect(12f, 8f, 180f, 22f), $"$ {market.Currency}");
            GUI.Label(new Rect(12f, 28f, 180f, 22f), secondaryResourceText);
            Vector2Int regionCell = Camera.main != null
                ? gridSystem.WorldToGrid(Camera.main.transform.position)
                : hoverHighlight.HoveredCell;
            FarmableRegion region = market.Regions?.GetRegionAt(regionCell);
            GUI.Label(new Rect(12f, 48f, 180f, 22f),
                region?.DisplayName ?? market.Regions?.Regions.FirstOrDefault()?.DisplayName ?? "");

            Rect hotbar = HotbarRect;
            float slotWidth = (hotbar.width - 48f) / HotbarBuildingIds.Length;
            string hoveredName = null;
            Vector2 hotbarPointer = Mouse.current?.position.ReadValue() ?? Vector2.zero;
            hotbarPointer.y = Screen.height - hotbarPointer.y;
            for (int slot = 0; slot < HotbarBuildingIds.Length; slot++)
            {
                int optionIndex = FindBuildingOption(HotbarBuildingIds[slot]);
                BuildingPlacementOption option = optionIndex >= 0
                    ? buildingOptions[optionIndex] : null;
                if (option?.Definition == null) continue;
                Rect cell = new(hotbar.x + slot * slotWidth, hotbar.y,
                    slotWidth - 2f, hotbar.height);
                if (cell.Contains(hotbarPointer)) hoveredName = option.Definition.Id;
                bool slotEnabled = GUI.enabled;
                GUI.enabled = slotEnabled && !IsMachineLocked(option);
                if (GUI.Button(cell, new GUIContent("", option.Definition.Id)))
                    SelectBuilding(optionIndex);
                GUI.enabled = slotEnabled;
                GUI.Label(new Rect(cell.x + 3f, cell.y + 2f, 18f, 18f),
                    (slot + 1).ToString());
                Color originalColor = GUI.backgroundColor;
                GUI.backgroundColor = option.Definition.PlacedColor;
                GUI.Box(new Rect(cell.center.x - 13f, cell.y + 17f, 26f, 22f),
                    option.Definition.Id.Substring(0, 2).ToUpperInvariant());
                GUI.backgroundColor = originalColor;
                if (isPlacementModeActive && selectedBuildingIndex == optionIndex)
                    GUI.Box(new Rect(cell.x, cell.y, cell.width, 3f), GUIContent.none);
            }
            if (GUI.Button(new Rect(hotbar.xMax - 46f, hotbar.y, 44f, hotbar.height), "+"))
                OpenPanel(DemoPanel.Build);
            if (!string.IsNullOrEmpty(hoveredName))
                GUI.Label(new Rect(hotbar.x, hotbar.y - 62f, hotbar.width, 20f),
                    hoveredName);
            if (isPlacementModeActive || demolishToolActive)
            {
                string name = demolishToolActive ? "Demolish" :
                    GetSelectedOption()?.Definition?.Id ?? "Build";
                GUI.Label(new Rect(hotbar.x, hotbar.y - 42f, hotbar.width, 40f),
                    $"{name}  |  {(isPlacementModeActive ? "R Rotate  ·  " : "")}Esc / Right Click Cancel");
            }
            else if (!string.IsNullOrEmpty(constructionMessage))
                GUI.Label(new Rect(hotbar.x, hotbar.y - 42f, hotbar.width, 40f),
                    constructionMessage);

            Rect utility = UtilityRect;
            if (GUI.Button(new Rect(utility.x, utility.y, 46f, 38f), "Recipe"))
                OpenPanel(DemoPanel.Recipe);
            if (GUI.Button(new Rect(utility.x + 49f, utility.y, 46f, 38f), "Help"))
                OpenPanel(DemoPanel.Help);
            if (GUI.Button(new Rect(utility.x + 98f, utility.y, 46f, 38f), "System"))
            {
                ClosePanel();
                systemMenuOpen = true;
            }

            if (demoPanel == DemoPanel.Build) DrawBuildMenu();
            if (demoPanel == DemoPanel.Help) DrawHelpPanel();
            GUI.enabled = previousEnabled;
            if (systemMenuOpen)
            {
                GUI.enabled = previousEnabled && recipeDiscoveryPanel?.HasModal != true;
                DrawSystemMenu();
            }
            if (devInspectorOpen)
                GUI.Box(new Rect(Mathf.Max(8f, Screen.width - 224f), 8f, 216f, 116f),
                    $"DEV INSPECTOR\nGrid: {hoverHighlight.HoveredCell}\nTool: " +
                    (demolishToolActive ? "Demolish" : isPlacementModeActive ?
                        GetSelectedOption()?.Definition?.Id : "None") +
                    $"\n{GetHoveredMachineDebug()}");
            GUI.enabled = previousEnabled;
        }

        private string GetHoveredMachineDebug()
        {
            if (!occupancy.TryGetBuilding(hoverHighlight.HoveredCell,
                    out BuildingPlacement placement) ||
                !buildingInstances.TryGetValue(placement, out PlacedBuilding building))
                return "Machine: none";
            if (building.TryGetComponent(out Processor processor))
                return $"Processor: {processor.State}\n{processor.ProcessingStateMessage}";
            if (building.TryGetComponent(out BasicMixer mixer))
                return $"Mixer A: {mixer.SlotA?.Id ?? "empty"}\n" +
                    $"Mixer B: {mixer.SlotB?.Id ?? "empty"}";
            if (building.TryGetComponent(out Cutter cutter))
                return $"Cutter: {cutter.State}";
            if (building.TryGetComponent(out Harvester harvester))
                return $"Harvester buffer: {harvester.OutputCount}/{harvester.OutputCapacity}";
            return "Machine: none";
        }

        private void DrawBuildMenu()
        {
            Rect rect = DemoPanelRect;
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f,
                rect.width - 20f, rect.height - 16f));
            GUILayout.Label("Build Menu");
            GUILayout.BeginHorizontal();
            string[] categories = { "Farming", "Logistics", "Production", "Utility" };
            for (int category = 0; category < categories.Length; category++)
                if (GUILayout.Button(categories[category])) constructionCategory = category;
            GUILayout.EndHorizontal();
            buildMenuScroll = GUILayout.BeginScrollView(buildMenuScroll);
            for (int index = 0; index < buildingOptions.Length; index++)
            {
                BuildingPlacementOption option = buildingOptions[index];
                if (option?.Definition == null ||
                    GetConstructionCategory(option.Definition.Id) != constructionCategory) continue;
                bool locked = IsMachineLocked(option);
                bool enabled = GUI.enabled;
                GUI.enabled = enabled && !locked;
                if (GUILayout.Button(option.Definition.Id)) SelectBuilding(index);
                GUI.enabled = enabled;
                if (locked) GUILayout.Label($"Requires {GetMachineUnlockRequirement(option)}");
            }
            if (constructionCategory == 1 && GUILayout.Button("Demolish"))
            {
                ClosePanel();
                if (isGroupPasteModeActive) ExitGroupPasteMode();
                propertySupply?.ExitTool();
                isPlacementModeActive = false;
                demolishToolActive = true;
            }
            if (constructionCategory == 3 && GUILayout.Button("Property connections"))
                OpenPanel(DemoPanel.Property);
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawHelpPanel()
        {
            Rect rect = DemoPanelRect;
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 8f,
                rect.width - 20f, rect.height - 16f));
            GUILayout.Label("Help");
            GUILayout.Label("Choose a building from the hotbar or Build Menu.");
            GUILayout.Label("R rotates. Esc or Right Click cancels the current tool.");
            GUILayout.Label("Hold Right Click over a building to remove it; drag to remove several.");
            GUILayout.Label("Click Market to view the current order.");
            GUILayout.EndArea();
        }

        private void DrawSystemMenu()
        {
            Rect rect = SystemRect;
            GUI.Box(rect, GUIContent.none);
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 10f,
                rect.width - 24f, rect.height - 20f));
            GUILayout.Label("SYSTEM");
            if (GUILayout.Button("Resume")) systemMenuOpen = false;
            if (GUILayout.Button("Save")) marketPanel?.SaveGame();
            if (GUILayout.Button("Load")) marketPanel?.LoadGame();
            GUI.enabled = false;
            GUILayout.Button("Settings");
            GUI.enabled = true;
            if (GUILayout.Button("Quit")) Application.Quit();
            if (!string.IsNullOrEmpty(marketPanel?.SaveMessage))
                GUILayout.Label(marketPanel.SaveMessage);
            GUILayout.EndArea();
        }

        private static int GetConstructionCategory(string id) => id switch
        {
            nameof(FarmPlot) or nameof(Harvester) => 0,
            nameof(Belt) => 1,
            nameof(Processor) or nameof(BasicMixer) or nameof(Cutter) => 2,
            _ => 2
        };

        private BuildingRotation GetRememberedRotation(BuildingPlacementOption option) =>
            rememberedRotations.Get(option?.Definition?.Id);

        private void RememberRotation(BuildingPlacementOption option,
            BuildingRotation rotation)
        {
            if (option?.Definition != null)
                rememberedRotations.Set(option.Definition.Id, rotation);
        }

        private string GetPlacementFailureReason(BuildingPlacementOption option,
            Vector2Int anchor, BuildingRotation rotation)
        {
            BuildingDefinition definition = option.Definition;
            if (definition.Id == nameof(FarmPlot))
            {
                FarmableRegion region = market?.Regions?.GetRegionAt(anchor);
                if (region == null) return "Non-farmable land";
                if (market.Regions.GetStatus(region.Id) != RegionStatus.Restored)
                    return "Locked region";
            }
            if (option.PlacementBehavior is HarvesterPlacementBehavior)
            {
                if (!TryGetHarvesterFarmPlacement(option, anchor, rotation,
                        out Vector2Int farmCell, out BuildingPlacement farmPlacement))
                    return "Harvester needs a Farm Plot";
                return occupancy.CanPlaceOver(anchor, definition.Footprint, rotation,
                    farmCell, farmPlacement) ? "Placement unavailable" :
                    "Harvester's free cell is occupied";
            }
            if (!occupancy.CanPlace(definition, anchor, rotation))
                return "Occupied cell";
            return "Placement requirements not met";
        }

        private bool IsMachineLocked(BuildingPlacementOption option) =>
            foodDemoControls && option?.Definition != null &&
            option.Definition.Id is nameof(Processor) or nameof(BasicMixer) or
                nameof(Cutter) &&
            market?.Unlocks.IsUnlocked(UnlockKey.MachineCategory,
                option.Definition.Id) != true;

        private string GetMachineUnlockRequirement(BuildingPlacementOption option)
        {
            string id = option?.Definition?.Id;
            foreach (FoodOrder order in market?.Orders ?? Array.Empty<FoodOrder>())
                if (order.Unlocks.Any(unlock =>
                        unlock.Category == UnlockKey.MachineCategory && unlock.Id == id))
                    return order.DisplayName;
            return "Market progression";
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
                RememberRotation(option, selectedRotation);
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
