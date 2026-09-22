using System.Collections.Generic;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class BeltTransportTests
    {
        [TestCase(BuildingRotation.Degrees0, GridDirection.North, 0, 1)]
        [TestCase(BuildingRotation.Degrees90, GridDirection.East, 1, 0)]
        [TestCase(BuildingRotation.Degrees180, GridDirection.South, 0, -1)]
        [TestCase(BuildingRotation.Degrees270, GridDirection.West, -1, 0)]
        public void BuildingRotation_MapsToExpectedOutputCell(
            BuildingRotation rotation,
            GridDirection expectedDirection,
            int outputX,
            int outputY)
        {
            GridDirection direction = rotation.ToGridDirection();
            var belt = new BeltCell(Vector2Int.zero, direction);

            Assert.That(direction, Is.EqualTo(expectedDirection));
            Assert.That(belt.OutputCell, Is.EqualTo(new Vector2Int(outputX, outputY)));
        }

        [Test]
        public void Belt_AcceptsOnlyOneRuneAtATime()
        {
            var belt = new BeltCell(Vector2Int.zero, GridDirection.East);

            bool acceptedFirst = belt.TryAccept(CreateRune(), GridDirection.East);
            bool acceptedSecond = belt.TryAccept(CreateRune(), GridDirection.East);

            Assert.That(acceptedFirst, Is.True);
            Assert.That(acceptedSecond, Is.False);
            Assert.That(belt.HasItem, Is.True);
        }

        [Test]
        public void ReadyRune_TransfersToAvailableNextBeltWithoutChangingRuneData()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell first = system.AddBelt(Vector2Int.zero, GridDirection.East);
            BeltCell second = system.AddBelt(Vector2Int.right, GridDirection.North);
            RuneData rune = CreateRune();
            first.TryAccept(rune, GridDirection.East);

            system.Advance(1f);

            Assert.That(first.HasItem, Is.False);
            Assert.That(second.HasItem, Is.True);
            Assert.That(second.Item.Rune, Is.SameAs(rune));
            Assert.That(second.Item.Progress, Is.Zero);
        }

        [Test]
        public void ReadyRune_WaitsWhenNextBeltIsOccupied()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell first = system.AddBelt(Vector2Int.zero, GridDirection.East);
            BeltCell second = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData waitingRune = CreateRune();
            first.TryAccept(waitingRune, GridDirection.East);
            second.TryAccept(CreateRune(), GridDirection.East);

            system.Advance(1f);

            Assert.That(first.Item.Rune, Is.SameAs(waitingRune));
            Assert.That(first.Item.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void Rune_StopsAtOutputEndWhenNoNextBeltExists()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell belt = system.AddBelt(Vector2Int.zero, GridDirection.East);
            RuneData rune = CreateRune();
            belt.TryAccept(rune, GridDirection.East);

            system.Advance(2f);

            Assert.That(belt.Item.Rune, Is.SameAs(rune));
            Assert.That(belt.Item.Progress, Is.EqualTo(1f));
        }

        [TestCase(GridDirection.North, 0, 1, GridDirection.South)]
        [TestCase(GridDirection.East, 1, 0, GridDirection.West)]
        [TestCase(GridDirection.South, 0, -1, GridDirection.North)]
        [TestCase(GridDirection.West, -1, 0, GridDirection.East)]
        public void OppositeFacingBelts_BlockRuneWithoutBounceBack(
            GridDirection sourceDirection,
            int destinationX,
            int destinationY,
            GridDirection destinationDirection)
        {
            var system = new BeltTransportSystem(1f);
            BeltCell source = system.AddBelt(Vector2Int.zero, sourceDirection);
            BeltCell destination = system.AddBelt(
                new Vector2Int(destinationX, destinationY),
                destinationDirection);
            RuneData rune = CreateRune();
            source.TryAccept(rune, sourceDirection);

            system.Advance(1f);
            system.Advance(1f);
            system.Advance(1f);

            Assert.That(source.Item.Rune, Is.SameAs(rune));
            Assert.That(source.Item.Progress, Is.EqualTo(1f));
            Assert.That(destination.HasItem, Is.False);
        }

        [Test]
        public void OccupiedOppositeFacingBelts_PreserveBothRunesInPlace()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell left = system.AddBelt(Vector2Int.zero, GridDirection.East);
            BeltCell right = system.AddBelt(Vector2Int.right, GridDirection.West);
            RuneData leftRune = CreateRune();
            RuneData rightRune = CreateRune();
            left.TryAccept(leftRune, GridDirection.East);
            right.TryAccept(rightRune, GridDirection.West);

            system.Advance(1f);
            system.Advance(1f);

            Assert.That(left.Item.Rune, Is.SameAs(leftRune));
            Assert.That(right.Item.Rune, Is.SameAs(rightRune));
            Assert.That(left.Item.Progress, Is.EqualTo(1f));
            Assert.That(right.Item.Progress, Is.EqualTo(1f));
        }

        [Test]
        public void ExtractorOutput_TransfersOnlyWhenReceivingBeltCanAccept()
        {
            var buffer = new RuneOutputBuffer(2);
            RuneData firstRune = CreateRune();
            RuneData secondRune = CreateRune();
            buffer.TryAdd(firstRune);
            buffer.TryAdd(secondRune);
            var source = new BufferedOutputSource(
                buffer,
                Vector2Int.right,
                GridDirection.East);
            var system = new BeltTransportSystem(1f);
            BeltCell receivingBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            system.RegisterOutputSource(source);

            system.Advance(0f);
            system.Advance(0f);

            Assert.That(receivingBelt.Item.Rune, Is.SameAs(firstRune));
            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.PeekOutput(), Is.SameAs(secondRune));
        }

        [Test]
        public void ExtractorOutput_RemainsBufferedWhenReceivingBeltIsBlocked()
        {
            var buffer = new RuneOutputBuffer(1);
            RuneData bufferedRune = CreateRune();
            buffer.TryAdd(bufferedRune);
            var source = new BufferedOutputSource(
                buffer,
                Vector2Int.right,
                GridDirection.East);
            var system = new BeltTransportSystem(1f);
            BeltCell receivingBelt = system.AddBelt(Vector2Int.right, GridDirection.East);
            receivingBelt.TryAccept(CreateRune(), GridDirection.East);
            system.RegisterOutputSource(source);

            system.Advance(0f);

            Assert.That(buffer.Count, Is.EqualTo(1));
            Assert.That(buffer.PeekOutput(), Is.SameAs(bufferedRune));
        }

        [Test]
        public void ThreeBeltChain_MovesRuneToFinalBelt()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell first = system.AddBelt(new Vector2Int(0, 0), GridDirection.East);
            BeltCell second = system.AddBelt(new Vector2Int(1, 0), GridDirection.East);
            BeltCell third = system.AddBelt(new Vector2Int(2, 0), GridDirection.North);
            RuneData rune = CreateRune();
            first.TryAccept(rune, GridDirection.East);

            system.Advance(1f);
            system.Advance(1f);

            Assert.That(first.HasItem, Is.False);
            Assert.That(second.HasItem, Is.False);
            Assert.That(third.Item.Rune, Is.SameAs(rune));
        }

        [Test]
        public void DragPlannedTurn_ProducesConnectedTransportDirections()
        {
            var planner = new BeltDragPlacementPlanner();
            var plannedSteps = new List<BeltPlacementStep>(planner.Continue(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1)
            }));
            planner.TryComplete(BuildingRotation.Degrees180, out BeltPlacementStep finalStep);
            plannedSteps.Add(finalStep);
            var system = new BeltTransportSystem(1f);
            var belts = new List<BeltCell>();
            foreach (BeltPlacementStep step in plannedSteps)
            {
                belts.Add(system.AddBelt(step.Cell, step.Rotation.ToGridDirection()));
            }

            RuneData rune = CreateRune();
            belts[0].TryAccept(rune, GridDirection.East);
            system.Advance(1f);
            system.Advance(1f);

            Assert.That(belts[0].OutputCell, Is.EqualTo(belts[1].Cell));
            Assert.That(belts[1].OutputCell, Is.EqualTo(belts[2].Cell));
            Assert.That(belts[2].Item.Rune, Is.SameAs(rune));
        }

        [Test]
        public void EmptyBelt_CanBeRemoved()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell belt = system.AddBelt(Vector2Int.zero, GridDirection.East);

            bool removed = system.RemoveBelt(belt);

            Assert.That(removed, Is.True);
            Assert.That(system.TryGetBelt(Vector2Int.zero, out _), Is.False);
        }

        [Test]
        public void OccupiedBelt_CanBeRemovedAndDiscardsRune()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell belt = system.AddBelt(Vector2Int.zero, GridDirection.East);
            belt.TryAccept(CreateRune(), GridDirection.East);

            bool removed = system.RemoveBelt(belt);

            Assert.That(removed, Is.True);
            Assert.That(belt.HasItem, Is.False);
        }

        [Test]
        public void RemovedBeltCell_CanBeReused()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell removedBelt = system.AddBelt(Vector2Int.zero, GridDirection.East);
            removedBelt.TryAccept(CreateRune(), GridDirection.East);
            system.RemoveBelt(removedBelt);

            BeltCell replacement = system.AddBelt(Vector2Int.zero, GridDirection.North);

            Assert.That(replacement, Is.Not.SameAs(removedBelt));
            Assert.That(replacement.Cell, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void RemovingDownstreamBelt_LeavesUpstreamRuneBlockedSafely()
        {
            var system = new BeltTransportSystem(1f);
            BeltCell upstream = system.AddBelt(Vector2Int.zero, GridDirection.East);
            BeltCell downstream = system.AddBelt(Vector2Int.right, GridDirection.East);
            RuneData rune = CreateRune();
            upstream.TryAccept(rune, GridDirection.East);
            system.RemoveBelt(downstream);

            Assert.DoesNotThrow(() => system.Advance(2f));
            Assert.That(upstream.Item.Rune, Is.SameAs(rune));
            Assert.That(upstream.Item.Progress, Is.EqualTo(1f));
        }

        private static RuneData CreateRune()
        {
            return new RuneData(RuneBaseShape.Circle);
        }

        private sealed class BufferedOutputSource : IRuneOutputSource
        {
            private readonly RuneOutputBuffer buffer;

            public BufferedOutputSource(
                RuneOutputBuffer buffer,
                Vector2Int outputCell,
                GridDirection outputDirection)
            {
                this.buffer = buffer;
                OutputCell = outputCell;
                OutputDirection = outputDirection;
            }

            public Vector2Int OutputCell { get; }

            public GridDirection OutputDirection { get; }

            public bool HasOutput => buffer.HasOutput;

            public RuneData PeekOutput()
            {
                return buffer.PeekOutput();
            }

            public bool TryTakeOutput(out RuneData rune)
            {
                return buffer.TryTakeOutput(out rune);
            }
        }
    }
}
