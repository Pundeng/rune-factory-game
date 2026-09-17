using FantasyShapez.Buildings;
using FantasyShapez.Grid;
using FantasyShapez.Logistics;
using FantasyShapez.Production;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FantasyShapez.Editor
{
    public static class PrototypeBeltTransportSetup
    {
        private const string ScenePath = "Assets/_Project/Scenes/Prototype.unity";
        private const string ExtractorPrefabPath = "Assets/_Project/Prefabs/RuneExtractor.prefab";
        private const string BeltPrefabPath = "Assets/_Project/Prefabs/Belt.prefab";

        [MenuItem("Fantasy Shapez/Configure Prototype Belt Transport")]
        public static void Configure()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GridSystem gridSystem = Object.FindAnyObjectByType<GridSystem>();
            BuildingPlacementController placementController =
                Object.FindAnyObjectByType<BuildingPlacementController>();
            RuneExtractorPlacementBehavior extractorBehavior =
                Object.FindAnyObjectByType<RuneExtractorPlacementBehavior>();

            if (gridSystem == null || placementController == null || extractorBehavior == null)
            {
                throw new MissingReferenceException(
                    "The Prototype scene requires its grid, placement controller, and extractor behavior.");
            }

            BeltTransportCoordinator coordinator = CreateCoordinator(gridSystem);
            BeltPlacementBehavior beltBehavior = CreateBeltBehavior(placementController, coordinator);
            ConfigureExtractorBehavior(extractorBehavior, coordinator);
            GameObject beltPrefab = CreateBeltPrefab();
            GameObject extractorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExtractorPrefabPath);

            if (extractorPrefab == null)
            {
                throw new MissingReferenceException("The Rune Extractor prefab could not be loaded.");
            }

            ConfigurePlacementOptions(
                placementController,
                extractorPrefab,
                extractorBehavior,
                beltPrefab,
                beltBehavior);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static BeltTransportCoordinator CreateCoordinator(GridSystem gridSystem)
        {
            GameObject coordinatorObject = GameObject.Find("Belt Transport");
            if (coordinatorObject == null)
            {
                coordinatorObject = new GameObject("Belt Transport");
            }

            BeltTransportCoordinator coordinator =
                coordinatorObject.GetComponent<BeltTransportCoordinator>();
            if (coordinator == null)
            {
                coordinator = coordinatorObject.AddComponent<BeltTransportCoordinator>();
            }

            var serializedCoordinator = new SerializedObject(coordinator);
            serializedCoordinator.FindProperty("gridSystem").objectReferenceValue = gridSystem;
            serializedCoordinator.FindProperty("movementSpeed").floatValue = 1f;
            serializedCoordinator.ApplyModifiedPropertiesWithoutUndo();
            return coordinator;
        }

        private static BeltPlacementBehavior CreateBeltBehavior(
            BuildingPlacementController placementController,
            BeltTransportCoordinator coordinator)
        {
            BeltPlacementBehavior behavior = placementController.GetComponent<BeltPlacementBehavior>();
            if (behavior == null)
            {
                behavior = placementController.gameObject.AddComponent<BeltPlacementBehavior>();
            }

            var serializedBehavior = new SerializedObject(behavior);
            serializedBehavior.FindProperty("transportCoordinator").objectReferenceValue = coordinator;
            serializedBehavior.ApplyModifiedPropertiesWithoutUndo();
            return behavior;
        }

        private static void ConfigureExtractorBehavior(
            RuneExtractorPlacementBehavior behavior,
            BeltTransportCoordinator coordinator)
        {
            var serializedBehavior = new SerializedObject(behavior);
            serializedBehavior.FindProperty("transportCoordinator").objectReferenceValue = coordinator;
            serializedBehavior.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateBeltPrefab()
        {
            var prefabRoot = new GameObject("Belt");
            prefabRoot.AddComponent<PlacedBuilding>();
            prefabRoot.AddComponent<Belt>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, BeltPrefabPath);
            Object.DestroyImmediate(prefabRoot);
            return prefab;
        }

        private static void ConfigurePlacementOptions(
            BuildingPlacementController placementController,
            GameObject extractorPrefab,
            RuneExtractorPlacementBehavior extractorBehavior,
            GameObject beltPrefab,
            BeltPlacementBehavior beltBehavior)
        {
            var serializedController = new SerializedObject(placementController);
            SerializedProperty options = serializedController.FindProperty("buildingOptions");
            options.arraySize = 2;
            ConfigureOption(
                options.GetArrayElementAtIndex(0),
                "RuneExtractor",
                extractorPrefab,
                new Color(0.25f, 0.75f, 0.45f, 1f),
                extractorBehavior);
            ConfigureOption(
                options.GetArrayElementAtIndex(1),
                "Belt",
                beltPrefab,
                new Color(0.18f, 0.45f, 0.75f, 1f),
                beltBehavior);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureOption(
            SerializedProperty option,
            string id,
            GameObject instancePrefab,
            Color placedColor,
            MonoBehaviour placementBehavior)
        {
            SerializedProperty definition = option.FindPropertyRelative("definition");
            definition.FindPropertyRelative("id").stringValue = id;
            definition.FindPropertyRelative("footprint").vector2IntValue = Vector2Int.one;
            definition.FindPropertyRelative("instancePrefab").objectReferenceValue = instancePrefab;
            definition.FindPropertyRelative("visualPrefab").objectReferenceValue = null;
            definition.FindPropertyRelative("placedColor").colorValue = placedColor;
            option.FindPropertyRelative("placementBehavior").objectReferenceValue = placementBehavior;
        }
    }
}
