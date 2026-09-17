using System;
using System.Collections.Generic;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    public sealed class BuildingPlacement
    {
        private readonly Vector2Int[] occupiedCells;

        public BuildingPlacement(
            string definitionId,
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
            {
                throw new ArgumentException("A building definition ID is required.", nameof(definitionId));
            }

            DefinitionId = definitionId;
            AnchorCell = anchorCell;
            Footprint = footprint;
            Rotation = rotation;
            RotatedFootprint = rotation.GetRotatedFootprint(footprint);
            occupiedCells = CreateOccupiedCells(anchorCell, RotatedFootprint);
        }

        public string DefinitionId { get; }

        public Vector2Int AnchorCell { get; }

        public Vector2Int Footprint { get; }

        public BuildingRotation Rotation { get; }

        public Vector2Int RotatedFootprint { get; }

        public IReadOnlyList<Vector2Int> OccupiedCells => occupiedCells;

        private static Vector2Int[] CreateOccupiedCells(Vector2Int anchorCell, Vector2Int footprint)
        {
            var cells = new Vector2Int[footprint.x * footprint.y];
            int index = 0;

            for (int y = 0; y < footprint.y; y++)
            {
                for (int x = 0; x < footprint.x; x++)
                {
                    cells[index++] = anchorCell + new Vector2Int(x, y);
                }
            }

            return cells;
        }
    }
}
