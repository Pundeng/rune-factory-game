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
