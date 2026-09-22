using System;
using FantasyShapez.Buildings;
using UnityEngine;

namespace FantasyShapez.Logistics
{
    public sealed class BeltPlacementBehavior :
        MonoBehaviour,
        IContinuousBuildingPlacementBehavior
    {
        [SerializeField] private BeltTransportCoordinator transportCoordinator = null;

        public bool CanPlace(
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation)
        {
            return transportCoordinator != null;
        }

        public void InitializePlacedBuilding(GameObject buildingObject, BuildingPlacement placement)
        {
            Belt belt = buildingObject.GetComponent<Belt>();
            if (belt == null)
            {
                throw new InvalidOperationException(
                    "The configured belt prefab must contain a Belt component.");
            }

            belt.Initialize(
                placement.AnchorCell,
                placement.Rotation.ToGridDirection(),
                transportCoordinator);
        }
    }
}
