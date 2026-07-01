#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChoNoi.Application;
using ChoNoi.Infrastructure;
using ChoNoi.Presentation;
using ChoNoi.Presentation.Environment;
using ChoNoi.Presentation.Player;
using ChoNoi.Systems;
using ChoNoi.UI;
using ChoNoiMienTay.Data;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.Systems;
using ChoNoiMienTay.UI;

namespace ChoNoiMienTay.Editor
{
    public static class FinalSceneBuilder
    {
        private const string FinalScenePath = "Assets/_Project/Scenes/Core/ChoNoiMain.unity";
        private const string BargainingConfigPath = "Assets/_Project/ScriptableObjects/Bargaining/BargainingEconomyConfig.asset";
        private const string NewsDatabasePath = "Assets/_Project/ScriptableObjects/RiverMarket/MarketNewsDatabase.asset";
        private const string SourceItemsFolder = "Assets/_Project/ScriptableObjects/Bargaining/Items";
        private const string UpgradeCatalogPath = "Assets/_Project/ScriptableObjects/RiverMarket/BoatUpgradeCatalog.asset";
        private const string BoatStatsPath = "Assets/_Project/ScriptableObjects/RiverMarket/RiverBoatStats.asset";
        private const string RuntimeBuildRootName = "__ChoNoiRuntimeBuilder";

        [MenuItem("ChoNoi/Scenes/Build Final Scene")]
        public static void BuildFinalScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[FinalSceneBuilder] Stop Play Mode before preparing the final scene.");
                return;
            }

            SceneAsset finalSceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(FinalScenePath);
            if (finalSceneAsset == null)
            {
                Debug.LogWarning($"[FinalSceneBuilder] Final scene not found at '{FinalScenePath}'. Create or restore ChoNoiMain before building.");
                return;
            }

            Scene targetScene = EditorSceneManager.OpenScene(FinalScenePath, OpenSceneMode.Single);

            EnsureCoreGameplayObjects(targetScene);
            RemoveEmptyRuntimeRoots(targetScene);

            EnsureCoreReferences(targetScene);
            RemoveKnownIntroObjects(targetScene);

            EditorSceneManager.SaveScene(targetScene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(FinalScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(FinalScenePath, OpenSceneMode.Single);

            Debug.Log("[FinalSceneBuilder] Refreshed ChoNoiMain runtime objects and scene references without loading RiverMarketScene.");
        }

        [MenuItem("ChoNoi/Scenes/Build Final Scene", true)]
        private static bool ValidateBuildFinalScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void EnsureCoreReferences(Scene scene)
        {
            GameObject systemsRoot = FindSceneObject(scene, "GameSystems");
            GameObject masterCanvas = FindSceneObject(scene, "MasterCanvas");
            GameObject playerBoat = FindSceneObject(scene, "PlayerBoat");
            GameObject playerOnFoot = FindSceneObject(scene, "PlayerOnFoot");
            GameObject waterPlane = FindSceneObject(scene, "WaterPlane");

            if (systemsRoot == null || playerBoat == null || playerOnFoot == null)
            {
                Debug.LogWarning("[FinalSceneBuilder] Missing one of the required core objects: GameSystems, PlayerBoat, or PlayerOnFoot.");
                return;
            }

            TimeManager timeManager = systemsRoot.GetComponent<TimeManager>();
            PlayerStats playerStats = systemsRoot.GetComponent<PlayerStats>();
            InventoryManager inventoryManager = systemsRoot.GetComponent<InventoryManager>();
            EconomyManager economyManager = systemsRoot.GetComponent<EconomyManager>();
            DurabilityManager durabilityManager = systemsRoot.GetComponent<DurabilityManager>();
            MarketNewsController marketNewsController = systemsRoot.GetComponent<MarketNewsController>();
            SaveLoadManager saveLoadManager = systemsRoot.GetComponent<SaveLoadManager>();
            RiverMarketHUD hud = systemsRoot.GetComponent<RiverMarketHUD>();
            BargainingSystem bargainingSystem = systemsRoot.GetComponent<BargainingSystem>();
            BargainingPrototypeUI bargainingPrototypeUi = systemsRoot.GetComponent<BargainingPrototypeUI>();
            FullSimulatorUI fullSimulatorUi = systemsRoot.GetComponent<FullSimulatorUI>();
            CustomerSpawnManager customerSpawnManager = systemsRoot.GetComponent<CustomerSpawnManager>();
            DayFlowController dayFlowController = systemsRoot.GetComponent<DayFlowController>();
            EnvironmentController environmentController = systemsRoot.GetComponent<EnvironmentController>();

            BoatController boatController = playerBoat.GetComponent<BoatController>();
            PCBoatInput boatInput = playerBoat.GetComponent<PCBoatInput>();
            BoatCampManager boatCampManager = playerBoat.GetComponent<BoatCampManager>();
            BambooPoleManager bambooPoleManager = playerBoat.GetComponent<BambooPoleManager>();

            ShorePlayerController shorePlayerController = playerOnFoot.GetComponent<ShorePlayerController>();
            BoatBoardingController boardingController = playerOnFoot.GetComponent<BoatBoardingController>();
            Transform playerVisualRoot = playerOnFoot.transform.Find("PlayerVisualRoot");
            Transform standPoint = playerBoat.transform.Find("PlayerStandPoint");
            Transform dismountPoint = FindSceneObject(scene, "DismountPoint")?.transform;

            if (economyManager != null)
            {
                economyManager.playerStats = playerStats;
                economyManager.durabilityManager = durabilityManager;
            }

            if (boatCampManager != null)
            {
                SetPrivateField(boatCampManager, "inventoryManager", inventoryManager);
                SetPrivateField(boatCampManager, "playerStats", playerStats);
                SetPrivateField(boatCampManager, "boatController", boatController);
            }

            if (bambooPoleManager != null)
            {
                SetPrivateField(bambooPoleManager, "boatCampManager", boatCampManager);
                SetPrivateField(bambooPoleManager, "inventoryManager", inventoryManager);
            }

            if (marketNewsController != null)
            {
                MarketNewsDatabaseSO newsDatabase = AssetDatabase.LoadAssetAtPath<MarketNewsDatabaseSO>(NewsDatabasePath);
                marketNewsController.Configure(timeManager, newsDatabase);
            }

            if (saveLoadManager != null)
            {
                SetPrivateField(saveLoadManager, "playerStats", playerStats);
                SetPrivateField(saveLoadManager, "inventoryManager", inventoryManager);
                SetPrivateField(saveLoadManager, "boatCampManager", boatCampManager);
                SetPrivateField(saveLoadManager, "bambooPoleManager", bambooPoleManager);
                SetPrivateField(saveLoadManager, "durabilityManager", durabilityManager);
                SetPrivateField(saveLoadManager, "timeManager", timeManager);
                SetPrivateField(saveLoadManager, "masterItemDatabase", LoadMarketItems());

                SerializedObject serializedSaveLoad = new SerializedObject(saveLoadManager);
                serializedSaveLoad.FindProperty("loadSavedGameOnStart").boolValue = false;
                serializedSaveLoad.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(saveLoadManager);
            }

            if (bargainingSystem != null)
            {
                BargainingEconomyConfig bargainingConfig = AssetDatabase.LoadAssetAtPath<BargainingEconomyConfig>(BargainingConfigPath);
                bargainingSystem.Configure(bargainingConfig, playerStats, inventoryManager, economyManager);
            }

            if (bargainingPrototypeUi != null)
            {
                bargainingPrototypeUi.Configure(bargainingSystem, playerStats, inventoryManager);
            }

            if (hud != null)
            {
                hud.Configure(
                    timeManager,
                    playerStats,
                    inventoryManager,
                    boatCampManager,
                    marketNewsController,
                    economyManager,
                    durabilityManager,
                    LoadMarketItems());
            }

            if (environmentController != null)
            {
                SetPrivateField(environmentController, "timeManager", timeManager);
                SetPrivateField(environmentController, "directionalLight", Object.FindAnyObjectByType<Light>());
                if (waterPlane != null)
                {
                    SetPrivateField(environmentController, "waterTransform", waterPlane.transform);
                }
            }

            if (dayFlowController != null)
            {
                SetPrivateField(dayFlowController, "timeManager", timeManager);
                SetPrivateField(dayFlowController, "playerStats", playerStats);
                SetPrivateField(dayFlowController, "durabilityManager", durabilityManager);
                SetPrivateField(dayFlowController, "saveLoadManager", saveLoadManager);
            }

            if (customerSpawnManager != null)
            {
                SetPrivateField(customerSpawnManager, "timeManager", timeManager);
                SetPrivateField(customerSpawnManager, "bambooPoleManager", bambooPoleManager);
            }

            if (fullSimulatorUi != null)
            {
                fullSimulatorUi.inventoryManager = inventoryManager;
                fullSimulatorUi.riverMarketHUD = hud;
                fullSimulatorUi.playerStats = playerStats;
                fullSimulatorUi.boatCampManager = boatCampManager;
                fullSimulatorUi.bambooPoleManager = bambooPoleManager;
                fullSimulatorUi.masterCanvasInstance = masterCanvas;
                SetPrivateField(fullSimulatorUi, "skipSplashAndHomeOnPlay", true);
                SetPrivateField(fullSimulatorUi, "autoContinueOnPlay", false);

                if (masterCanvas != null)
                {
                    fullSimulatorUi.hudCtrl = masterCanvas.GetComponentInChildren<HUDController>(true);
                    fullSimulatorUi.invUI = masterCanvas.GetComponentInChildren<ChoNoiMienTay.UI.InventoryUI>(true);
                    fullSimulatorUi.diaUI = masterCanvas.GetComponentInChildren<DialogueUI>(true);
                    fullSimulatorUi.trUI = masterCanvas.GetComponentInChildren<TradeUI>(true);
                    fullSimulatorUi.daySumUI = masterCanvas.GetComponentInChildren<DaySummaryUI>(true);
                    fullSimulatorUi.settingsUI = masterCanvas.GetComponentInChildren<SettingsUI>(true);
                    fullSimulatorUi.confUI = masterCanvas.GetComponentInChildren<ConfirmationPopup>(true);
                    fullSimulatorUi.loadUI = masterCanvas.GetComponentInChildren<LoadingUI>(true);
                    fullSimulatorUi.transUI = masterCanvas.GetComponentInChildren<TransitionUI>(true);
                    fullSimulatorUi.goUI = masterCanvas.GetComponentInChildren<GameOverUI>(true);
                }

                SerializedObject serializedUi = new SerializedObject(fullSimulatorUi);
                serializedUi.FindProperty("skipSplashAndHomeOnPlay").boolValue = true;
                serializedUi.FindProperty("autoContinueOnPlay").boolValue = false;
                serializedUi.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(fullSimulatorUi);
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                BoatFollowCamera followCamera = mainCamera.GetComponent<BoatFollowCamera>();
                if (followCamera == null)
                {
                    followCamera = mainCamera.gameObject.AddComponent<BoatFollowCamera>();
                }

                followCamera.Configure(playerOnFoot.transform);

                if (boardingController != null)
                {
                    boardingController.Configure(
                        shorePlayerController,
                        playerVisualRoot,
                        playerBoat.transform,
                        standPoint,
                        dismountPoint,
                        boatController,
                        boatInput,
                        followCamera);
                }
            }

            ApplyChoNoiMainLandMode(scene, playerBoat, boardingController, boatController, boatInput, dismountPoint);

            EditorUtility.SetDirty(systemsRoot);
            EditorUtility.SetDirty(playerBoat);
            EditorUtility.SetDirty(playerOnFoot);
            if (masterCanvas != null)
            {
                EditorUtility.SetDirty(masterCanvas);
            }
        }

        private static void ApplyChoNoiMainLandMode(
            Scene scene,
            GameObject playerBoat,
            BoatBoardingController boardingController,
            BoatController boatController,
            PCBoatInput boatInput,
            Transform dismountPoint)
        {
            if (scene.name != "ChoNoiMain" || playerBoat == null)
                return;

            Transform boatVisualRoot = playerBoat.transform.Find("BoatVisualRoot");
            if (boatVisualRoot != null)
            {
                boatVisualRoot.gameObject.SetActive(false);
            }

            foreach (Collider collider in playerBoat.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = false;
            }

            Rigidbody boatRigidbody = playerBoat.GetComponent<Rigidbody>();
            if (boatRigidbody != null)
            {
                boatRigidbody.linearVelocity = Vector3.zero;
                boatRigidbody.angularVelocity = Vector3.zero;
                boatRigidbody.isKinematic = true;
                boatRigidbody.detectCollisions = false;
            }

            if (boatController != null)
            {
                boatController.enabled = false;
            }

            if (boatInput != null)
            {
                boatInput.enabled = false;
            }

            if (boardingController != null)
            {
                boardingController.enabled = false;
            }

            if (dismountPoint != null)
            {
                dismountPoint.gameObject.SetActive(false);
            }
        }

        private static void EnsureCoreGameplayObjects(Scene scene)
        {
            GameObject systemsRoot = FindSceneObject(scene, "GameSystems");
            if (systemsRoot == null)
            {
                Debug.LogWarning("[FinalSceneBuilder] GameSystems is missing, cannot rebuild player gameplay objects.");
                return;
            }

            NormalizeRuntimeHierarchy(scene);

            GameObject waterPlane = FindSceneObject(scene, "WaterPlane");
            BoatStats boatStats = AssetDatabase.LoadAssetAtPath<BoatStats>(BoatStatsPath);
            BoatUpgradeCatalogSO upgradeCatalog = AssetDatabase.LoadAssetAtPath<BoatUpgradeCatalogSO>(UpgradeCatalogPath);
            PlayerStats playerStats = systemsRoot.GetComponent<PlayerStats>();
            InventoryManager inventoryManager = systemsRoot.GetComponent<InventoryManager>();
            GameObject runtimeBuildRoot = new GameObject(RuntimeBuildRootName);
            SceneManager.MoveGameObjectToScene(runtimeBuildRoot, scene);

            if (FindSceneObject(scene, "PlayerBoat") == null && waterPlane != null && boatStats != null)
            {
                GameObject boat = RiverMarketSceneBuilder.BuildBoat(runtimeBuildRoot.transform, boatStats, waterPlane.transform);
                BoatCampManager boatCampManager = boat.GetComponent<BoatCampManager>();
                if (boatCampManager == null)
                {
                    boatCampManager = boat.AddComponent<BoatCampManager>();
                }

                BambooPoleManager bambooPoleManager = boat.GetComponent<BambooPoleManager>();
                if (bambooPoleManager == null)
                {
                    bambooPoleManager = boat.AddComponent<BambooPoleManager>();
                }

                SetPrivateField(boatCampManager, "upgradeCatalog", upgradeCatalog);
                SetPrivateField(boatCampManager, "inventoryManager", inventoryManager);
                SetPrivateField(boatCampManager, "playerStats", playerStats);
                SetPrivateField(boatCampManager, "boatController", boat.GetComponent<BoatController>());
                SetPrivateField(bambooPoleManager, "boatCampManager", boatCampManager);
                SetPrivateField(bambooPoleManager, "inventoryManager", inventoryManager);
                RiverMarketSceneBuilder.SetupBoatVisualModules(boatCampManager, boat.transform);
                boat.transform.SetParent(null, true);
                EditorUtility.SetDirty(boat);
            }

            GameObject playerBoat = FindSceneObject(scene, "PlayerBoat");
            if (FindSceneObject(scene, "PlayerOnFoot") == null && playerBoat != null)
            {
                GameObject player = RiverMarketSceneBuilder.BuildShorePlayer(runtimeBuildRoot.transform, playerBoat);
                RiverMarketSceneBuilder.SetupBoardingFlow(player, playerBoat, Camera.main != null ? Camera.main.GetComponent<BoatFollowCamera>() : null);
                player.transform.SetParent(null, true);
                GameObject dismountPoint = FindSceneObject(scene, "DismountPoint");
                if (dismountPoint != null && dismountPoint.transform.parent == runtimeBuildRoot.transform)
                {
                    dismountPoint.transform.SetParent(null, true);
                }
                EditorUtility.SetDirty(player);
            }

            Object.DestroyImmediate(runtimeBuildRoot);
        }

        private static void NormalizeRuntimeHierarchy(Scene scene)
        {
            GameObject worldRoot = FindSceneObject(scene, "RiverMarketWorld");
            if (worldRoot == null)
            {
                return;
            }

            string[] runtimeObjectNames =
            {
                "PlayerBoat",
                "PlayerOnFoot",
                "DismountPoint"
            };

            foreach (string objectName in runtimeObjectNames)
            {
                GameObject runtimeObject = FindSceneObject(scene, objectName);
                if (runtimeObject != null && runtimeObject.transform.parent == worldRoot.transform)
                {
                    runtimeObject.transform.SetParent(null, true);
                }
            }

            if (worldRoot.transform.childCount == 0)
            {
                Object.DestroyImmediate(worldRoot);
            }
        }

        private static void RemoveEmptyRuntimeRoots(Scene scene)
        {
            string[] rootNames =
            {
                "RiverMarketWorld",
                RuntimeBuildRootName
            };

            foreach (string rootName in rootNames)
            {
                GameObject root = FindSceneObject(scene, rootName);
                if (root != null && root.transform.parent == null && root.transform.childCount == 0)
                {
                    Object.DestroyImmediate(root);
                }
            }
        }

        private static void RemoveKnownIntroObjects(Scene scene)
        {
            string[] introObjectNames =
            {
                "ForestShot",
                "LakeShot",
                "GladeShot",
                "GulleyShot",
                "Landscape",
                "Fader",
                "CuuHo",
                "VirtualCameras",
                "Timelines",
                "UI",
                "Canvas",
                "IntroCanvas",
                "OpeningCanvas"
            };

            foreach (string objectName in introObjectNames)
            {
                GameObject found = FindSceneObject(scene, objectName);
                if (found != null)
                {
                    Object.DestroyImmediate(found);
                }
            }
        }

        private static List<ItemData> LoadMarketItems()
        {
            List<ItemData> items = new List<ItemData>();
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { SourceItemsFolder });
            foreach (string guid in guids)
            {
                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(AssetDatabase.GUIDToAssetPath(guid));
                if (item != null)
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static GameObject FindSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }

                Transform nested = FindDeepChild(rootObject.transform, objectName);
                if (nested != null)
                {
                    return nested.gameObject;
                }
            }

            return null;
        }

        private static GameObject FindRootSceneObject(Scene scene, string objectName)
        {
            foreach (GameObject rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.name == objectName)
                {
                    return rootObject;
                }
            }

            return null;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                Transform result = FindDeepChild(child, childName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null)
            {
                return;
            }

            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
#endif
