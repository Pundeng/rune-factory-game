using System;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public enum GlyphRotatorState
    {
        Idle,
        Processing,
        WaitingForOutput
    }

    public sealed class GlyphRotatorProcess : IRuneInputReceiver, IRuneOutputSource
    {
        private float configuredProcessingDuration;
        private float activeProcessingDuration;
        private GlyphType selectedGlyph;
        private GlyphType activeSelectedGlyph;
        private float elapsedProcessingTime;
        private RuneData heldRune;

        public GlyphRotatorProcess(
            Vector2Int cell,
            GridDirection direction,
            GlyphType selectedGlyph,
            float processingDuration)
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
            State = GlyphRotatorState.Idle;
        }

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public Vector2Int OutputCell { get; }

        public GridDirection OutputDirection { get; }

        public GlyphRotatorState State { get; private set; }

        public GlyphType SelectedGlyph => selectedGlyph;

        public GlyphType ActiveSelectedGlyph => activeSelectedGlyph;

        public RuneData HeldRune => heldRune;

        public bool CanAcceptInput => State == GlyphRotatorState.Idle;

        public bool HasOutput => State == GlyphRotatorState.WaitingForOutput;

        public bool? LastRotationSucceeded { get; private set; }

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

            this.selectedGlyph = selectedGlyph;
            configuredProcessingDuration = processingDuration;
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

            heldRune = rune;
            activeSelectedGlyph = selectedGlyph;
            activeProcessingDuration = configuredProcessingDuration;
            elapsedProcessingTime = 0f;
            LastRotationSucceeded = null;
            State = GlyphRotatorState.Processing;
            return true;
        }

        public bool Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time cannot be negative.");
            }

            if (State != GlyphRotatorState.Processing)
            {
                return false;
            }

            elapsedProcessingTime += deltaTime;
            if (elapsedProcessingTime < activeProcessingDuration)
            {
                return false;
            }

            if (!TryFindSelectedGlyph(heldRune, activeSelectedGlyph, out GlyphData glyph))
            {
                LastRotationSucceeded = false;
            }
            else
            {
                LastRotationSucceeded = RuneOperations.TryRotateGlyphClockwise(
                    heldRune,
                    glyph,
                    out RuneData rotatedRune);
                heldRune = rotatedRune;
            }

            State = GlyphRotatorState.WaitingForOutput;
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
            State = GlyphRotatorState.Idle;
            return true;
        }

        public void DiscardContents()
        {
            heldRune = null;
            elapsedProcessingTime = 0f;
            LastRotationSucceeded = null;
            State = GlyphRotatorState.Idle;
        }

        private static bool TryFindSelectedGlyph(
            RuneData rune,
            GlyphType selectedGlyph,
            out GlyphData selected)
        {
            foreach (GlyphData glyph in rune.Glyphs)
            {
                if (glyph.Type == selectedGlyph)
                {
                    selected = glyph;
                    return true;
                }
            }

            selected = default;
            return false;
        }
    }
}
