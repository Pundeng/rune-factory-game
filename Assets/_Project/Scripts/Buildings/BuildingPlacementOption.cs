using System;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    [Serializable]
    public sealed class BuildingPlacementOption
    {
        [SerializeField] private BuildingDefinition definition = new();
        [SerializeField] private MonoBehaviour placementBehavior = null;

        public BuildingDefinition Definition => definition;

        public IBuildingPlacementBehavior PlacementBehavior =>
            placementBehavior as IBuildingPlacementBehavior;

        public void Validate()
        {
            definition?.Validate();

            if (placementBehavior != null && placementBehavior is not IBuildingPlacementBehavior)
            {
                placementBehavior = null;
            }
        }
    }
}
