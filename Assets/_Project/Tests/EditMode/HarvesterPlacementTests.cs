using FantasyShapez.Buildings;
using FantasyShapez.Food;
using FantasyShapez.Logistics;
using NUnit.Framework;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class HarvesterPlacementTests
    {
        private static readonly Vector2Int Footprint = new(1, 2);

        [TestCase(BuildingRotation.Degrees0, 4, 7, 4, 8, 4, 9)]
        [TestCase(BuildingRotation.Degrees90, 4, 7, 5, 7, 6, 7)]
        [TestCase(BuildingRotation.Degrees180, 4, 8, 4, 7, 4, 6)]
        [TestCase(BuildingRotation.Degrees270, 5, 7, 4, 7, 3, 7)]
        public void RotatedHarvester_CoversPlotAndOutputsBeyondFreeCell(
            BuildingRotation rotation,
            int farmX, int farmY,
            int machineX, int machineY,
            int outputX, int outputY)
        {
            var anchor = new Vector2Int(4, 7);
            Vector2Int farmCell = HarvesterPlacementBehavior.GetFarmCell(
                anchor, Footprint, rotation);
            Vector2Int machineCell = farmCell + rotation.ToGridDirection().ToOffset();
            Vector2Int outputCell = machineCell + rotation.ToGridDirection().ToOffset();

            Assert.That(farmCell, Is.EqualTo(new Vector2Int(farmX, farmY)));
            Assert.That(HarvesterPlacementBehavior.GetAnchorForFarmCell(
                farmCell, Footprint, rotation), Is.EqualTo(anchor));
            Assert.That(machineCell, Is.EqualTo(new Vector2Int(machineX, machineY)));
            Assert.That(outputCell, Is.EqualTo(new Vector2Int(outputX, outputY)));

            var occupancy = new GridOccupancy();
            Assert.That(occupancy.TryRegister(nameof(FarmPlot), farmCell, Vector2Int.one,
                BuildingRotation.Degrees0, out BuildingPlacement plot), Is.True);
            Assert.That(occupancy.TryRegisterOver(nameof(Harvester), anchor, Footprint,
                rotation, farmCell, plot, out BuildingPlacement harvester), Is.True);
            Assert.That(occupancy.OccupiedCellCount, Is.EqualTo(2));
            Assert.That(occupancy.TryGetBuilding(farmCell, out BuildingPlacement top), Is.True);
            Assert.That(top, Is.SameAs(harvester));
            Assert.That(occupancy.TryGetUnderlyingBuilding(farmCell,
                out BuildingPlacement covered), Is.True);
            Assert.That(covered, Is.SameAs(plot));
            Assert.That(occupancy.CanPlace(machineCell, Vector2Int.one,
                BuildingRotation.Degrees0), Is.False);
            Assert.That(occupancy.Remove(plot), Is.False);

            Assert.That(occupancy.Remove(harvester), Is.True);
            Assert.That(occupancy.TryGetBuilding(farmCell, out top), Is.True);
            Assert.That(top, Is.SameAs(plot));
            Assert.That(occupancy.CanPlace(machineCell, Vector2Int.one,
                BuildingRotation.Degrees0), Is.True);
        }

        [Test]
        public void Harvester_RequiresFreeSecondCellAndCannotStackOnAnotherHarvester()
        {
            var anchor = Vector2Int.zero;
            var occupancy = new GridOccupancy();
            Assert.That(occupancy.TryRegister(nameof(FarmPlot), anchor, Vector2Int.one,
                BuildingRotation.Degrees0, out BuildingPlacement plot), Is.True);
            Assert.That(occupancy.TryRegister("Belt", Vector2Int.up, Vector2Int.one,
                BuildingRotation.Degrees0, out BuildingPlacement belt), Is.True);
            Assert.That(occupancy.CanPlaceOver(anchor, Footprint, BuildingRotation.Degrees0,
                anchor, plot), Is.False);
            Assert.That(occupancy.Remove(belt), Is.True);
            Assert.That(occupancy.TryRegisterOver(nameof(Harvester), anchor, Footprint,
                BuildingRotation.Degrees0, anchor, plot, out _), Is.True);
            Assert.That(occupancy.CanPlaceOver(anchor, Footprint, BuildingRotation.Degrees0,
                anchor, plot), Is.False);
        }

        [TestCase(BuildingRotation.Degrees90, true)]
        [TestCase(BuildingRotation.Degrees90, false)]
        [TestCase(BuildingRotation.Degrees270, true)]
        [TestCase(BuildingRotation.Degrees270, false)]
        public void VerticallyAdjacentPlots_AcceptIndependentHorizontalHarvesters(
            BuildingRotation rotation, bool upperFirst)
        {
            var occupancy = new GridOccupancy();
            var lowerCell = new Vector2Int(-10, 1);
            var upperCell = lowerCell + Vector2Int.up;
            Assert.That(occupancy.TryRegister(nameof(FarmPlot), lowerCell, Vector2Int.one,
                BuildingRotation.Degrees0, out BuildingPlacement lowerPlot), Is.True);
            Assert.That(occupancy.TryRegister(nameof(FarmPlot), upperCell, Vector2Int.one,
                BuildingRotation.Degrees0, out BuildingPlacement upperPlot), Is.True);

            Vector2Int firstCell = upperFirst ? upperCell : lowerCell;
            Vector2Int secondCell = upperFirst ? lowerCell : upperCell;
            BuildingPlacement firstPlot = upperFirst ? upperPlot : lowerPlot;
            BuildingPlacement secondPlot = upperFirst ? lowerPlot : upperPlot;
            Vector2Int firstAnchor = HarvesterPlacementBehavior.GetAnchorForFarmCell(
                firstCell, Footprint, rotation);
            Vector2Int secondAnchor = HarvesterPlacementBehavior.GetAnchorForFarmCell(
                secondCell, Footprint, rotation);

            Assert.That(occupancy.TryRegisterOver(nameof(Harvester), firstAnchor, Footprint,
                rotation, firstCell, firstPlot, out BuildingPlacement firstHarvester), Is.True);
            Assert.That(occupancy.TryRegisterOver(nameof(Harvester), secondAnchor, Footprint,
                rotation, secondCell, secondPlot, out BuildingPlacement secondHarvester), Is.True);

            Vector2Int direction = rotation.ToGridDirection().ToOffset();
            Assert.That(firstHarvester.OccupiedCells,
                Is.EquivalentTo(new[] { firstCell, firstCell + direction }));
            Assert.That(secondHarvester.OccupiedCells,
                Is.EquivalentTo(new[] { secondCell, secondCell + direction }));
            Assert.That(occupancy.OccupiedCellCount, Is.EqualTo(4));
            Assert.That(occupancy.TryGetBuilding(firstCell, out BuildingPlacement firstAtPlot),
                Is.True);
            Assert.That(firstAtPlot, Is.SameAs(firstHarvester));
            Assert.That(occupancy.TryGetBuilding(secondCell, out BuildingPlacement secondAtPlot),
                Is.True);
            Assert.That(secondAtPlot, Is.SameAs(secondHarvester));
            Assert.That(occupancy.TryGetUnderlyingBuilding(firstCell, out BuildingPlacement firstUnderlying),
                Is.True);
            Assert.That(firstUnderlying, Is.SameAs(firstPlot));
            Assert.That(occupancy.TryGetUnderlyingBuilding(secondCell, out BuildingPlacement secondUnderlying),
                Is.True);
            Assert.That(secondUnderlying, Is.SameAs(secondPlot));
        }
    }
}
