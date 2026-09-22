using System;
using System.Collections.Generic;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class ElementInfuserPlacementBehavior :
        MonoBehaviour,
        IBuildingPlacementBehavior,
        IBuildingPortPreviewProvider
    {
        [SerializeField] private BeltTransportCoordinator transportCoordinator = null;

        public IReadOnlyList<BuildingPortPreview> PortPreviews =>
            BuildingPortPreviewLayouts.DirectionalProcessor;

        public bool CanPlace(
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation)
        {
            return transportCoordinator != null;
        }

        public void InitializePlacedBuilding(GameObject buildingObject, BuildingPlacement placement)
        {
            ElementInfuser infuser = buildingObject.GetComponent<ElementInfuser>();
            if (infuser == null)
            {
                throw new InvalidOperationException(
                    "The configured element infuser prefab must contain an ElementInfuser component.");
            }

            infuser.Initialize(
                placement.AnchorCell,
                placement.Rotation.ToGridDirection(),
                transportCoordinator);
        }
    }
}
