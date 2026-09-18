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
        private static readonly RuneData AccelerationUpgradeRune = new(
            RuneBaseShape.Circle,
            new[] { new GlyphData(GlyphType.Acceleration, GlyphRotation.Degrees0) },
            null);

        private float configuredProcessingDuration;
        private int requiredAccelerationRunes;
        private float upgradedSpeedMultiplier;
        private float activeProcessingDuration;
        private GlyphData configuredGlyph;
        private GlyphData activeGlyph;
        private float elapsedProcessingTime;
        private RuneData heldRune;

        public EngraverProcess(
            Vector2Int cell,
            GridDirection direction,
            GlyphType selectedGlyph,
            float processingDuration,
            int requiredAccelerationRunes = 100,
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
            Configure(selectedGlyph, processingDuration);
            ConfigureUpgrade(requiredAccelerationRunes, upgradedSpeedMultiplier);
            State = EngraverState.Idle;
        }

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public Vector2Int OutputCell { get; }

        public GridDirection OutputDirection { get; }

        public EngraverState State { get; private set; }

        public GlyphData ConfiguredGlyph => configuredGlyph;

        public GlyphData ActiveGlyph => activeGlyph;

        public RuneData HeldRune => heldRune;

        public bool CanAcceptInput => State == EngraverState.Idle;

        public bool HasOutput => State == EngraverState.WaitingForOutput;

        public bool? LastEngravingSucceeded { get; private set; }

        public int AccelerationUpgradeProgress { get; private set; }

        public int RequiredAccelerationRunes => requiredAccelerationRunes;

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
            configuredProcessingDuration = processingDuration;
        }

        public void ConfigureUpgrade(
            int accelerationRuneRequirement,
            float speedMultiplier)
        {
            if (accelerationRuneRequirement <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(accelerationRuneRequirement),
                    "Acceleration rune requirement must be positive.");
            }

            if (speedMultiplier <= 1f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(speedMultiplier),
                    "Upgrade speed multiplier must be greater than one.");
            }

            requiredAccelerationRunes = accelerationRuneRequirement;
            upgradedSpeedMultiplier = speedMultiplier;
            if (!IsAccelerationUpgraded &&
                AccelerationUpgradeProgress >= requiredAccelerationRunes)
            {
                ActivateAccelerationUpgrade();
            }
        }

        public bool TryAcceptInput(RuneData rune, GridDirection incomingDirection)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            if (!CanAcceptInput || incomingDirection != RequiredIncomingDirection)
            {
                return false;
            }

            if (TryConsumeAccelerationUpgradeRune(rune))
            {
                return true;
            }

            heldRune = rune;
            activeGlyph = configuredGlyph;
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

            LastEngravingSucceeded = RuneOperations.TryEngrave(
                heldRune,
                activeGlyph,
                out RuneData engravedRune);
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

        private bool TryConsumeAccelerationUpgradeRune(RuneData rune)
        {
            if (IsAccelerationUpgraded || !AccelerationUpgradeRune.Equals(rune))
            {
                return false;
            }

            AccelerationUpgradeProgress++;
            if (AccelerationUpgradeProgress >= requiredAccelerationRunes)
            {
                ActivateAccelerationUpgrade();
            }

            return true;
        }

        private void ActivateAccelerationUpgrade()
        {
            IsAccelerationUpgraded = true;
            AccelerationUpgradeProgress = 0;
        }
    }
}
