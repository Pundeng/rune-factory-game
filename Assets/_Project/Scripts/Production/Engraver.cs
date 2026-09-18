using System;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class Engraver : MonoBehaviour, IBuildingRemovalRule
    {
        [SerializeField] private GlyphType selectedGlyph = GlyphType.Attack;
        [SerializeField, Min(0.01f)] private float processingDuration = 1.5f;
        [Header("Acceleration Upgrade")]
        [SerializeField, Min(1)] private int requiredAccelerationRunes = 100;
        [SerializeField, Min(1.01f)] private float upgradedSpeedMultiplier = 2f;
        [SerializeField] private int accelerationUpgradeProgress;
        [SerializeField] private bool accelerationUpgradeApplied;
        [SerializeField] private EngraverState state;

        private static Sprite placeholderSprite;
        private BeltTransportCoordinator transportCoordinator;
        private EngraverProcess process;
        private SpriteRenderer stateIndicator;
        private EngraverState? lastVisualState;

        public bool CanRemove => true;

        public void Initialize(
            Vector2Int anchorCell,
            GridDirection direction,
            BeltTransportCoordinator coordinator)
        {
            transportCoordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            process = new EngraverProcess(
                anchorCell,
                direction,
                selectedGlyph,
                processingDuration,
                requiredAccelerationRunes,
                upgradedSpeedMultiplier);
            transportCoordinator.RegisterInputReceiver(process);
            transportCoordinator.RegisterOutputSource(process);
            CreateDirectionArrow();
            CreateStateIndicator();
            RefreshUpgradeDebugState();
            RefreshVisualState();
        }

        private void Update()
        {
            if (process == null)
            {
                return;
            }

            process.Configure(selectedGlyph, processingDuration);
            process.ConfigureUpgrade(requiredAccelerationRunes, upgradedSpeedMultiplier);
            bool completed = process.Advance(Time.deltaTime);
            if (completed && process.LastEngravingSucceeded == false)
            {
                Debug.LogWarning(
                    $"Engraver could not add {process.ActiveGlyph}; the rune will pass through unchanged.",
                    this);
            }

            RefreshUpgradeDebugState();
            RefreshVisualState();
        }

        private void RefreshUpgradeDebugState()
        {
            accelerationUpgradeProgress = process?.AccelerationUpgradeProgress ?? 0;
            accelerationUpgradeApplied = process?.IsAccelerationUpgraded ?? false;
        }

        private void RefreshVisualState()
        {
            state = process?.State ?? EngraverState.Idle;
            if (lastVisualState == state)
            {
                return;
            }

            Color color = state switch
            {
                EngraverState.Processing => new Color(0.75f, 0.3f, 0.9f, 1f),
                EngraverState.WaitingForOutput => new Color(0.95f, 0.65f, 0.2f, 1f),
                _ => new Color(0.25f, 0.7f, 0.85f, 1f)
            };

            stateIndicator.color = color;
            lastVisualState = state;
        }

        private void CreateDirectionArrow()
        {
            CreateVisualPart("Output Arrow Shaft", new Vector2(0f, 0.18f), new Vector2(0.08f, 0.35f), 0f);
            CreateVisualPart("Output Arrow Left", new Vector2(-0.09f, 0.31f), new Vector2(0.08f, 0.22f), -45f);
            CreateVisualPart("Output Arrow Right", new Vector2(0.09f, 0.31f), new Vector2(0.08f, 0.22f), 45f);
        }

        private void CreateStateIndicator()
        {
            stateIndicator = CreateVisualPart(
                "State Indicator",
                Vector2.zero,
                Vector2.one * 0.25f,
                45f);
        }

        private SpriteRenderer CreateVisualPart(
            string partName,
            Vector2 position,
            Vector2 scale,
            float angle)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(transform, false);
            part.transform.localPosition = new Vector3(position.x, position.y, -0.03f);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            part.transform.localScale = new Vector3(scale.x, scale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.color = Color.white;
            renderer.sortingOrder = 20;
            return renderer;
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
                placeholderSprite.name = "Runtime Engraver Placeholder";
            }

            return placeholderSprite;
        }

        private void OnValidate()
        {
            processingDuration = Mathf.Max(0.01f, processingDuration);
            requiredAccelerationRunes = Mathf.Max(1, requiredAccelerationRunes);
            upgradedSpeedMultiplier = Mathf.Max(1.01f, upgradedSpeedMultiplier);
        }

        private void OnDestroy()
        {
            if (process == null)
            {
                return;
            }

            if (transportCoordinator != null)
            {
                transportCoordinator.UnregisterInputReceiver(process);
                transportCoordinator.UnregisterOutputSource(process);
            }

            process.DiscardContents();
        }
    }
}
