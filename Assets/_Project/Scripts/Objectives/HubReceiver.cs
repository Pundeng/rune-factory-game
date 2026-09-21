using System;
using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Objectives
{
    public sealed class HubReceiver : IRuneInputReceiver
    {
        private readonly ObjectiveProgress objectiveProgress;
        private readonly AccelerationRuneInventory accelerationRuneInventory;

        public HubReceiver(
            Vector2Int inputCell,
            GridDirection requiredIncomingDirection,
            ObjectiveProgress objectiveProgress,
            AccelerationRuneInventory accelerationRuneInventory)
        {
            if (!Enum.IsDefined(typeof(GridDirection), requiredIncomingDirection))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(requiredIncomingDirection),
                    requiredIncomingDirection,
                    null);
            }

            InputCell = inputCell;
            RequiredIncomingDirection = requiredIncomingDirection;
            this.objectiveProgress = objectiveProgress ??
                throw new ArgumentNullException(nameof(objectiveProgress));
            this.accelerationRuneInventory = accelerationRuneInventory ??
                throw new ArgumentNullException(nameof(accelerationRuneInventory));
        }

        public event Action<RuneDeliveryResult> RuneConsumed;

        public Vector2Int InputCell { get; }

        public GridDirection RequiredIncomingDirection { get; }

        public bool CanAcceptInput => true;

        public RuneDeliveryResult? LastDeliveryResult { get; private set; }

        public bool TryAcceptInput(RuneData rune, GridDirection incomingDirection)
        {
            if (rune == null)
            {
                throw new ArgumentNullException(nameof(rune));
            }

            if (incomingDirection != RequiredIncomingDirection)
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
    }
}
