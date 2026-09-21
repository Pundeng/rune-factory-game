using System;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public enum EngraverState
    {
        Idle,
        Processing,
        WaitingForOutput
    }

    public sealed class EngraverProcess : IRuneInputReceiver, IRuneOutputSource
    {
        private float configuredProcessingDuration;
        private float upgradedSpeedMultiplier;
        private float activeProcessingDuration;
        private GlyphData configuredGlyph;
        private GlyphData activeGlyph;
        private RuneSigil configuredSigil;
        private RuneSigil activeSigil;
        private bool configuredForLegacyGlyph;
        private bool activeForLegacyGlyph;
        private float elapsedProcessingTime;
        private RuneData heldRune;

        public EngraverProcess(
            Vector2Int cell,
            GridDirection direction,
            GlyphType selectedGlyph,
            float processingDuration,
            float upgradedSpeedMultiplier = 2f)
            : this(
                cell,
                direction,
                RuneSigil.Spirit,
                processingDuration,
                upgradedSpeedMultiplier)
        {
            Configure(selectedGlyph, processingDuration);
        }

        public EngraverProcess(
            Vector2Int cell,
            GridDirection direction,
            RuneSigil selectedSigil,
            float processingDuration,
            float upgradedSpeedMultiplier = 2f)
        {
            if (!Enum.IsDefined(typeof(GridDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            InputCell = cell;
            RequiredIncomingDirection = direction;
            OutputCell = cell + direction.ToOffset();
            OutputDirection = direction;
            Configure(selectedSigil, processingDuration);
            ConfigureUpgrade(upgradedSpeedMultiplier);
            State = EngraverState.Idle;
        }

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public Vector2Int OutputCell { get; }

        public GridDirection OutputDirection { get; }

        public EngraverState State { get; private set; }

        public GlyphData ConfiguredGlyph => configuredGlyph;

        public GlyphData ActiveGlyph => activeGlyph;

        public RuneSigil ConfiguredSigil => configuredSigil;

        public RuneSigil ActiveSigil => activeSigil;

        public RuneData HeldRune => heldRune;

        public bool CanAcceptInput => State == EngraverState.Idle;

        public bool AllowsConcurrentInput => false;

        public bool HasOutput => State == EngraverState.WaitingForOutput;

        public bool? LastEngravingSucceeded { get; private set; }

        public bool CanAcceptInputFrom(GridDirection incomingDirection)
        {
            return incomingDirection == RequiredIncomingDirection;
        }

        public float UpgradedSpeedMultiplier => upgradedSpeedMultiplier;

        public bool IsAccelerationUpgraded { get; private set; }

        public float EffectiveProcessingDuration =>
            configuredProcessingDuration / (IsAccelerationUpgraded ? upgradedSpeedMultiplier : 1f);

        public void Configure(GlyphType selectedGlyph, float processingDuration)
        {
            if (!Enum.IsDefined(typeof(GlyphType), selectedGlyph))
            {
                throw new ArgumentOutOfRangeException(nameof(selectedGlyph), selectedGlyph, null);
            }

            if (processingDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processingDuration),
                    "Processing duration must be positive.");
            }

            configuredGlyph = new GlyphData(selectedGlyph, GlyphRotation.Degrees0);
            configuredSigil = RuneSigil.None;
            configuredForLegacyGlyph = true;
            configuredProcessingDuration = processingDuration;
        }

        public void Configure(RuneSigil selectedSigil, float processingDuration)
        {
            if (!Enum.IsDefined(typeof(RuneSigil), selectedSigil) ||
                selectedSigil == RuneSigil.None)
            {
                throw new ArgumentOutOfRangeException(nameof(selectedSigil), selectedSigil, null);
            }

            if (processingDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processingDuration),
                    "Processing duration must be positive.");
            }

            configuredSigil = selectedSigil;
            configuredForLegacyGlyph = false;
            configuredProcessingDuration = processingDuration;
        }

        public void ConfigureUpgrade(float speedMultiplier)
        {
            if (speedMultiplier <= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speedMultiplier),
                    "Upgrade speed multiplier must be greater than one.");
            }

            upgradedSpeedMultiplier = speedMultiplier;
        }

        public bool TryInstallAccelerationUpgrade()
        {
            if (IsAccelerationUpgraded)
            {
                return false;
            }

            IsAccelerationUpgraded = true;
            return true;
        }

        public bool TryAcceptInput(RuneData rune, GridDirection incomingDirection)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            if (!CanAcceptInput || !CanAcceptInputFrom(incomingDirection))
            {
                return false;
            }

            heldRune = rune;
            activeGlyph = configuredGlyph;
            activeSigil = configuredSigil;
            activeForLegacyGlyph = configuredForLegacyGlyph;
            activeProcessingDuration = EffectiveProcessingDuration;
            elapsedProcessingTime = 0f;
            LastEngravingSucceeded = null;
            State = EngraverState.Processing;
            return true;
        }

        public bool Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time cannot be negative.");
            }

            if (State != EngraverState.Processing)
            {
                return false;
            }

            elapsedProcessingTime += deltaTime;
            if (elapsedProcessingTime < activeProcessingDuration)
            {
                return false;
            }

            RuneData engravedRune;
            LastEngravingSucceeded = activeForLegacyGlyph
                ? RuneOperations.TryEngrave(heldRune, activeGlyph, out engravedRune)
                : RuneOperations.TryEngraveSigil(heldRune, activeSigil, out engravedRune);
            heldRune = engravedRune;
            State = EngraverState.WaitingForOutput;
            return true;
        }

        public RuneData PeekOutput()
        {
            return HasOutput ? heldRune : null;
        }

        public bool TryTakeOutput(out RuneData rune)
        {
            if (!HasOutput)
            {
                rune = null;
                return false;
            }

            rune = heldRune;
            heldRune = null;
            State = EngraverState.Idle;
            return true;
        }

        public void DiscardContents()
        {
            heldRune = null;
            elapsedProcessingTime = 0f;
            LastEngravingSucceeded = null;
            State = EngraverState.Idle;
        }

    }
}
