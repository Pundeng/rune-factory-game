using System;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class EngraverPlacementBehavior : MonoBehaviour, IBuildingPlacementBehavior
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
            Engraver engraver = buildingObject.GetComponent<Engraver>();
            if (engraver == null)
            {
                throw new InvalidOperationException(
                    "The configured engraver prefab must contain an Engraver component.");
            }

            engraver.Initialize(
                placement.AnchorCell,
                placement.Rotation.ToGridDirection(),
                transportCoordinator);
        }
    }
}
