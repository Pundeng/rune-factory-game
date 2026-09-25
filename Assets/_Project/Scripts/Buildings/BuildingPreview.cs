using System;
using System.Collections.Generic;
using FantasyShapez.Grid;
using UnityEngine;

namespace FantasyShapez.Buildings
{
    public sealed class BuildingPreview : MonoBehaviour
    {
        [SerializeField] private Color validColor = new(0.25f, 0.9f, 0.45f, 0.55f);
        [SerializeField] private Color invalidColor = new(0.95f, 0.25f, 0.25f, 0.55f);

        private BuildingDefinition currentDefinition;
        private IReadOnlyList<BuildingPortPreview> currentPortPreviews;
        private GameObject directionIndicator;
        private GameObject portIndicatorsRoot;
        private GameObject visualRoot;

        public void Show(
            BuildingPlacementOption option,
            GridSystem gridSystem,
            Vector2Int anchorCell,
            BuildingRotation rotation,
            bool isValid)
        {
            BuildingDefinition definition = option.Definition;
            EnsureVisual(definition, gridSystem.CellSize);
            EnsureDirectionIndicator(gridSystem.CellSize);
            EnsurePortIndicators(option.PortPreviews, gridSystem.CellSize);

            bool hasPortPreviews = option.PortPreviews.Count > 0;
            directionIndicator.SetActive(!hasPortPreviews);
            portIndicatorsRoot.SetActive(hasPortPreviews);

            Vector2Int rotatedFootprint = definition.GetRotatedFootprint(rotation);
            Vector3 firstCellCenter = gridSystem.GridToWorld(anchorCell);
            transform.position = firstCellCenter + new Vector3(
                (rotatedFootprint.x - 1) * gridSystem.CellSize * 0.5f,
                (rotatedFootprint.y - 1) * gridSystem.CellSize * 0.5f,
                -0.02f);
            transform.rotation = Quaternion.Euler(0f, 0f, -(int)rotation);
            BuildingVisualFactory.Tint(visualRoot, isValid ? validColor : invalidColor);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void EnsureVisual(BuildingDefinition definition, float cellSize)
        {
            if (visualRoot != null && ReferenceEquals(currentDefinition, definition))
            {
                return;
            }

            if (visualRoot != null)
            {
                Destroy(visualRoot);
            }

            currentDefinition = definition;
            visualRoot = BuildingVisualFactory.Create(definition, transform, cellSize, 75);
        }

        private void EnsureDirectionIndicator(float cellSize)
        {
            if (directionIndicator != null)
            {
                return;
            }

            directionIndicator = new GameObject("Direction Indicator");
            directionIndicator.transform.SetParent(transform, false);
            CreateIndicatorPart(
                directionIndicator.transform,
                "Shaft",
                new Vector2(0f, 0.12f) * cellSize,
                new Vector2(0.09f, 0.42f) * cellSize,
                0f,
                Color.white);
            CreateIndicatorPart(
                directionIndicator.transform,
                "Left",
                new Vector2(-0.1f, 0.29f) * cellSize,
                new Vector2(0.09f, 0.26f) * cellSize,
                -45f,
                Color.white);
            CreateIndicatorPart(
                directionIndicator.transform,
                "Right",
                new Vector2(0.1f, 0.29f) * cellSize,
                new Vector2(0.09f, 0.26f) * cellSize,
                45f,
                Color.white);
        }

        private void EnsurePortIndicators(
            IReadOnlyList<BuildingPortPreview> portPreviews,
            float cellSize)
        {
            if (portIndicatorsRoot != null &&
                ReferenceEquals(currentPortPreviews, portPreviews))
            {
                return;
            }

            if (portIndicatorsRoot != null)
            {
                Destroy(portIndicatorsRoot);
            }

            currentPortPreviews = portPreviews;
            portIndicatorsRoot = new GameObject("Port Indicators");
            portIndicatorsRoot.transform.SetParent(transform, false);
            for (int index = 0; index < portPreviews.Count; index++)
            {
                CreatePortIndicator(portPreviews[index], index, cellSize);
            }
        }

        private void CreatePortIndicator(
            BuildingPortPreview port,
            int index,
            float cellSize)
        {
            var indicator = new GameObject($"{port.Kind} Port {index + 1}");
            indicator.transform.SetParent(portIndicatorsRoot.transform, false);
            indicator.transform.localPosition =
                new Vector3(port.LocalPosition.x, port.LocalPosition.y, -0.04f) * cellSize;
            indicator.transform.localRotation =
                Quaternion.Euler(0f, 0f, -(int)port.LocalDirection);
            Color color = port.Kind switch
            {
                BuildingPortKind.Input => BuildingPortPreviewLayouts.InputColor,
                BuildingPortKind.PropertyInput => BuildingPortPreviewLayouts.PropertyInputColor,
                _ => BuildingPortPreviewLayouts.OutputColor
            };
            CreateIndicatorPart(
                indicator.transform,
                "Shaft",
                new Vector2(0f, 0.04f) * cellSize,
                new Vector2(0.07f, 0.24f) * cellSize,
                0f,
                color);
            CreateIndicatorPart(
                indicator.transform,
                "Left",
                new Vector2(-0.06f, 0.12f) * cellSize,
                new Vector2(0.07f, 0.16f) * cellSize,
                -45f,
                color);
            CreateIndicatorPart(
                indicator.transform,
                "Right",
                new Vector2(0.06f, 0.12f) * cellSize,
                new Vector2(0.07f, 0.16f) * cellSize,
                45f,
                color);
        }

        private void CreateIndicatorPart(
            Transform parent,
            string partName,
            Vector2 localPosition,
            Vector2 localScale,
            float angle,
            Color color)
        {
            var part = new GameObject(partName);
            part.transform.SetParent(parent, false);
            part.transform.localPosition = new Vector3(localPosition.x, localPosition.y, -0.03f);
            part.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            part.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);
            SpriteRenderer renderer = part.AddComponent<SpriteRenderer>();
            renderer.sprite = BuildingVisualFactory.PlaceholderSprite;
            renderer.color = color;
            renderer.sortingOrder = 80;
        }
    }

    public enum BuildingPortKind
    {
        Input,
        Output,
        PropertyInput
    }

    public readonly struct BuildingPortPreview
    {
        public BuildingPortPreview(
            BuildingPortKind kind,
            Vector2 localPosition,
            BuildingRotation localDirection)
        {
            Kind = kind;
            LocalPosition = localPosition;
            LocalDirection = localDirection;
        }

        public BuildingPortKind Kind { get; }

        public Vector2 LocalPosition { get; }

        public BuildingRotation LocalDirection { get; }

        public BuildingRotation ResolveDirection(BuildingRotation buildingRotation)
        {
            int combinedRotation = ((int)LocalDirection + (int)buildingRotation) % 360;
            return (BuildingRotation)combinedRotation;
        }
    }

    public static class BuildingPortPreviewLayouts
    {
        public static readonly Color InputColor = new(0.2f, 0.75f, 1f, 1f);
        public static readonly Color OutputColor = new(1f, 0.55f, 0.15f, 1f);
        public static readonly Color PropertyInputColor = new(0.65f, 0.9f, 0.5f, 1f);

        public static readonly IReadOnlyList<BuildingPortPreview> DirectionalProcessor =
            new[]
            {
                new BuildingPortPreview(
                    BuildingPortKind.Input,
                    new Vector2(0f, -0.36f),
                    BuildingRotation.Degrees0),
                new BuildingPortPreview(
                    BuildingPortKind.Output,
                    new Vector2(0f, 0.36f),
                    BuildingRotation.Degrees0)
            };

        public static readonly IReadOnlyList<BuildingPortPreview> OutputOnly =
            new[]
            {
                new BuildingPortPreview(
                    BuildingPortKind.Output,
                    new Vector2(0f, 0.36f),
                    BuildingRotation.Degrees0)
            };
    }
}
