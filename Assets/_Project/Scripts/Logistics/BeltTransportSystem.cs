using System;
using System.Collections.Generic;
using FantasyShapez.Runes;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class BeltTransportSystem
    {
        private readonly Dictionary<Vector2Int, BeltCell> beltsByCell = new();
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
            if (beltsByCell.ContainsKey(cell))
            {
                throw new InvalidOperationException($"A belt already exists at {cell}.");
            }

            var belt = new BeltCell(cell, direction);
            beltsByCell.Add(cell, belt);
            orderedBelts.Add(belt);
            orderedBelts.Sort(CompareCells);
            return belt;
        }

        public bool RemoveBelt(BeltCell belt)
        {
            if (belt == null || belt.HasItem ||
                !beltsByCell.TryGetValue(belt.Cell, out BeltCell registered) ||
                !ReferenceEquals(registered, belt))
            {
                return false;
            }

            beltsByCell.Remove(belt.Cell);
            orderedBelts.Remove(belt);
            return true;
        }

        public bool TryGetBelt(Vector2Int cell, out BeltCell belt)
        {
            return beltsByCell.TryGetValue(cell, out belt);
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
            var transfers = new List<(BeltCell Source, BeltCell Destination)>();
            var reservedDestinations = new HashSet<Vector2Int>();

            foreach (BeltCell source in orderedBelts)
            {
                if (!source.HasItem || source.Item.Progress < 1f ||
                    !beltsByCell.TryGetValue(source.OutputCell, out BeltCell destination) ||
                    !destination.CanAccept ||
                    !reservedDestinations.Add(destination.Cell))
                {
                    continue;
                }

                transfers.Add((source, destination));
            }

            foreach ((BeltCell source, BeltCell destination) in transfers)
            {
                TransportedRune item = source.TakeItem();
                destination.TryAccept(item, source.Direction);
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
