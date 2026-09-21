using FantasyShapez.Buildings;
using FantasyShapez.Grid;
using FantasyShapez.Logistics;
using FantasyShapez.Objectives;
using FantasyShapez.Production;
using FantasyShapez.Runes;
using FantasyShapez.UI;
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
        private const string EngraverPrefabPath = "Assets/_Project/Prefabs/Engraver.prefab";
        private const string GlyphRotatorPrefabPath = "Assets/_Project/Prefabs/GlyphRotator.prefab";
        private const string ElementInfuserPrefabPath = "Assets/_Project/Prefabs/ElementInfuser.prefab";
        private const string ScriptableObjectsFolderPath = "Assets/_Project/ScriptableObjects";
        private const string ObjectiveFolderPath = ScriptableObjectsFolderPath + "/Objectives";

        [MenuItem("Fantasy Shapez/Configure Prototype Production")]
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
            EngraverPlacementBehavior engraverBehavior =
                CreateEngraverBehavior(placementController, coordinator);
            ElementInfuserPlacementBehavior infuserBehavior =
                CreateElementInfuserBehavior(placementController, coordinator);
            ConfigureExtractorBehavior(extractorBehavior, coordinator);
            GameObject beltPrefab = CreateBeltPrefab();
            GameObject engraverPrefab = CreateEngraverPrefab();
            GameObject infuserPrefab = CreateElementInfuserPrefab();
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
                beltBehavior,
                engraverPrefab,
                engraverBehavior,
                infuserPrefab,
                infuserBehavior);
            ObjectivePanel objectivePanel =
                CreateHub(gridSystem, coordinator, CreateObjectiveAssets());
            var serializedPlacementController = new SerializedObject(placementController);
            serializedPlacementController.FindProperty("engraverUpgradePanel")
                .objectReferenceValue = objectivePanel;
            serializedPlacementController.ApplyModifiedPropertiesWithoutUndo();

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

        private static EngraverPlacementBehavior CreateEngraverBehavior(
            BuildingPlacementController placementController,
            BeltTransportCoordinator coordinator)
        {
            EngraverPlacementBehavior behavior =
                placementController.GetComponent<EngraverPlacementBehavior>();
            if (behavior == null)
            {
                behavior = placementController.gameObject.AddComponent<EngraverPlacementBehavior>();
            }

            var serializedBehavior = new SerializedObject(behavior);
            serializedBehavior.FindProperty("transportCoordinator").objectReferenceValue = coordinator;
            serializedBehavior.ApplyModifiedPropertiesWithoutUndo();
            return behavior;
        }

        private static GlyphRotatorPlacementBehavior CreateGlyphRotatorBehavior(
            BuildingPlacementController placementController,
            BeltTransportCoordinator coordinator)
        {
            GlyphRotatorPlacementBehavior behavior =
                placementController.GetComponent<GlyphRotatorPlacementBehavior>();
            if (behavior == null)
            {
                behavior = placementController.gameObject.AddComponent<GlyphRotatorPlacementBehavior>();
            }

            var serializedBehavior = new SerializedObject(behavior);
            serializedBehavior.FindProperty("transportCoordinator").objectReferenceValue = coordinator;
            serializedBehavior.ApplyModifiedPropertiesWithoutUndo();
            return behavior;
        }

        private static ElementInfuserPlacementBehavior CreateElementInfuserBehavior(
            BuildingPlacementController placementController,
            BeltTransportCoordinator coordinator)
        {
            ElementInfuserPlacementBehavior behavior =
                placementController.GetComponent<ElementInfuserPlacementBehavior>();
            if (behavior == null)
            {
                behavior = placementController.gameObject.AddComponent<ElementInfuserPlacementBehavior>();
            }

            var serializedBehavior = new SerializedObject(behavior);
            serializedBehavior.FindProperty("transportCoordinator").objectReferenceValue = coordinator;
            serializedBehavior.ApplyModifiedPropertiesWithoutUndo();
            return behavior;
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

        private static GameObject CreateEngraverPrefab()
        {
            var prefabRoot = new GameObject("Engraver");
            prefabRoot.AddComponent<PlacedBuilding>();
            Engraver engraver = prefabRoot.AddComponent<Engraver>();
            var serializedEngraver = new SerializedObject(engraver);
            serializedEngraver.FindProperty("selectedSigil").enumValueIndex =
                (int)RuneSigil.Spirit;
            serializedEngraver.FindProperty("selectedGlyph").enumValueIndex = 0;
            serializedEngraver.FindProperty("processingDuration").floatValue = 1.5f;
            serializedEngraver.FindProperty("upgradedSpeedMultiplier").floatValue = 2f;
            serializedEngraver.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, EngraverPrefabPath);
            Object.DestroyImmediate(prefabRoot);
            return prefab;
        }

        private static GameObject CreateGlyphRotatorPrefab()
        {
            var prefabRoot = new GameObject("Glyph Rotator");
            prefabRoot.AddComponent<PlacedBuilding>();
            GlyphRotator rotator = prefabRoot.AddComponent<GlyphRotator>();
            var serializedRotator = new SerializedObject(rotator);
            serializedRotator.FindProperty("selectedGlyph").enumValueIndex = 1;
            serializedRotator.FindProperty("processingDuration").floatValue = 1.5f;
            serializedRotator.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, GlyphRotatorPrefabPath);
            Object.DestroyImmediate(prefabRoot);
            return prefab;
        }

        private static GameObject CreateElementInfuserPrefab()
        {
            var prefabRoot = new GameObject("Element Infuser");
            prefabRoot.AddComponent<PlacedBuilding>();
            ElementInfuser infuser = prefabRoot.AddComponent<ElementInfuser>();
            var serializedInfuser = new SerializedObject(infuser);
            serializedInfuser.FindProperty("useWholeRuneElement").boolValue = true;
            serializedInfuser.FindProperty("primaryElement").enumValueIndex = 0;
            serializedInfuser.FindProperty("targetZone").enumValueIndex = 0;
            serializedInfuser.FindProperty("processingDuration").floatValue = 1.5f;
            serializedInfuser.ApplyModifiedPropertiesWithoutUndo();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(prefabRoot, ElementInfuserPrefabPath);
            Object.DestroyImmediate(prefabRoot);
            return prefab;
        }

        private static void ConfigurePlacementOptions(
            BuildingPlacementController placementController,
            GameObject extractorPrefab,
            RuneExtractorPlacementBehavior extractorBehavior,
            GameObject beltPrefab,
            BeltPlacementBehavior beltBehavior,
            GameObject engraverPrefab,
            EngraverPlacementBehavior engraverBehavior,
            GameObject infuserPrefab,
            ElementInfuserPlacementBehavior infuserBehavior)
        {
            var serializedController = new SerializedObject(placementController);
            SerializedProperty options = serializedController.FindProperty("buildingOptions");
            options.arraySize = 4;
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
            ConfigureOption(
                options.GetArrayElementAtIndex(2),
                "Engraver",
                engraverPrefab,
                new Color(0.55f, 0.3f, 0.75f, 1f),
                engraverBehavior);
            ConfigureOption(
                options.GetArrayElementAtIndex(3),
                "ElementInfuser",
                infuserPrefab,
                new Color(0.75f, 0.35f, 0.25f, 1f),
                infuserBehavior);
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

        private static ObjectivePanel CreateHub(
            GridSystem gridSystem,
            BeltTransportCoordinator coordinator,
            ObjectiveDefinitionAsset[] objectives)
        {
            GameObject hubObject = GameObject.Find("Hub");
            if (hubObject == null)
            {
                hubObject = new GameObject("Hub");
            }

            Hub hub = hubObject.GetComponent<Hub>();
            if (hub == null)
            {
                hub = hubObject.AddComponent<Hub>();
            }

            ObjectivePanel panel = hubObject.GetComponent<ObjectivePanel>();
            if (panel == null)
            {
                panel = hubObject.AddComponent<ObjectivePanel>();
            }

            Vector2Int hubCell = new(10, 1);
            hub.Configure(
                coordinator,
                hubCell,
                GridDirection.East,
                objectives);
            var serializedHub = new SerializedObject(hub);
            SerializedProperty additionalDirections =
                serializedHub.FindProperty("additionalIncomingDirections");
            additionalDirections.arraySize = 3;
            additionalDirections.GetArrayElementAtIndex(0).enumValueIndex =
                (int)GridDirection.North;
            additionalDirections.GetArrayElementAtIndex(1).enumValueIndex =
                (int)GridDirection.South;
            additionalDirections.GetArrayElementAtIndex(2).enumValueIndex =
                (int)GridDirection.West;
            serializedHub.ApplyModifiedPropertiesWithoutUndo();
            panel.Configure(hub);
            hubObject.transform.position = gridSystem.GridToWorld(hubCell);
            EditorUtility.SetDirty(hub);
            EditorUtility.SetDirty(panel);
            return panel;
        }

        private static ObjectiveDefinitionAsset[] CreateObjectiveAssets()
        {
            EnsureObjectiveFolderExists();
            return new[]
            {
                LoadOrCreateObjective(
                    "EmptyCircle",
                    "Empty Circle",
                    new RuneData(RuneBaseShape.Circle),
                    20),
                LoadOrCreateObjective(
                    "SpiritRune",
                    "Spirit Rune",
                    new RuneData(RuneBaseShape.Circle, RuneSigil.Spirit),
                    20),
                LoadOrCreateObjective(
                    "FireSpiritRune",
                    "Fire Spirit Rune",
                    new RuneData(
                        RuneBaseShape.Circle,
                        RuneSigil.Spirit,
                        RuneElement.Fire),
                    20),
                LoadOrCreateObjective(
                    "AccelerationRune",
                    "Acceleration Rune",
                    new RuneData(RuneBaseShape.Circle, RuneSigil.Acceleration),
                    20),
                LoadOrCreateObjective(
                    "SharedThroughput",
                    "Shared Throughput",
                    new ObjectiveRequirement(
                        new RuneData(
                            RuneBaseShape.Circle,
                            RuneSigil.Spirit,
                            RuneElement.Fire),
                        20),
                    new ObjectiveRequirement(
                        new RuneData(RuneBaseShape.Circle, RuneSigil.Acceleration),
                        20)),
                LoadOrCreateSustainedObjective(
                    "SustainedAcceleration",
                    "Sustained Acceleration",
                    new RuneData(RuneBaseShape.Circle, RuneSigil.Acceleration),
                    0.5f,
                    4f,
                    12f)
            };
        }

        private static void EnsureObjectiveFolderExists()
        {
            if (!AssetDatabase.IsValidFolder(ScriptableObjectsFolderPath))
            {
                AssetDatabase.CreateFolder("Assets/_Project", "ScriptableObjects");
            }

            if (!AssetDatabase.IsValidFolder(ObjectiveFolderPath))
            {
                AssetDatabase.CreateFolder(ScriptableObjectsFolderPath, "Objectives");
            }
        }

        private static ObjectiveDefinitionAsset LoadOrCreateObjective(
            string assetName,
            string displayName,
            RuneData targetRune,
            int requiredCount)
        {
            string path = $"{ObjectiveFolderPath}/{assetName}.asset";
            ObjectiveDefinitionAsset objective =
                AssetDatabase.LoadAssetAtPath<ObjectiveDefinitionAsset>(path);
            if (objective != null)
            {
                return objective;
            }

            objective = ScriptableObject.CreateInstance<ObjectiveDefinitionAsset>();
            objective.Configure(displayName, targetRune, requiredCount);
            AssetDatabase.CreateAsset(objective, path);
            return objective;
        }

        private static ObjectiveDefinitionAsset LoadOrCreateObjective(
            string assetName,
            string displayName,
            params ObjectiveRequirement[] requirements)
        {
            string path = $"{ObjectiveFolderPath}/{assetName}.asset";
            ObjectiveDefinitionAsset objective =
                AssetDatabase.LoadAssetAtPath<ObjectiveDefinitionAsset>(path);
            if (objective != null)
            {
                return objective;
            }

            objective = ScriptableObject.CreateInstance<ObjectiveDefinitionAsset>();
            objective.Configure(displayName, requirements);
            AssetDatabase.CreateAsset(objective, path);
            return objective;
        }

        private static ObjectiveDefinitionAsset LoadOrCreateSustainedObjective(
            string assetName,
            string displayName,
            RuneData targetRune,
            float targetRatePerSecond,
            float measurementWindowSeconds,
            float sustainDurationSeconds)
        {
            string path = $"{ObjectiveFolderPath}/{assetName}.asset";
            ObjectiveDefinitionAsset objective =
                AssetDatabase.LoadAssetAtPath<ObjectiveDefinitionAsset>(path);
            if (objective != null)
            {
                return objective;
            }

            objective = ScriptableObject.CreateInstance<ObjectiveDefinitionAsset>();
            objective.ConfigureSustainedRate(
                displayName,
                targetRune,
                targetRatePerSecond,
                measurementWindowSeconds,
                sustainDurationSeconds);
            AssetDatabase.CreateAsset(objective, path);
            return objective;
        }

    }
}
