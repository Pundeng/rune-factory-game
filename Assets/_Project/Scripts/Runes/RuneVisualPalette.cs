using System;
using UnityEngine;

namespace FantasyShapez.Runes
{
    [CreateAssetMenu(menuName = "Fantasy Shapez/Rune Visual Palette")]
    public sealed class RuneVisualPalette : ScriptableObject
    {
        [Serializable]
        private sealed class ElementColor
        {
            public RuneElement element = RuneElement.Fire;
            public Color color = Color.white;
        }

        [SerializeField] private Sprite stoneBase;
        [SerializeField] private Sprite spiritSigil;
        [SerializeField] private Sprite accelerationSigil;
        [SerializeField] private Material fireGradientMaterial;
        [SerializeField] private Color neutralSigilColor = Color.white;
        [SerializeField, Range(0.5f, 1.8f)] private float sigilScale = 1.7f;
        [SerializeField, Range(0f, 5f)] private float outlineWidth = 2f;
        [SerializeField] private Color outlineColor = new(0.12f, 0.07f, 0.2f, 1f);
        [SerializeField] private ElementColor[] elementColors = Array.Empty<ElementColor>();

        public Sprite StoneBase => stoneBase;

        public Material FireGradientMaterial => fireGradientMaterial;
        public float SigilScale => sigilScale > 0f ? Mathf.Min(sigilScale, 1.8f) : 1.7f;
        public float OutlineWidth => Mathf.Clamp(outlineWidth, 0f, 5f);
        public Color OutlineColor => outlineColor;

        public Sprite GetSigilSprite(RuneSigil sigil)
        {
            return sigil switch
            {
                RuneSigil.Spirit => spiritSigil,
                RuneSigil.Acceleration => accelerationSigil,
                _ => null
            };
        }

        public Color GetSigilColor(RuneElement? element)
        {
            if (element.HasValue && elementColors != null)
            {
                foreach (ElementColor entry in elementColors)
                {
                    if (entry != null && entry.element == element.Value)
                    {
                        return entry.color;
                    }
                }
            }

            return neutralSigilColor;
        }

    }
}
