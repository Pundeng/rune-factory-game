using FantasyShapez.Runes;
using FantasyShapez.Logistics;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace FantasyShapez.Tests.EditMode
{
    public sealed class RuneVisualTests
    {
        private const string PalettePath = "Assets/_Project/Images/RuneVisualPalette.asset";

        [Test]
        public void Palette_UsesPreparedArtworkAndFireGradient()
        {
            RuneVisualPalette palette = AssetDatabase.LoadAssetAtPath<RuneVisualPalette>(PalettePath);
            Assert.That(palette, Is.Not.Null);
            Assert.That(palette.StoneBase, Is.Not.Null);
            Assert.That(palette.GetSigilSprite(RuneSigil.None), Is.Null);
            Assert.That(palette.GetSigilSprite(RuneSigil.Spirit), Is.Not.Null);
            Assert.That(palette.GetSigilSprite(RuneSigil.Acceleration), Is.Not.Null);
            Assert.That(palette.FireGradientMaterial, Is.Not.Null);
            Assert.That(palette.FireGradientMaterial.GetTexture("_GradientTex"), Is.Not.Null);
            Assert.That(palette.SigilScale, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(palette.OutlineWidth, Is.GreaterThan(0f));

            GameObject beltPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Belt.prefab");
            var beltProperties = new SerializedObject(beltPrefab.GetComponent<Belt>());
            Assert.That(beltProperties.FindProperty("runeVisualPalette").objectReferenceValue,
                Is.SameAs(palette));
        }

        [Test]
        public void Display_FollowsRuneIdentityAndKeepsStoneNeutral()
        {
            RuneVisualPalette palette = AssetDatabase.LoadAssetAtPath<RuneVisualPalette>(PalettePath);
            var gameObject = new GameObject("Rune Visual Test");
            try
            {
                RuneVisual visual = gameObject.AddComponent<RuneVisual>();
                visual.Initialize(palette, 25);
                Sprite stone = visual.StoneRenderer.sprite;

                RuneData empty = new RuneData(RuneBaseShape.Circle);
                visual.Display(empty);
                Assert.That(visual.SigilRenderer.enabled, Is.False);
                Assert.That(visual.StoneRenderer.sprite, Is.SameAs(stone));

                Assert.That(RuneOperations.TryEngraveSigil(empty, RuneSigil.Spirit, out RuneData spirit), Is.True);
                visual.Display(spirit);
                Assert.That(visual.SigilRenderer.sprite, Is.SameAs(palette.GetSigilSprite(RuneSigil.Spirit)));
                Assert.That(visual.SigilRenderer.sharedMaterial, Is.SameAs(palette.FireGradientMaterial));
                Assert.That(visual.SigilRenderer.transform.localScale.x, Is.EqualTo(palette.SigilScale));
                var properties = new MaterialPropertyBlock();
                visual.SigilRenderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_UseGradient"), Is.Zero);
                Assert.That(properties.GetFloat("_OutlineWidth"), Is.EqualTo(palette.OutlineWidth));

                Assert.That(RuneOperations.TryAssignPrimaryElement(spirit, RuneElement.Fire, out RuneData fireSpirit), Is.True);
                visual.Display(fireSpirit);
                Assert.That(visual.SigilRenderer.sharedMaterial, Is.SameAs(palette.FireGradientMaterial));
                visual.SigilRenderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_UseGradient"), Is.EqualTo(1f));
                Assert.That(visual.StoneRenderer.sprite, Is.SameAs(stone));
                Assert.That(visual.StoneRenderer.color, Is.EqualTo(Color.white));
                Assert.That(visual.StoneRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));
                Assert.That(visual.SigilRenderer.transform.localPosition, Is.EqualTo(Vector3.zero));

                visual.Display(new RuneData(RuneBaseShape.Circle, RuneSigil.Acceleration));
                Assert.That(visual.SigilRenderer.sprite, Is.SameAs(palette.GetSigilSprite(RuneSigil.Acceleration)));
                Assert.That(visual.SigilRenderer.sharedMaterial, Is.SameAs(palette.FireGradientMaterial));
                visual.SigilRenderer.GetPropertyBlock(properties);
                Assert.That(properties.GetFloat("_UseGradient"), Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
