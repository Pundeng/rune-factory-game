using UnityEngine;

namespace FantasyShapez.Runes
{
    public sealed class RuneVisual : MonoBehaviour
    {
        private static readonly int UseGradientId = Shader.PropertyToID("_UseGradient");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

        private RuneVisualPalette palette;
        private SpriteRenderer stoneRenderer;
        private SpriteRenderer sigilRenderer;
        private MaterialPropertyBlock sigilProperties;

        public SpriteRenderer StoneRenderer => stoneRenderer;
        public SpriteRenderer SigilRenderer => sigilRenderer;

        public void Initialize(RuneVisualPalette visualPalette, int sortingOrder)
        {
            palette = visualPalette;
            stoneRenderer = CreateLayer("Stone", sortingOrder);
            sigilRenderer = CreateLayer("Sigil", sortingOrder + 1);
            stoneRenderer.sprite = palette != null ? palette.StoneBase : null;
            sigilProperties = new MaterialPropertyBlock();
            sigilRenderer.enabled = false;
        }

        public void Display(RuneData rune)
        {
            if (sigilRenderer == null)
            {
                return;
            }

            Sprite sigil = rune == null || palette == null
                ? null
                : palette.GetSigilSprite(rune.Sigil);
            sigilRenderer.sprite = sigil;
            sigilRenderer.enabled = sigil != null;
            sigilRenderer.transform.localScale = Vector3.one * (palette != null ? palette.SigilScale : 1f);
            sigilRenderer.sharedMaterial = palette != null ? palette.FireGradientMaterial : null;
            sigilRenderer.color = rune == null || palette == null
                ? Color.white
                : palette.GetSigilColor(rune.PrimaryElement);
            sigilProperties.SetFloat(UseGradientId,
                rune != null && rune.PrimaryElement == RuneElement.Fire ? 1f : 0f);
            sigilProperties.SetFloat(OutlineWidthId, palette != null ? palette.OutlineWidth : 0f);
            sigilProperties.SetColor(OutlineColorId, palette != null ? palette.OutlineColor : Color.clear);
            sigilRenderer.SetPropertyBlock(sigilProperties);
        }

        private SpriteRenderer CreateLayer(string layerName, int sortingOrder)
        {
            var layer = new GameObject(layerName);
            layer.transform.SetParent(transform, false);
            SpriteRenderer renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }
}
