using System;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    [Serializable]
    public sealed class BuildingDefinition
    {
        [SerializeField] private string id = "PrototypeMachine";
        [SerializeField] private Vector2Int footprint = new(2, 1);
        [SerializeField] private GameObject visualPrefab = null;
        [SerializeField] private Color placedColor = new(0.3f, 0.65f, 0.9f, 1f);

        public string Id => id;

        public Vector2Int Footprint => footprint;

        public GameObject VisualPrefab => visualPrefab;

        public Color PlacedColor => placedColor;

        public Vector2Int GetRotatedFootprint(BuildingRotation rotation)
        {
            return rotation.GetRotatedFootprint(footprint);
        }

        public void Validate()
        {
            footprint.x = Mathf.Max(1, footprint.x);
            footprint.y = Mathf.Max(1, footprint.y);
        }
    }
}
