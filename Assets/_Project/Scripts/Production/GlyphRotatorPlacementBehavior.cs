using System;
using System.Collections.Generic;
using FantasyShapez.Buildings;
using FantasyShapez.Logistics;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class GlyphRotatorPlacementBehavior :
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
            GlyphRotator rotator = buildingObject.GetComponent<GlyphRotator>();
            if (rotator == null)
            {
                throw new InvalidOperationException(
                    "The configured glyph rotator prefab must contain a GlyphRotator component.");
            }

            rotator.Initialize(
                placement.AnchorCell,
                placement.Rotation.ToGridDirection(),
                transportCoordinator);
        }
    }
}
