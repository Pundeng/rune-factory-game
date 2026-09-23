using FantasyShapez.Logistics;
using FantasyShapez.Runes;
using UnityEditor;
using UnityEngine;

namespace FantasyShapez.Editor
{
    public static class RuneVisualAssetSetup
    {
        private const string ImageFolder = "Assets/_Project/Images/";
        private const string MaterialPath = "Assets/_Project/Images/RuneSigilFire.mat";
        private const string PalettePath = "Assets/_Project/Images/RuneVisualPalette.asset";
        private const string BeltPath = "Assets/_Project/Prefabs/Belt.prefab";

        [InitializeOnLoadMethod]
        private static void ScheduleInitialSetup()
        {
            if (AssetDatabase.LoadAssetAtPath<RuneVisualPalette>(PalettePath) == null ||
                NeedsFullRect(ImageFolder + "rune-spirit-glyph-overlay.png") ||
                NeedsFullRect(ImageFolder + "rune-speed-glyph-overlay.png"))
            {
                EditorApplication.delayCall += SetUp;
            }
        }

        [MenuItem("Fantasy Shapez/Set Up Rune Visuals")]
        public static void SetUp()
        {
            ConfigureSprite(ImageFolder + "rune-stone-base.png");
            ConfigureSprite(ImageFolder + "rune-spirit-glyph-overlay.png");
            ConfigureSprite(ImageFolder + "rune-speed-glyph-overlay.png");

            string gradientPath = ImageFolder + "rune-flame-gradient.png";
            var gradientImporter = (TextureImporter)AssetImporter.GetAtPath(gradientPath);
            if (gradientImporter.textureType != TextureImporterType.Default ||
                gradientImporter.wrapMode != TextureWrapMode.Clamp)
            {
                gradientImporter.textureType = TextureImporterType.Default;
                gradientImporter.wrapMode = TextureWrapMode.Clamp;
                gradientImporter.SaveAndReimport();
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/_Project/Shaders/RuneSigilFire.shader");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, MaterialPath);
            }

            material.shader = shader;
            material.SetTexture("_GradientTex", AssetDatabase.LoadAssetAtPath<Texture2D>(gradientPath));
            EditorUtility.SetDirty(material);

            RuneVisualPalette palette = AssetDatabase.LoadAssetAtPath<RuneVisualPalette>(PalettePath);
            if (palette == null)
            {
                palette = ScriptableObject.CreateInstance<RuneVisualPalette>();
                AssetDatabase.CreateAsset(palette, PalettePath);
            }

            var paletteProperties = new SerializedObject(palette);
            paletteProperties.FindProperty("stoneBase").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(ImageFolder + "rune-stone-base.png");
            paletteProperties.FindProperty("spiritSigil").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(ImageFolder + "rune-spirit-glyph-overlay.png");
            paletteProperties.FindProperty("accelerationSigil").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Sprite>(ImageFolder + "rune-speed-glyph-overlay.png");
            paletteProperties.FindProperty("fireGradientMaterial").objectReferenceValue = material;
            paletteProperties.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.LoadPrefabContents(BeltPath);
            try
            {
                var beltProperties = new SerializedObject(prefab.GetComponent<Belt>());
                beltProperties.FindProperty("runeVisualPalette").objectReferenceValue = palette;
                beltProperties.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, BeltPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(prefab);
            }

            AssetDatabase.SaveAssets();
        }

        private static void ConfigureSprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (importer.textureType == TextureImporterType.Sprite &&
                importer.spritePixelsPerUnit == 512f &&
                importer.spriteImportMode == SpriteImportMode.Single &&
                settings.spriteMeshType == SpriteMeshType.FullRect)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512f;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static bool NeedsFullRect(string path)
        {
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                return false;
            }

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType != SpriteMeshType.FullRect;
        }
    }
}
