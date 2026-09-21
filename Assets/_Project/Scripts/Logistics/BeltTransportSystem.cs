using System;
using System.Collections.Generic;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class BeltTransportSystem
    {
        private readonly Dictionary<Vector2Int, BeltCell> beltsByCell = new();
        private readonly Dictionary<Vector2Int, IRuneInputReceiver> receiversByCell = new();
        private readonly List<BeltCell> orderedBelts = new();
        private readonly List<IRuneOutputSource> outputSources = new();

        public BeltTransportSystem(float movementSpeed)
        {
            if (movementSpeed <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(movementSpeed),
                    "Movement speed must be positive.");
            }

            MovementSpeed = movementSpeed;
        }

        public float MovementSpeed { get; }

        public BeltCell AddBelt(Vector2Int cell, GridDirection direction)
        {
            if (beltsByCell.ContainsKey(cell) || receiversByCell.ContainsKey(cell))
            {
                throw new InvalidOperationException($"A logistics carrier already exists at {cell}.");
            }

            var belt = new BeltCell(cell, direction);
            beltsByCell.Add(cell, belt);
            orderedBelts.Add(belt);
            orderedBelts.Sort(CompareCells);
            return belt;
        }

        public bool RemoveBelt(BeltCell belt)
        {
            if (belt == null ||
                !beltsByCell.TryGetValue(belt.Cell, out BeltCell registered) ||
                !ReferenceEquals(registered, belt))
            {
                return false;
            }

            // Removing a belt intentionally discards its in-flight Rune for MVP rebuilding.
            belt.TakeItem();
            beltsByCell.Remove(belt.Cell);
            orderedBelts.Remove(belt);
            return true;
        }

        public bool TryGetBelt(Vector2Int cell, out BeltCell belt)
        {
            return beltsByCell.TryGetValue(cell, out belt);
        }

        public void RegisterInputReceiver(IRuneInputReceiver receiver)
        {
            if (receiver == null)
            {
                throw new ArgumentNullException(nameof(receiver));
            }

            if (beltsByCell.ContainsKey(receiver.InputCell) ||
                receiversByCell.ContainsKey(receiver.InputCell))
            {
                throw new InvalidOperationException(
                    $"A logistics carrier already exists at {receiver.InputCell}.");
            }

            receiversByCell.Add(receiver.InputCell, receiver);
        }

        public void UnregisterInputReceiver(IRuneInputReceiver receiver)
        {
            if (receiver != null &&
                receiversByCell.TryGetValue(receiver.InputCell, out IRuneInputReceiver registered) &&
                ReferenceEquals(receiver, registered))
            {
                receiversByCell.Remove(receiver.InputCell);
            }
        }

        public void RegisterOutputSource(IRuneOutputSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!outputSources.Contains(source))
            {
                outputSources.Add(source);
            }
        }

        public void UnregisterOutputSource(IRuneOutputSource source)
        {
            outputSources.Remove(source);
        }

        public void Advance(float deltaTime)
        {
            if (deltaTime < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime), "Delta time cannot be negative.");
            }

            float movementDistance = deltaTime * MovementSpeed;
            foreach (BeltCell belt in orderedBelts)
            {
                belt.Advance(movementDistance);
            }

            TransferReadyItems();
            TransferSourceOutputs();
        }

        private void TransferReadyItems()
        {
            // Decisions use the pre-transfer occupancy snapshot so a chain cannot cascade
            // differently based on component or dictionary iteration order.
            var beltTransfers = new List<(BeltCell Source, BeltCell Destination)>();
            var receiverTransfers = new List<(BeltCell Source, IRuneInputReceiver Destination)>();
            var reservedDestinations = new HashSet<Vector2Int>();

            foreach (BeltCell source in orderedBelts)
            {
                if (!source.HasItem || source.Item.Progress < 1f)
                {
                    continue;
                }

                if (beltsByCell.TryGetValue(source.OutputCell, out BeltCell beltDestination) &&
                    beltDestination.OutputCell != source.Cell &&
                    beltDestination.CanAccept &&
                    reservedDestinations.Add(beltDestination.Cell))
                {
                    beltTransfers.Add((source, beltDestination));
                    continue;
                }

                if (receiversByCell.TryGetValue(
                        source.OutputCell,
                        out IRuneInputReceiver receiverDestination) &&
                    receiverDestination.CanAcceptInput &&
                    receiverDestination.CanAcceptInputFrom(source.Direction) &&
                    (receiverDestination.AllowsConcurrentInput ||
                        reservedDestinations.Add(receiverDestination.InputCell)))
                {
                    receiverTransfers.Add((source, receiverDestination));
                }
            }

            foreach ((BeltCell source, BeltCell destination) in beltTransfers)
            {
                TransportedRune item = source.TakeItem();
                destination.TryAccept(item, source.Direction);
            }

            foreach ((BeltCell source, IRuneInputReceiver destination) in receiverTransfers)
            {
                RuneData rune = source.Item.Rune;
                if (!destination.TryAcceptInput(rune, source.Direction))
                {
                    throw new InvalidOperationException(
                        "A rune input receiver changed during a deterministic transfer.");
                }

                source.TakeItem();
            }
        }

        private void TransferSourceOutputs()
        {
            foreach (IRuneOutputSource source in outputSources)
            {
                if (!source.HasOutput ||
                    !beltsByCell.TryGetValue(source.OutputCell, out BeltCell destination) ||
                    !destination.CanAccept)
                {
                    continue;
                }

                RuneData pendingRune = source.PeekOutput();
                if (pendingRune == null || !source.TryTakeOutput(out RuneData takenRune))
                {
                    continue;
                }

                if (!ReferenceEquals(pendingRune, takenRune) ||
                    !destination.TryAccept(takenRune, source.OutputDirection))
                {
                    throw new InvalidOperationException(
                        "A rune output source changed during a deterministic transfer.");
                }
            }
        }

        private static int CompareCells(BeltCell left, BeltCell right)
        {
            int yComparison = left.Cell.y.CompareTo(right.Cell.y);
            return yComparison != 0 ? yComparison : left.Cell.x.CompareTo(right.Cell.x);
        }
    }
}
