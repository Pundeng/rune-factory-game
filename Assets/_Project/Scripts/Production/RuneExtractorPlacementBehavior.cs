using System;
using FantasyShapez.Buildings;
using FantasyShapez.Resources;
using UnityEngine;

namespace FantasyShapez.Production
{
    public sealed class RuneExtractorPlacementBehavior : MonoBehaviour, IBuildingPlacementBehavior
    {
        [SerializeField] private RuneResourceMap resourceMap = null;

        public bool CanPlace(
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation)
        {
            return resourceMap != null &&
                   resourceMap.TryGetResource(anchorCell, out _);
        }

        public void InitializePlacedBuilding(GameObject buildingObject, BuildingPlacement placement)
        {
            if (!resourceMap.TryGetResource(
                    placement.AnchorCell,
                    out RuneStoneResourceNode resourceNode))
            {
                throw new InvalidOperationException(
                    $"No rune stone resource exists at {placement.AnchorCell}.");
            }

            RuneExtractor extractor = buildingObject.GetComponent<RuneExtractor>();
            if (extractor == null)
            {
                throw new InvalidOperationException(
                    "The configured extractor prefab must contain a RuneExtractor component.");
            }

            extractor.Initialize(resourceNode);
        }
    }
}
