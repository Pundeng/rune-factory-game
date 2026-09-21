using System;
using System.Collections.Generic;
using FantasyShapez.Grid;
using UnityEngine;

namespace FantasyShapez.Resources
{
    public sealed class RuneResourceMap : MonoBehaviour
    {
        [SerializeField] private GridSystem gridSystem = null;
        [SerializeField] private RuneStoneResourceNode[] resourceNodes =
            Array.Empty<RuneStoneResourceNode>();

        private readonly Dictionary<Vector2Int, RuneStoneResourceNode> resourcesByCell = new();
        private bool isCacheReady;

        public bool TryGetResource(Vector2Int cell, out RuneStoneResourceNode resourceNode)
        {
            EnsureCache();
            return resourcesByCell.TryGetValue(cell, out resourceNode);
        }

        private void EnsureCache()
        {
            if (isCacheReady)
            {
                return;
            }

            resourcesByCell.Clear();
            if (gridSystem != null)
            {
                foreach (RuneStoneResourceNode resourceNode in resourceNodes)
                {
                    RegisterResource(resourceNode);
                }

                RuneStoneResourceNode[] sceneResourceNodes =
                    FindObjectsByType<RuneStoneResourceNode>(
                        FindObjectsInactive.Exclude);
                foreach (RuneStoneResourceNode resourceNode in sceneResourceNodes)
                {
                    RegisterResource(resourceNode);
                }
            }

            isCacheReady = true;
        }

        private void RegisterResource(RuneStoneResourceNode resourceNode)
        {
            if (resourceNode == null)
            {
                return;
            }

            resourcesByCell[gridSystem.WorldToGrid(resourceNode.transform.position)] =
                resourceNode;
        }

        private void OnValidate()
        {
            isCacheReady = false;
        }
    }
}
