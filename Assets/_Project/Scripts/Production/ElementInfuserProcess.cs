using System;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Production
{
    public enum ElementInfuserState
    {
        Idle,
        Processing,
        WaitingForOutput
    }

    public sealed class ElementInfuserProcess : IRuneInputReceiver, IRuneOutputSource
    {
        private readonly float processingDuration;
        private float elapsedProcessingTime;
        private RuneData heldRune;

        public ElementInfuserProcess(
            Vector2Int cell,
            GridDirection direction,
            RuneElement configuredElement,
            ElementZone configuredZone,
            float processingDuration)
        {
            if (!Enum.IsDefined(typeof(GridDirection), direction))
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            if (!Enum.IsDefined(typeof(RuneElement), configuredElement))
            {
                throw new ArgumentOutOfRangeException(nameof(configuredElement), configuredElement, null);
            }

            if (!Enum.IsDefined(typeof(ElementZone), configuredZone))
            {
                throw new ArgumentOutOfRangeException(nameof(configuredZone), configuredZone, null);
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
            ConfiguredElement = configuredElement;
            ConfiguredZone = configuredZone;
            this.processingDuration = processingDuration;
            State = ElementInfuserState.Idle;
        }

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public Vector2Int OutputCell { get; }

        public GridDirection OutputDirection { get; }

        public ElementInfuserState State { get; private set; }

        public RuneElement ConfiguredElement { get; }

        public ElementZone ConfiguredZone { get; }

        public RuneData HeldRune => heldRune;

        public bool CanAcceptInput => State == ElementInfuserState.Idle;

        public bool HasOutput => State == ElementInfuserState.WaitingForOutput;

        public bool? LastInfusionSucceeded { get; private set; }

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
            LastInfusionSucceeded = null;
            State = ElementInfuserState.Processing;
            return true;
        }

        public bool Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time cannot be negative.");
            }

            if (State != ElementInfuserState.Processing)
            {
                return false;
            }

            elapsedProcessingTime += deltaTime;
            if (elapsedProcessingTime < processingDuration)
            {
                return false;
            }

            LastInfusionSucceeded = RuneOperations.TryAssignElement(
                heldRune,
                ConfiguredZone,
                ConfiguredElement,
                out RuneData infusedRune);
            heldRune = infusedRune;
            State = ElementInfuserState.WaitingForOutput;
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
            State = ElementInfuserState.Idle;
            return true;
        }

        public void DiscardContents()
        {
            heldRune = null;
            elapsedProcessingTime = 0f;
            LastInfusionSucceeded = null;
            State = ElementInfuserState.Idle;
        }
    }
}
