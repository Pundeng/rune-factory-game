using System.Linq;
using FantasyShapez.Buildings;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class GridOccupancyTests
    {
        [Test]
        public void RegisteringFootprint_OccupiesEveryCoveredCell()
        {
            var occupancy = new GridOccupancy();

            bool registered = occupancy.TryRegister(
                "PrototypeMachine",
                new Vector2Int(3, 4),
                new Vector2Int(2, 2),
                BuildingRotation.Degrees0,
                out BuildingPlacement placement);

            Assert.That(registered, Is.True);
            CollectionAssert.AreEquivalent(
                new[]
                {
                    new Vector2Int(3, 4),
                    new Vector2Int(4, 4),
                    new Vector2Int(3, 5),
                    new Vector2Int(4, 5)
                },
                placement.OccupiedCells.ToArray());
            Assert.That(occupancy.OccupiedCellCount, Is.EqualTo(4));
        }

        [Test]
        public void OverlappingPlacement_IsRejectedWithoutChangingOccupancy()
        {
            var occupancy = new GridOccupancy();
            occupancy.TryRegister(
                "First",
                Vector2Int.zero,
                new Vector2Int(2, 1),
                BuildingRotation.Degrees0,
                out _);

            bool registered = occupancy.TryRegister(
                "Overlapping",
                new Vector2Int(1, 0),
                Vector2Int.one,
                BuildingRotation.Degrees0,
                out BuildingPlacement placement);

            Assert.That(registered, Is.False);
            Assert.That(placement, Is.Null);
            Assert.That(occupancy.OccupiedCellCount, Is.EqualTo(2));
        }

        [Test]
        public void RemovingBuilding_FreesAllOfItsCells()
        {
            var occupancy = new GridOccupancy();
            occupancy.TryRegister(
                "PrototypeMachine",
                Vector2Int.zero,
                new Vector2Int(2, 1),
                BuildingRotation.Degrees0,
                out BuildingPlacement placement);

            bool removed = occupancy.Remove(placement);

            Assert.That(removed, Is.True);
            Assert.That(occupancy.OccupiedCellCount, Is.Zero);
            Assert.That(
                occupancy.CanPlace(Vector2Int.zero, new Vector2Int(2, 1), BuildingRotation.Degrees0),
                Is.True);
        }

        [Test]
        public void RemovedCell_CanRegisterNewBuilding()
        {
            var occupancy = new GridOccupancy();
            occupancy.TryRegister(
                "Belt",
                Vector2Int.zero,
                Vector2Int.one,
                BuildingRotation.Degrees0,
                out BuildingPlacement removedPlacement);
            occupancy.Remove(removedPlacement);

            bool registered = occupancy.TryRegister(
                "Replacement",
                Vector2Int.zero,
                Vector2Int.one,
                BuildingRotation.Degrees90,
                out BuildingPlacement replacement);

            Assert.That(registered, Is.True);
            Assert.That(replacement.AnchorCell, Is.EqualTo(Vector2Int.zero));
        }

        [Test]
        public void RectangularFootprint_AtNinetyDegrees_SwapsDimensions()
        {
            Vector2Int rotated = BuildingRotation.Degrees90.GetRotatedFootprint(new Vector2Int(2, 1));

            Assert.That(rotated, Is.EqualTo(new Vector2Int(1, 2)));
        }

        [Test]
        public void DragTracker_FillsSkippedCellsWithContinuousOrthogonalPath()
        {
            var tracker = new GridDragTracker();

            CollectionAssert.AreEqual(
                new[] { Vector2Int.zero },
                tracker.Continue(Vector2Int.zero));
            CollectionAssert.AreEqual(
                new[]
                {
                    new Vector2Int(1, 0),
                    new Vector2Int(2, 0),
                    new Vector2Int(2, 1),
                    new Vector2Int(2, 2)
                },
                tracker.Continue(new Vector2Int(2, 2)));
        }

        [Test]
        public void DragTracker_DoesNotReturnVisitedCellTwice()
        {
            var tracker = new GridDragTracker();
            tracker.Continue(Vector2Int.zero);
            tracker.Continue(new Vector2Int(2, 0));

            Assert.That(tracker.Continue(Vector2Int.zero), Is.Empty);
        }

        [Test]
        public void BeltDragPlanner_FollowsStraightPathAndNinetyDegreeTurn()
        {
            var planner = new BeltDragPlacementPlanner();

            var steps = planner.Continue(new[]
            {
                new Vector2Int(0, 0),
                new Vector2Int(1, 0),
                new Vector2Int(1, 1)
            });

            Assert.That(steps.Count, Is.EqualTo(2));
            Assert.That(steps[0].Cell, Is.EqualTo(new Vector2Int(0, 0)));
            Assert.That(steps[0].Rotation, Is.EqualTo(BuildingRotation.Degrees90));
            Assert.That(steps[1].Cell, Is.EqualTo(new Vector2Int(1, 0)));
            Assert.That(steps[1].Rotation, Is.EqualTo(BuildingRotation.Degrees0));
            Assert.That(
                planner.TryComplete(
                    BuildingRotation.Degrees180,
                    out BeltPlacementStep finalStep),
                Is.True);
            Assert.That(finalStep.Cell, Is.EqualTo(new Vector2Int(1, 1)));
            Assert.That(finalStep.Rotation, Is.EqualTo(BuildingRotation.Degrees0));
        }

        [Test]
        public void BeltDragPlanner_SingleCellUsesManualRotation()
        {
            var planner = new BeltDragPlacementPlanner();
            planner.Continue(new[] { Vector2Int.zero });

            bool completed = planner.TryComplete(
                BuildingRotation.Degrees270,
                out BeltPlacementStep step);

            Assert.That(completed, Is.True);
            Assert.That(step.Cell, Is.EqualTo(Vector2Int.zero));
            Assert.That(step.Rotation, Is.EqualTo(BuildingRotation.Degrees270));
        }

        [TestCase(BuildingRotation.Degrees0)]
        [TestCase(BuildingRotation.Degrees90)]
        [TestCase(BuildingRotation.Degrees180)]
        [TestCase(BuildingRotation.Degrees270)]
        public void DirectionalPortPreview_ResolvesToBuildingRotation(
            BuildingRotation buildingRotation)
        {
            var ports = BuildingPortPreviewLayouts.DirectionalProcessor;

            Assert.That(ports.Count, Is.EqualTo(2));
            Assert.That(ports[0].Kind, Is.EqualTo(BuildingPortKind.Input));
            Assert.That(ports[0].LocalPosition.y, Is.LessThan(0f));
            Assert.That(ports[0].ResolveDirection(buildingRotation), Is.EqualTo(buildingRotation));
            Assert.That(ports[1].Kind, Is.EqualTo(BuildingPortKind.Output));
            Assert.That(ports[1].LocalPosition.y, Is.GreaterThan(0f));
            Assert.That(ports[1].ResolveDirection(buildingRotation), Is.EqualTo(buildingRotation));
        }

        [Test]
        public void DragTracker_ResetAllowsCellOnNextDrag()
        {
            var tracker = new GridDragTracker();
            tracker.Continue(Vector2Int.zero);

            tracker.Reset();

            CollectionAssert.AreEqual(
                new[] { Vector2Int.zero },
                tracker.Continue(Vector2Int.zero));
        }

        [Test]
        public void ContinuousBeltDrag_RejectsHubCellButKeepsAdjacentCellsAvailable()
        {
            var occupancy = new GridOccupancy();
            var tracker = new GridDragTracker();
            occupancy.TryRegister(
                "Hub",
                new Vector2Int(1, 0),
                Vector2Int.one,
                BuildingRotation.Degrees0,
                out _);

            foreach (Vector2Int cell in tracker.Continue(Vector2Int.zero))
            {
                occupancy.TryRegister(
                    "Belt",
                    cell,
                    Vector2Int.one,
                    BuildingRotation.Degrees0,
                    out _);
            }

            foreach (Vector2Int cell in tracker.Continue(new Vector2Int(2, 0)))
            {
                occupancy.TryRegister(
                    "Belt",
                    cell,
                    Vector2Int.one,
                    BuildingRotation.Degrees0,
                    out _);
            }

            Assert.That(occupancy.OccupiedCellCount, Is.EqualTo(3));
            Assert.That(
                occupancy.TryGetBuilding(new Vector2Int(1, 0), out BuildingPlacement hub),
                Is.True);
            Assert.That(hub.DefinitionId, Is.EqualTo("Hub"));
            Assert.That(
                occupancy.TryGetBuilding(Vector2Int.zero, out BuildingPlacement firstBelt),
                Is.True);
            Assert.That(firstBelt.DefinitionId, Is.EqualTo("Belt"));
            Assert.That(
                occupancy.TryGetBuilding(new Vector2Int(2, 0), out BuildingPlacement lastBelt),
                Is.True);
            Assert.That(lastBelt.DefinitionId, Is.EqualTo("Belt"));
            Assert.That(tracker.Continue(Vector2Int.zero), Is.Empty);
        }

        [Test]
        public void HubFootprint_RejectsOverlappingBuildingWithoutBlockingAdjacentInputBelt()
        {
            var occupancy = new GridOccupancy();
            var hubCell = new Vector2Int(10, 1);
            occupancy.TryRegister(
                "Hub",
                hubCell,
                Vector2Int.one,
                BuildingRotation.Degrees0,
                out _);

            bool overlappingBuildingRegistered = occupancy.TryRegister(
                "Machine",
                hubCell + Vector2Int.left,
                new Vector2Int(2, 1),
                BuildingRotation.Degrees0,
                out _);
            bool adjacentInputBeltRegistered = occupancy.TryRegister(
                "Belt",
                hubCell + Vector2Int.left,
                Vector2Int.one,
                BuildingRotation.Degrees90,
                out _);

            Assert.That(overlappingBuildingRegistered, Is.False);
            Assert.That(adjacentInputBeltRegistered, Is.True);
            Assert.That(occupancy.OccupiedCellCount, Is.EqualTo(2));
        }

        [Test]
        public void Occupancy_SupportsNegativeCoordinates()
        {
            var occupancy = new GridOccupancy();

            bool registered = occupancy.TryRegister(
                "PrototypeMachine",
                new Vector2Int(-2, -3),
                new Vector2Int(2, 1),
                BuildingRotation.Degrees0,
                out BuildingPlacement placement);

            Assert.That(registered, Is.True);
            Assert.That(placement.OccupiedCells, Does.Contain(new Vector2Int(-2, -3)));
            Assert.That(placement.OccupiedCells, Does.Contain(new Vector2Int(-1, -3)));
            Assert.That(occupancy.CanPlace(new Vector2Int(-1, -3), Vector2Int.one, BuildingRotation.Degrees0), Is.False);
        }
    }
}
