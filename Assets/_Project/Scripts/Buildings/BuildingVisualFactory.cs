using UnityEngine;

namespace FantasyShapez.Buildings
{
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
