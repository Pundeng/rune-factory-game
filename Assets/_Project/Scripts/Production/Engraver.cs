using System;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class Engraver : MonoBehaviour, IBuildingRemovalRule, IBuildingMoveState
    {
        public readonly struct RecipeConfiguration
        {
            public RecipeConfiguration(RuneSigil sigil, GlyphType glyph, float duration)
            {
                Sigil = sigil;
                Glyph = glyph;
                Duration = duration;
            }

            public RuneSigil Sigil { get; }
            public GlyphType Glyph { get; }
            public float Duration { get; }
        }

        [SerializeField] private RuneSigil selectedSigil = RuneSigil.None;
        [SerializeField] private GlyphType selectedGlyph = GlyphType.Attack;
        [SerializeField, Min(0.01f)] private float processingDuration = 1.5f;
        [Header("Acceleration Upgrade")]
        [SerializeField, Min(1.01f)] private float upgradedSpeedMultiplier = 2f;
        [SerializeField] private bool accelerationUpgradeApplied;
        [SerializeField] private EngraverState state;

        private static Sprite placeholderSprite;
        private BeltTransportCoordinator transportCoordinator;
        private EngraverProcess process;
        private SpriteRenderer stateIndicator;
        private SpriteRenderer socketIndicator;
        private EngraverState? lastVisualState;
        private bool? lastSocketOccupied;

        public bool CanRemove => true;

        public bool CanMove => process != null &&
            process.State == EngraverState.Idle &&
            !process.IsAccelerationUpgraded;

        public void DetachForMove()
        {
            if (process == null)
            {
                return;
            }

            transportCoordinator?.UnregisterInputReceiver(process);
            transportCoordinator?.UnregisterOutputSource(process);
        }

        public void ReattachAfterFailedMove()
        {
            transportCoordinator.RegisterInputReceiver(process);
            transportCoordinator.RegisterOutputSource(process);
        }

        public void Initialize(
            Vector2Int anchorCell,
            GridDirection direction,
            BeltTransportCoordinator coordinator)
        {
            transportCoordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
            process = selectedSigil == RuneSigil.None
                ? new EngraverProcess(
                    anchorCell,
                    direction,
                    selectedGlyph,
                    processingDuration,
                    upgradedSpeedMultiplier)
                : new EngraverProcess(
                    anchorCell,
                    direction,
                    selectedSigil,
                    processingDuration,
                    upgradedSpeedMultiplier);
            transportCoordinator.RegisterInputReceiver(process);
            transportCoordinator.RegisterOutputSource(process);
            CreateDirectionArrow();
            CreateStateIndicator();
            CreateSocketIndicator();
            RefreshUpgradeDebugState();
            RefreshVisualState();
        }

        private void Update()
        {
            if (process == null)
            {
                return;
            }

            if (selectedSigil == RuneSigil.None)
            {
                process.Configure(selectedGlyph, processingDuration);
            }
            else
            {
                process.Configure(selectedSigil, processingDuration);
            }

            process.ConfigureUpgrade(upgradedSpeedMultiplier);
            bool completed = process.Advance(Time.deltaTime);
            if (completed && process.LastEngravingSucceeded == false)
            {
                string activeIdentity = process.ActiveSigil == RuneSigil.None
                    ? process.ActiveGlyph.ToString()
                    : process.ActiveSigil.ToString();
                Debug.LogWarning(
                    $"Engraver could not add {activeIdentity}; " +
                    "the rune will pass through unchanged.",
                    this);
            }

            RefreshUpgradeDebugState();
            RefreshVisualState();
        }

        private void RefreshUpgradeDebugState()
        {
            accelerationUpgradeApplied = process?.IsAccelerationUpgraded ?? false;
        }

        private void RefreshVisualState()
        {
            state = process?.State ?? EngraverState.Idle;
            bool socketOccupied = process?.IsAccelerationUpgraded ?? false;
            if (lastVisualState == state &&
                lastSocketOccupied == socketOccupied)
            {
                return;
            }

            Color color = state switch
            {
                EngraverState.Processing when socketOccupied =>
                    new Color(1f, 0.35f, 0.95f, 1f),
                EngraverState.Processing => new Color(0.75f, 0.3f, 0.9f, 1f),
                EngraverState.WaitingForOutput => new Color(0.95f, 0.65f, 0.2f, 1f),
                _ => new Color(0.25f, 0.7f, 0.85f, 1f)
            };

            stateIndicator.color = color;
            socketIndicator.color = socketOccupied
                ? new Color(0.85f, 0.25f, 1f, 1f)
                : new Color(0.2f, 0.2f, 0.25f, 1f);
            lastVisualState = state;
            lastSocketOccupied = socketOccupied;
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

        private void CreateSocketIndicator()
        {
            socketIndicator = CreateVisualPart(
                "Acceleration Socket",
                new Vector2(-0.3f, -0.3f),
                Vector2.one * 0.16f,
                45f);
        }

        public bool IsAccelerationSocketOccupied =>
            process?.IsAccelerationUpgraded ?? false;

        public RuneSigil SelectedSigil => selectedSigil;

        public RecipeConfiguration CaptureRecipeConfiguration() =>
            new(selectedSigil, selectedGlyph, processingDuration);

        public void ApplyRecipeConfiguration(RecipeConfiguration configuration)
        {
            if (process != null)
            {
                throw new InvalidOperationException("Apply a copied recipe before initialization.");
            }

            selectedSigil = configuration.Sigil;
            selectedGlyph = configuration.Glyph;
            processingDuration = configuration.Duration;
        }

        public float UpgradedSpeedMultiplier => upgradedSpeedMultiplier;

        public void SetSelectedSigil(RuneSigil sigil)
        {
            if (!Enum.IsDefined(typeof(RuneSigil), sigil) || sigil == RuneSigil.None)
            {
                throw new ArgumentOutOfRangeException(nameof(sigil), sigil, null);
            }

            selectedSigil = sigil;
            process?.Configure(selectedSigil, processingDuration);
        }

        public bool TryInstallAccelerationRune(Func<bool> tryConsumeRune)
        {
            if (tryConsumeRune == null)
            {
                throw new ArgumentNullException(nameof(tryConsumeRune));
            }

            if (process == null || process.IsAccelerationUpgraded || !tryConsumeRune())
            {
                return false;
            }

            bool installed = process.TryInstallAccelerationUpgrade();
            RefreshUpgradeDebugState();
            RefreshVisualState();
            return installed;
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
