using UnityEngine;

namespace FantasyShapez.Buildings
{
    public interface IBuildingPlacementBehavior
    {
        bool CanPlace(
            Vector2Int anchorCell,
            Vector2Int footprint,
            BuildingRotation rotation);

        void InitializePlacedBuilding(GameObject buildingObject, BuildingPlacement placement);
    }
}
