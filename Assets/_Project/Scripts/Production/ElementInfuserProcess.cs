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
        private float configuredProcessingDuration;
        private float activeProcessingDuration;
        private RuneElement configuredElement;
        private RuneElement activeElement;
        private ElementZone configuredZone;
        private ElementZone activeZone;
        private bool configuredForLegacyZone;
        private bool activeForLegacyZone;
        private float elapsedProcessingTime;
        private RuneData heldRune;

        public ElementInfuserProcess(
            Vector2Int cell,
            GridDirection direction,
            RuneElement configuredElement,
            ElementZone configuredZone,
            float processingDuration)
            : this(cell, direction, configuredElement, processingDuration)
        {
            Configure(configuredElement, configuredZone, processingDuration);
        }

        public ElementInfuserProcess(
            Vector2Int cell,
            GridDirection direction,
            RuneElement configuredElement,
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
            Configure(configuredElement, processingDuration);
            State = ElementInfuserState.Idle;
        }

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public Vector2Int OutputCell { get; }

        public GridDirection OutputDirection { get; }

        public ElementInfuserState State { get; private set; }

        public RuneElement ConfiguredElement => configuredElement;

        public ElementZone ConfiguredZone => configuredZone;

        public RuneElement ActiveElement => activeElement;

        public ElementZone ActiveZone => activeZone;

        public bool ActiveUsesLegacyZone => activeForLegacyZone;

        public RuneData HeldRune => heldRune;

        public bool CanAcceptInput => State == ElementInfuserState.Idle;

        public bool AllowsConcurrentInput => false;

        public bool HasOutput => State == ElementInfuserState.WaitingForOutput;

        public bool? LastInfusionSucceeded { get; private set; }

        public bool CanAcceptInputFrom(GridDirection incomingDirection)
        {
            return incomingDirection == RequiredIncomingDirection;
        }

        public void Configure(
            RuneElement element,
            ElementZone zone,
            float processingDuration)
        {
            if (!Enum.IsDefined(typeof(RuneElement), element))
            {
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }

            if (!Enum.IsDefined(typeof(ElementZone), zone))
            {
                throw new ArgumentOutOfRangeException(nameof(zone), zone, null);
            }

            if (processingDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processingDuration),
                    "Processing duration must be positive.");
            }

            configuredElement = element;
            configuredZone = zone;
            configuredForLegacyZone = true;
            configuredProcessingDuration = processingDuration;
        }

        public void Configure(RuneElement element, float processingDuration)
        {
            if (!Enum.IsDefined(typeof(RuneElement), element))
            {
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
            }

            if (processingDuration <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(processingDuration),
                    "Processing duration must be positive.");
            }

            configuredElement = element;
            configuredForLegacyZone = false;
            configuredProcessingDuration = processingDuration;
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
            activeElement = configuredElement;
            activeZone = configuredZone;
            activeForLegacyZone = configuredForLegacyZone;
            activeProcessingDuration = configuredProcessingDuration;
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
            if (elapsedProcessingTime < activeProcessingDuration)
            {
                return false;
            }

            RuneData infusedRune;
            LastInfusionSucceeded = activeForLegacyZone
                ? RuneOperations.TryAssignElement(
                    heldRune,
                    activeZone,
                    activeElement,
                    out infusedRune)
                : RuneOperations.TryAssignPrimaryElement(
                    heldRune,
                    activeElement,
                    out infusedRune);
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
