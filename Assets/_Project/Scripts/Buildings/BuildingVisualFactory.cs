using UnityEngine;
using System.Collections.Generic;

namespace FantasyShapez.Buildings
{
    internal enum MachineVisualStatus
    {
        Working,
        Waiting,
        MissingSupply,
        BlockedOutput,
        InvalidRecipe
    }

    internal static class BuildingVisualFactory
    {
        private static Sprite placeholderSprite;

        public static Sprite PlaceholderSprite => GetPlaceholderSprite();

        public static GameObject Create(
            BuildingDefinition definition,
            Transform parent,
            float cellSize,
            int sortingOrder)
        {
            if (definition.VisualPrefab != null)
            {
                GameObject prefabVisual = Object.Instantiate(definition.VisualPrefab, parent);
                prefabVisual.name = definition.VisualPrefab.name;
                prefabVisual.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                return prefabVisual;
            }

            var placeholder = new GameObject("Placeholder Visual");
            placeholder.transform.SetParent(parent, false);
            if (definition.HasExplicitFootprint)
            {
                foreach (Vector2Int cell in definition.OccupiedCells)
                {
                    var tile = new GameObject($"Cell {cell.x},{cell.y}");
                    tile.transform.SetParent(placeholder.transform, false);
                    tile.transform.localPosition = new Vector3(
                        (cell.x - (definition.Footprint.x - 1) * 0.5f) * cellSize,
                        (cell.y - (definition.Footprint.y - 1) * 0.5f) * cellSize,
                        0f);
                    tile.transform.localScale = new Vector3(cellSize, cellSize, 1f);
                    SpriteRenderer tileRenderer = tile.AddComponent<SpriteRenderer>();
                    tileRenderer.sprite = GetPlaceholderSprite();
                    tileRenderer.sortingOrder = sortingOrder;
                }

                return placeholder;
            }

            placeholder.transform.localScale = new Vector3(
                definition.Footprint.x * cellSize,
                definition.Footprint.y * cellSize,
                1f);

            SpriteRenderer renderer = placeholder.AddComponent<SpriteRenderer>();
            renderer.sprite = GetPlaceholderSprite();
            renderer.sortingOrder = sortingOrder;
            return placeholder;
        }

        public static void Tint(GameObject visualRoot, Color color)
        {
            foreach (SpriteRenderer renderer in visualRoot.GetComponentsInChildren<SpriteRenderer>())
            {
                renderer.color = color;
            }
        }

        public static void CreatePortMarkers(Transform parent,
            IReadOnlyList<BuildingPortPreview> ports, float cellSize,
            BuildingRotation rotation)
        {
            int inputs = 0;
            int outputs = 0;
            int inputCount = 0;
            int outputCount = 0;
            foreach (BuildingPortPreview port in ports)
            {
                if (port.Kind == BuildingPortKind.Input) inputCount++;
                if (port.Kind == BuildingPortKind.Output) outputCount++;
            }
            foreach (BuildingPortPreview port in ports)
            {
                int ordinal = port.Kind switch
                {
                    BuildingPortKind.Input => ++inputs,
                    BuildingPortKind.Output => ++outputs,
                    _ => 0
                };
                string label = port.Kind switch
                {
                    BuildingPortKind.Input => "IN",
                    BuildingPortKind.Output => "OUT",
                    _ => "PROP"
                };
                if (port.Kind == BuildingPortKind.Input && inputCount > 1 ||
                    port.Kind == BuildingPortKind.Output && outputCount > 1)
                    label += ordinal == 1 ? " A" : " B";
                var marker = new GameObject($"{label} port");
                marker.transform.SetParent(parent, false);
                marker.transform.localPosition = new Vector3(
                    port.LocalPosition.x * cellSize,
                    port.LocalPosition.y * cellSize, -0.04f);
                marker.transform.localScale = Vector3.one * 0.24f * cellSize;
                if (port.Kind == BuildingPortKind.PropertyInput)
                    marker.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                SpriteRenderer renderer = marker.AddComponent<SpriteRenderer>();
                renderer.sprite = PlaceholderSprite;
                renderer.color = port.Kind switch
                {
                    BuildingPortKind.Input => BuildingPortPreviewLayouts.InputColor,
                    BuildingPortKind.Output => BuildingPortPreviewLayouts.OutputColor,
                    _ => BuildingPortPreviewLayouts.PropertyInputColor
                };
                renderer.sortingOrder = 22;
                var labelObject = new GameObject($"{label} label");
                labelObject.transform.SetParent(parent, false);
                labelObject.transform.localPosition = marker.transform.localPosition +
                    new Vector3(0f, -0.22f * cellSize, -0.05f);
                labelObject.transform.localRotation = Quaternion.Euler(0f, 0f,
                    (int)rotation);
                TextMesh text = labelObject.AddComponent<TextMesh>();
                text.text = label;
                text.fontSize = 32;
                text.characterSize = 0.1f * cellSize;
                text.anchor = TextAnchor.MiddleCenter;
                text.color = Color.white;
                text.GetComponent<MeshRenderer>().sortingOrder = 23;
            }
        }

        public static TextMesh CreateStatusLabel(Transform parent,
            Vector2Int footprint, float cellSize, BuildingRotation rotation)
        {
            var root = new GameObject("Machine status");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f,
                (footprint.y * 0.5f + 0.18f) * cellSize, -0.06f);
            root.transform.localRotation = Quaternion.Euler(0f, 0f,
                (int)rotation);
            var backing = new GameObject("Status backing");
            backing.transform.SetParent(root.transform, false);
            backing.transform.localScale = new Vector3(1.1f * cellSize,
                0.28f * cellSize, 1f);
            SpriteRenderer background = backing.AddComponent<SpriteRenderer>();
            background.sprite = PlaceholderSprite;
            background.color = new Color(0.09f, 0.13f, 0.17f, 0.88f);
            background.sortingOrder = 30;
            TextMesh label = root.AddComponent<TextMesh>();
            label.fontSize = 32;
            label.characterSize = 0.12f * cellSize;
            label.anchor = TextAnchor.MiddleCenter;
            label.GetComponent<MeshRenderer>().sortingOrder = 31;
            SetStatus(label, MachineVisualStatus.Waiting);
            return label;
        }

        public static void SetStatus(TextMesh label, MachineVisualStatus status)
        {
            if (label == null) return;
            label.text = status switch
            {
                MachineVisualStatus.Working => "WORKING",
                MachineVisualStatus.MissingSupply => "NO SUPPLY",
                MachineVisualStatus.BlockedOutput => "OUTPUT BLOCKED",
                MachineVisualStatus.InvalidRecipe => "NO RECIPE",
                _ => "WAITING"
            };
            label.color = status switch
            {
                MachineVisualStatus.Working => new Color(0.35f, 0.92f, 0.55f),
                MachineVisualStatus.MissingSupply => new Color(0.45f, 0.8f, 1f),
                MachineVisualStatus.BlockedOutput => new Color(1f, 0.4f, 0.32f),
                MachineVisualStatus.InvalidRecipe => new Color(1f, 0.72f, 0.35f),
                _ => new Color(0.95f, 0.95f, 0.82f)
            };
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (placeholderSprite == null)
            {
                placeholderSprite = Sprite.Create(
                    Texture2D.whiteTexture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                placeholderSprite.name = "Runtime Building Placeholder";
            }

            return placeholderSprite;
        }
    }
}
