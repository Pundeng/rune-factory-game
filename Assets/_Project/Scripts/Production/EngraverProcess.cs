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
        private readonly float processingDuration;
        private readonly GlyphData configuredGlyph;
        private float elapsedProcessingTime;
        private RuneData heldRune;

        public EngraverProcess(
            Vector2Int cell,
            GridDirection direction,
            GlyphType selectedGlyph,
            float processingDuration)
        {
            if (!Enum.IsDefined(typeof(GridDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

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

            InputCell = cell;
            RequiredIncomingDirection = direction;
            OutputCell = cell + direction.ToOffset();
            OutputDirection = direction;
            this.processingDuration = processingDuration;
            configuredGlyph = new GlyphData(selectedGlyph, GlyphRotation.Degrees0);
            State = EngraverState.Idle;
        }

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public Vector2Int OutputCell { get; }

        public GridDirection OutputDirection { get; }

        public EngraverState State { get; private set; }

        public GlyphData ConfiguredGlyph => configuredGlyph;

        public RuneData HeldRune => heldRune;

        public bool CanAcceptInput => State == EngraverState.Idle;

        public bool HasOutput => State == EngraverState.WaitingForOutput;

        public bool? LastEngravingSucceeded { get; private set; }

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
            if (elapsedProcessingTime < processingDuration)
            {
                return false;
            }

            LastEngravingSucceeded = RuneOperations.TryEngrave(
                heldRune,
                configuredGlyph,
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
    }
}
