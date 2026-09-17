using System.Collections.Generic;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    public sealed class GridOccupancy
    {
        private readonly Dictionary<Vector2Int, BuildingPlacement> buildingsByCell = new();

        public int OccupiedCellCount => buildingsByCell.Count;

        public bool CanPlace(Vector2Int anchorCell, Vector2Int footprint, BuildingRotation rotation)
        {
            Vector2Int rotatedFootprint = rotation.GetRotatedFootprint(footprint);

            for (int y = 0; y < rotatedFootprint.y; y++)
            {
                for (int x = 0; x < rotatedFootprint.x; x++)
                {
                    if (buildingsByCell.ContainsKey(anchorCell + new Vector2Int(x, y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public bool TryRegister(
            string definitionId,
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation,
            out BuildingPlacement placement)
        {
            if (!CanPlace(anchorCell, footprint, rotation))
            {
                placement = null;
                return false;
            }

            placement = new BuildingPlacement(definitionId, anchorCell, footprint, rotation);
            foreach (Vector2Int cell in placement.OccupiedCells)
            {
                buildingsByCell.Add(cell, placement);
            }

            return true;
        }

        public bool TryGetBuilding(Vector2Int cell, out BuildingPlacement placement)
        {
            return buildingsByCell.TryGetValue(cell, out placement);
        }

        public bool Remove(BuildingPlacement placement)
        {
            if (placement == null)
            {
                return false;
            }

            bool removedAnyCell = false;
            foreach (Vector2Int cell in placement.OccupiedCells)
            {
                if (buildingsByCell.TryGetValue(cell, out BuildingPlacement occupyingBuilding) &&
                    ReferenceEquals(occupyingBuilding, placement))
                {
                    buildingsByCell.Remove(cell);
                    removedAnyCell = true;
                }
            }

            return removedAnyCell;
        }
    }
}
