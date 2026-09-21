using System;
using System.Collections.Generic;
using System.Linq;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    public sealed class HubReceiver : IRuneInputReceiver
    {
        private readonly ObjectiveProgress objectiveProgress;
        private readonly AccelerationRuneInventory accelerationRuneInventory;
        private readonly HashSet<GridDirection> acceptedIncomingDirections;

        public HubReceiver(
            Vector2Int inputCell,
            GridDirection requiredIncomingDirection,
            ObjectiveProgress objectiveProgress,
            AccelerationRuneInventory accelerationRuneInventory)
            : this(
                inputCell,
                ValidateRequiredDirection(requiredIncomingDirection),
                objectiveProgress,
                accelerationRuneInventory)
        {
        }

        public HubReceiver(
            Vector2Int inputCell,
            IEnumerable<GridDirection> acceptedIncomingDirections,
            ObjectiveProgress objectiveProgress,
            AccelerationRuneInventory accelerationRuneInventory)
        {
            if (acceptedIncomingDirections == null)
            {
                throw new ArgumentNullException(nameof(acceptedIncomingDirections));
            }

            GridDirection[] incomingDirections = acceptedIncomingDirections.ToArray();
            if (incomingDirections.Length == 0 ||
                incomingDirections.Any(direction =>
                    !Enum.IsDefined(typeof(GridDirection), direction)))
            {
                throw new ArgumentException(
                    "The Hub requires at least one valid incoming direction.",
                    nameof(acceptedIncomingDirections));
            }

            this.acceptedIncomingDirections = incomingDirections.ToHashSet();
            InputCell = inputCell;
            RequiredIncomingDirection = incomingDirections[0];
            this.objectiveProgress = objectiveProgress ??
                throw new ArgumentNullException(nameof(objectiveProgress));
            this.accelerationRuneInventory = accelerationRuneInventory ??
                throw new ArgumentNullException(nameof(accelerationRuneInventory));
        }

        public event Action<RuneDeliveryResult> RuneConsumed;

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public bool CanAcceptInput => true;

        public bool AllowsConcurrentInput => true;

        public RuneDeliveryResult? LastDeliveryResult { get; private set; }

        public bool CanAcceptInputFrom(GridDirection incomingDirection)
        {
            return acceptedIncomingDirections.Contains(incomingDirection);
        }

        public bool TryAcceptInput(RuneData rune, GridDirection incomingDirection)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            if (!CanAcceptInputFrom(incomingDirection))
            {
                return false;
            }

            // Every valid-side delivery is consumed. Incorrect runes report a result but
            // deliberately do not block the Hub input or advance objective progress.
            accelerationRuneInventory.StoreDeliveredRune(rune);
            LastDeliveryResult = objectiveProgress.Deliver(rune);
            RuneConsumed?.Invoke(LastDeliveryResult.Value);
            return true;
        }

        private static IEnumerable<GridDirection> ValidateRequiredDirection(
            GridDirection requiredIncomingDirection)
        {
            if (!Enum.IsDefined(typeof(GridDirection), requiredIncomingDirection))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredIncomingDirection),
                    requiredIncomingDirection,
                    null);
            }

            return new[] { requiredIncomingDirection };
        }
    }
}
