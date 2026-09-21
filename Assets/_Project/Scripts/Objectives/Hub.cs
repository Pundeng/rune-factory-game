using System;
using System.Linq;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    public sealed class Hub : MonoBehaviour
    {
        [SerializeField] private BeltTransportCoordinator transportCoordinator = null;
        [SerializeField] private Vector2Int inputCell = new(10, 1);
        [SerializeField] private GridDirection requiredIncomingDirection = GridDirection.East;
        [SerializeField] private ObjectiveDefinitionAsset[] objectives =
            Array.Empty<ObjectiveDefinitionAsset>();
        [SerializeField] private string currentObjectiveDebug = string.Empty;
        [SerializeField] private string lastDeliveryDebug = string.Empty;
        [SerializeField] private int accelerationRuneInventoryDebug;

        private static Sprite placeholderSprite;
        private ObjectiveProgress objectiveProgress;
        private AccelerationRuneInventory accelerationRuneInventory;
        private HubReceiver receiver;

        public ObjectiveProgress Progress => objectiveProgress;

        public string LastDeliveryMessage => lastDeliveryDebug;

        public int AccelerationRuneCount => accelerationRuneInventory?.Count ?? 0;

        public bool TryConsumeAccelerationRune()
        {
            bool consumed = accelerationRuneInventory?.TryConsume() ?? false;
            RefreshDebugState();
            return consumed;
        }

        public void Configure(
            BeltTransportCoordinator coordinator,
            Vector2Int cell,
            GridDirection incomingDirection,
            params ObjectiveDefinitionAsset[] objectiveDefinitions)
        {
            transportCoordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            inputCell = cell;
            requiredIncomingDirection = incomingDirection;
            objectives = objectiveDefinitions ?? throw new ArgumentNullException(nameof(objectiveDefinitions));
        }

        private void Awake()
        {
            if (transportCoordinator == null)
            {
                throw new MissingReferenceException("The Hub requires a Belt Transport Coordinator.");
            }

            ObjectiveDefinition[] runtimeObjectives = objectives
                .Select(objective => objective != null
                    ? objective.CreateRuntimeDefinition()
                    : throw new InvalidOperationException("Hub objectives cannot contain null assets."))
                .ToArray();
            objectiveProgress = new ObjectiveProgress(runtimeObjectives);
            accelerationRuneInventory = new AccelerationRuneInventory();
            receiver = new HubReceiver(
                inputCell,
                requiredIncomingDirection,
                objectiveProgress,
                accelerationRuneInventory);
            receiver.RuneConsumed += HandleRuneConsumed;
            transportCoordinator.RegisterInputReceiver(receiver);
            CreatePlaceholderVisual();
            RefreshDebugState();
        }

        private void HandleRuneConsumed(RuneDeliveryResult result)
        {
            lastDeliveryDebug = result switch
            {
                RuneDeliveryResult.Incorrect => "Incorrect Rune",
                RuneDeliveryResult.AllObjectivesAlreadyComplete => "MVP Objectives Complete",
                RuneDeliveryResult.CorrectAndObjectiveCompleted => "Objective Complete",
                _ => "Correct Rune"
            };
            RefreshDebugState();
        }

        private void RefreshDebugState()
        {
            accelerationRuneInventoryDebug = AccelerationRuneCount;
            if (objectiveProgress.AreAllObjectivesComplete)
            {
                currentObjectiveDebug = "MVP Objectives Complete";
                return;
            }

            ObjectiveDefinition current = objectiveProgress.CurrentObjective;
            string requirements = string.Join(
                " | ",
                current.Requirements.Select((requirement, index) =>
                    $"{requirement.TargetRune} " +
                    $"{objectiveProgress.GetCurrentCount(index)}/{requirement.RequiredCount}"));
            currentObjectiveDebug = $"{current.DisplayName} | {requirements}";
        }

        private void CreatePlaceholderVisual()
        {
            CreateVisualPart("Hub Body", Vector2.zero, new Vector2(0.85f, 0.85f),
                new Color(0.85f, 0.65f, 0.15f, 1f), 8);
            CreateVisualPart("Hub Core", Vector2.zero, new Vector2(0.38f, 0.38f),
                new Color(0.2f, 0.75f, 0.95f, 1f), 9);
            CreateVisualPart("Input Marker", new Vector2(-0.42f, 0f), new Vector2(0.12f, 0.3f),
                Color.white, 10);
        }

        private void CreateVisualPart(
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            Color color,
            int sortingOrder)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            part.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
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
                placeholderSprite.name = "Runtime Hub Placeholder";
            }

            return placeholderSprite;
        }

        private void OnDestroy()
        {
            if (receiver == null)
            {
                return;
            }

            receiver.RuneConsumed -= HandleRuneConsumed;
            transportCoordinator?.UnregisterInputReceiver(receiver);
        }
    }
}
