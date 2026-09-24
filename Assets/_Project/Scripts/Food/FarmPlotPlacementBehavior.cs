using System;
using FantasyShapez.Buildings;
using UnityEngine;

namespace FantasyShapez.Food
{
    public sealed class FarmPlotPlacementBehavior : MonoBehaviour, IBuildingPlacementBehavior
    {
        public bool CanPlace(
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation)
        {
            return true;
        }

        public void InitializePlacedBuilding(GameObject buildingObject, BuildingPlacement placement)
        {
            FarmPlot farmPlot = buildingObject.GetComponent<FarmPlot>();
            if (farmPlot == null)
            {
                throw new InvalidOperationException(
                    "The Farm Plot prefab must contain a FarmPlot component.");
            }

            farmPlot.Initialize(placement.AnchorCell);
        }
    }
}
