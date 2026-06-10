#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using ChoNoi.Application;
using ChoNoi.Infrastructure;
using ChoNoi.Presentation;
using ChoNoi.Presentation.Environment;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.UI;

namespace ChoNoiMienTay.Editor
{
    public static class RiverMarketSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Core/RiverMarketScene.unity";
        private const string UpgradeCatalogPath = "Assets/_Project/ScriptableObjects/RiverMarket/BoatUpgradeCatalog.asset";
        private const string NewsDatabasePath = "Assets/_Project/ScriptableObjects/RiverMarket/MarketNewsDatabase.asset";
        private const string EnvironmentProfilePath = "Assets/_Project/ScriptableObjects/RiverMarket/EnvironmentProfile.asset";
        private const string BoatStatsPath = "Assets/_Project/ScriptableObjects/RiverMarket/RiverBoatStats.asset";
        private const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";
        private const string MerchantAvatarPath = "Assets/_Project/Art/Avatars/merchant.png";
        private const string VillagerAvatarPath = "Assets/_Project/Art/Avatars/villager.png";
        private const string BoatModelPath = "Assets/_Project/Art/Models/thuyencoban.fbx";
        private const string BargainingItemsFolder = "Assets/_Project/ScriptableObjects/Bargaining/Items";

        [MenuItem("ChoNoi/Scenes/Build River Market Scene")]
        public static void BuildScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("[RiverMarketSceneBuilder] Stop Play Mode before rebuilding the River Market scene.");
                return;
            }

            EnsureFolders();

            BoatUpgradeCatalogSO upgradeCatalog = EnsureUpgradeCatalog();
            MarketNewsDatabaseSO newsDatabase = EnsureNewsDatabase();
            EnvironmentProfileSO environmentProfile = EnsureEnvironmentProfile();
            BoatStats boatStats = EnsureBoatStats();
            List<ItemData> marketItems = LoadMarketItems();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigureMainCamera();

            GameObject worldRoot = new GameObject("RiverMarketWorld");
            GameObject terrainObj = BuildTerrainAndRiver(worldRoot.transform);
            Terrain terrain = terrainObj.GetComponent<Terrain>();
            GameObject waterPlane = BuildWater(worldRoot.transform);
            BuildObstacles(worldRoot.transform);
            BuildNpcBoats(worldRoot.transform);
            BuildRiverLife(worldRoot.transform);
            BuildEnvironmentAssets(worldRoot.transform, terrain);

            GameObject systemsRoot = new GameObject("GameSystems");
            TimeManager timeManager = systemsRoot.AddComponent<TimeManager>();
            PlayerStats playerStats = systemsRoot.AddComponent<PlayerStats>();
            InventoryManager inventoryManager = systemsRoot.AddComponent<InventoryManager>();
            EconomyManager economyManager = systemsRoot.AddComponent<EconomyManager>();
            DurabilityManager durabilityManager = systemsRoot.AddComponent<DurabilityManager>();
            MarketNewsController marketNewsController = systemsRoot.AddComponent<MarketNewsController>();
            SaveLoadManager saveLoadManager = systemsRoot.AddComponent<SaveLoadManager>();

            GameObject boat = BuildBoat(worldRoot.transform, boatStats, waterPlane.transform);
            AttachFollowCamera(boat.transform);
            BoatCampManager boatCampManager = boat.AddComponent<BoatCampManager>();
            BambooPoleManager bambooPoleManager = boat.AddComponent<BambooPoleManager>();
            boatCampManager.GetType().GetField("upgradeCatalog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, upgradeCatalog);
            boatCampManager.GetType().GetField("inventoryManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, inventoryManager);
            boatCampManager.GetType().GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, playerStats);
            boatCampManager.GetType().GetField("boatController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, boat.GetComponent<BoatController>());
            bambooPoleManager.GetType().GetField("boatCampManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bambooPoleManager, boatCampManager);
            bambooPoleManager.GetType().GetField("inventoryManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bambooPoleManager, inventoryManager);

            SetupBoatVisualModules(boatCampManager, boat.transform);

            EnvironmentController environmentController = systemsRoot.AddComponent<EnvironmentController>();
            environmentController.GetType().GetField("timeManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(environmentController, timeManager);
            environmentController.GetType().GetField("profile", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(environmentController, environmentProfile);
            environmentController.GetType().GetField("directionalLight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(environmentController, Object.FindAnyObjectByType<Light>());
            environmentController.GetType().GetField("waterTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(environmentController, waterPlane.transform);

            marketNewsController.Configure(timeManager, newsDatabase);
            ConfigureSaveLoad(saveLoadManager, playerStats, inventoryManager, boatCampManager, bambooPoleManager, durabilityManager, timeManager, marketItems);

            RiverMarketHUD hud = systemsRoot.AddComponent<RiverMarketHUD>();
            hud.Configure(timeManager, playerStats, inventoryManager, boatCampManager, marketNewsController, economyManager, durabilityManager, marketItems);

            Selection.activeGameObject = worldRoot;
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [MenuItem("ChoNoi/Scenes/Build River Market Scene", true)]
        private static bool ValidateBuildScene()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes/Core");
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects/RiverMarket");
        }

        private static List<ItemData> LoadMarketItems()
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { BargainingItemsFolder });
            List<ItemData> items = new List<ItemData>();
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

        private static BoatUpgradeCatalogSO EnsureUpgradeCatalog()
        {
            BoatUpgradeCatalogSO catalog = AssetDatabase.LoadAssetAtPath<BoatUpgradeCatalogSO>(UpgradeCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BoatUpgradeCatalogSO>();
                AssetDatabase.CreateAsset(catalog, UpgradeCatalogPath);
            }

            SerializedObject serialized = new SerializedObject(catalog);
            FillUpgradeArray(serialized.FindProperty("storageTiers"), new (string, int, float)[] {
                ("Mo rong khoang nho", 18000, 25f),
                ("Mo rong khoang lon", 28000, 35f),
            });
            FillEngineArray(serialized.FindProperty("engineTiers"), new (string, int, float)[] {
                ("May duoi tom cap 1", 22000, 1.18f),
                ("May duoi tom cap 2", 34000, 1.35f),
            });
            FillRoofArray(serialized.FindProperty("roofTiers"), new (string, int)[] {
                ("Lop mai", 15000),
            });
            FillBambooArray(serialized.FindProperty("bambooPoleTiers"), new (string, int, float)[] {
                ("Cay Beo rong 1", 12000, 0.08f),
                ("Cay Beo rong 2", 16000, 0.10f),
            });
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static MarketNewsDatabaseSO EnsureNewsDatabase()
        {
            MarketNewsDatabaseSO database = AssetDatabase.LoadAssetAtPath<MarketNewsDatabaseSO>(NewsDatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<MarketNewsDatabaseSO>();
                AssetDatabase.CreateAsset(database, NewsDatabasePath);
            }

            Sprite merchantAvatar = AssetDatabase.LoadAssetAtPath<Sprite>(MerchantAvatarPath);
            Sprite villagerAvatar = AssetDatabase.LoadAssetAtPath<Sprite>(VillagerAvatarPath);

            SerializedObject serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty("entries");
            entries.arraySize = 3;

            WriteNewsEntry(entries.GetArrayElementAtIndex(0), 1, "Merchant NPC", merchantAvatar,
                "Khach tu Chau Doc dang san khom dep.",
                "Ngay 1: thuong lai rat chuong khom va cam ngon, gia mo cua se cao vao buoi sang.");
            WriteNewsEntry(entries.GetArrayElementAtIndex(1), 2, "Villager NPC", villagerAvatar,
                "Dan lang can mua bi dao va dua hau de dua cho.",
                "Ngay 2: cac ghe nho tra gia chat hon, nhung rat de mua dua hau cuoi ngay.");
            WriteNewsEntry(entries.GetArrayElementAtIndex(2), 3, "Merchant NPC", merchantAvatar,
                "Tin don co con nuoc rong som.",
                "Ngay 3: nuoc len cao luc rang dong, ghe de luon rach hep; hang nang duoc gia hon neu giao som.");

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(database);
            return database;
        }

        private static EnvironmentProfileSO EnsureEnvironmentProfile()
        {
            EnvironmentProfileSO profile = AssetDatabase.LoadAssetAtPath<EnvironmentProfileSO>(EnvironmentProfilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<EnvironmentProfileSO>();
                AssetDatabase.CreateAsset(profile, EnvironmentProfilePath);
            }

            SetPrivate(profile, "lightColorOverDay", BuildGradient());
            SetPrivate(profile, "lightIntensityOverDay", AnimationCurve.EaseInOut(0f, 0.25f, 1f, 1.1f));
            SetPrivate(profile, "sunPitchOverDay", new AnimationCurve(
                new Keyframe(0f, -20f),
                new Keyframe(0.25f, 25f),
                new Keyframe(0.5f, 70f),
                new Keyframe(0.75f, 15f),
                new Keyframe(1f, -30f)));
            SetPrivate(profile, "fogDensityOverDay", new AnimationCurve(
                new Keyframe(0f, 0.035f),
                new Keyframe(0.25f, 0.02f),
                new Keyframe(0.5f, 0.006f),
                new Keyframe(0.75f, 0.018f),
                new Keyframe(1f, 0.04f)));
            SetPrivate(profile, "maxWaterHeight", 4f);
            SetPrivate(profile, "minWaterHeight", 1.6f);
            SetPrivate(profile, "waterLevelOverDay", new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.25f, 0.85f),
                new Keyframe(0.5f, 0.45f),
                new Keyframe(0.75f, 0.15f),
                new Keyframe(1f, 1f)));

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static BoatStats EnsureBoatStats()
        {
            BoatStats stats = AssetDatabase.LoadAssetAtPath<BoatStats>(BoatStatsPath);
            if (stats == null)
            {
                stats = ScriptableObject.CreateInstance<BoatStats>();
                AssetDatabase.CreateAsset(stats, BoatStatsPath);
            }

            SetPrivate(stats, "thrustForce", 16f);
            SetPrivate(stats, "waterDrag", 1.35f);
            SetPrivate(stats, "sidewaysDrag", 7f);
            SetPrivate(stats, "turnTorque", 4.9f);
            SetPrivate(stats, "lateralThrust", 2.6f);
            SetPrivate(stats, "activeDragFactor", 1f);
            SetPrivate(stats, "coastDragFactor", 0.42f);
            SetPrivate(stats, "riverCurrent", new Vector3(0.14f, 0f, 0.26f));
            SetPrivate(stats, "maxPenaltyFactor", 0.42f);
            SetPrivate(stats, "baseMaxSpeed", 11f);
            EditorUtility.SetDirty(stats);
            return stats;
        }

        private static void BuildEnvironmentAssets(Transform parent, Terrain terrain)
        {
            GameObject envGroup = new GameObject("EnvironmentAssets");
            envGroup.transform.SetParent(parent);

            // Các đường dẫn tới asset cây cối
            string[] treePaths = new string[]
            {
                "Assets/_Project/Art/model_mau/Cay/palm_trees.glb",
                "Assets/_Project/Art/model_mau/Cay/mango_tree.glb",
                "Assets/_Project/Art/model_mau/Cay/tree_elm.glb",
                "Assets/_Project/Art/model_mau/Cay/tree_for_games.glb",
                "Assets/_Project/Art/model_mau/Cay/small_trees.glb"
            };

            // Các đường dẫn tới asset lục bình (water hyacinth)
            string[] hyacinthPaths = new string[]
            {
                "Assets/_Project/Art/model_mau/lucbinh/lucbinhlon/lucbinhlon (1).glb",
                "Assets/_Project/Art/model_mau/lucbinh/lucbinhlon/lucbinhlon (2).glb",
                "Assets/_Project/Art/model_mau/lucbinh/lucbinhlon/lucbinhlon (3).glb",
                "Assets/_Project/Art/model_mau/lucbinh/lucbinhlon/lucbinhlon (4).glb",
                "Assets/_Project/Art/model_mau/lucbinh/lucbinhdonle/lucbinhdonle (1).glb",
                "Assets/_Project/Art/model_mau/lucbinh/lucbinhdonle/lucbinhdonle (2).glb"
            };

            Random.InitState(42); // Seed cố định để sinh ngẫu nhiên nhưng ổn định giữa các lần build

            // Tọa độ định nghĩa sông ngòi để tránh đặt cây đè lên sông
            Vector2 mainStart = new Vector2(90f, 0f);
            Vector2 junction = new Vector2(90f, 82f);
            Vector2 leftForkEnd = new Vector2(20f, 180f);
            Vector2 rightForkEnd = new Vector2(160f, 180f);
            float mainHalfWidth = 21f;
            float forkHalfWidth = 13f;

            // Rải cây cối trên đất liền dọc theo 2 bên bờ sông
            for (float x = 10f; x <= 170f; x += 12f)
            {
                for (float z = 10f; z <= 170f; z += 12f)
                {
                    // Thêm độ lệch ngẫu nhiên nhẹ để phân bố trông tự nhiên
                    float posX = x + Random.Range(-4f, 4f);
                    float posZ = z + Random.Range(-4f, 4f);
                    Vector2 point = new Vector2(posX, posZ);

                    float dMain = DistanceToSegment(point, mainStart, junction);
                    float dLeft = DistanceToSegment(point, junction, leftForkEnd);
                    float dRight = DistanceToSegment(point, junction, rightForkEnd);

                    // Nếu vị trí là đất liền (nằm ngoài phạm vi sông + biên an toàn)
                    if (dMain > mainHalfWidth + 3f && dLeft > forkHalfWidth + 3f && dRight > forkHalfWidth + 3f)
                    {
                        float posY = terrain.SampleHeight(new Vector3(posX, 0f, posZ));
                        
                        // Chọn loại cây. Ưu tiên cây dừa (palm_trees) ở gần mép bờ sông hơn
                        string path = treePaths[Random.Range(0, treePaths.Length)];
                        float minDistanceToWater = Mathf.Min(dMain, dLeft, dRight);
                        if (minDistanceToWater < 35f && Random.value < 0.7f)
                        {
                            path = "Assets/_Project/Art/model_mau/Cay/palm_trees.glb";
                        }

                        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            GameObject treeInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                            if (treeInstance != null)
                            {
                                treeInstance.name = Path.GetFileNameWithoutExtension(path) + "_" + posX + "_" + posZ;
                                treeInstance.transform.SetParent(envGroup.transform);
                                treeInstance.transform.position = new Vector3(posX, posY, posZ);
                                treeInstance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                                // Điều chỉnh tỉ lệ tương thích cho từng loại cây
                                float scaleMultiplier = 1f;
                                if (path.Contains("palm_trees")) scaleMultiplier = Random.Range(1.8f, 2.5f);
                                else if (path.Contains("mango_tree")) scaleMultiplier = Random.Range(0.6f, 0.9f);
                                else if (path.Contains("tree_elm")) scaleMultiplier = Random.Range(0.8f, 1.2f);
                                else if (path.Contains("tree_for_games")) scaleMultiplier = Random.Range(1f, 1.4f);
                                else if (path.Contains("small_trees")) scaleMultiplier = Random.Range(1.2f, 1.8f);

                                treeInstance.transform.localScale = Vector3.one * scaleMultiplier;
                            }
                        }
                    }
                }
            }

            // Rải các đám bèo lục bình (water hyacinth) trôi nổi trên mặt sông
            // Mặt nước có Y cố định khoảng 4f. Ưu tiên rải lục bình tụ lại dọc theo ven bờ sông (vùng nước nông)
            for (int i = 0; i < 45; i++)
            {
                float posX = Random.Range(15f, 165f);
                float posZ = Random.Range(15f, 165f);
                Vector2 point = new Vector2(posX, posZ);

                float dMain = DistanceToSegment(point, mainStart, junction);
                float dLeft = DistanceToSegment(point, junction, leftForkEnd);
                float dRight = DistanceToSegment(point, junction, rightForkEnd);

                bool inRiver = dMain < mainHalfWidth || dLeft < forkHalfWidth || dRight < forkHalfWidth;
                bool nearBank = false;

                if (inRiver)
                {
                    if (dMain < mainHalfWidth && dMain > mainHalfWidth - 6f) nearBank = true;
                    else if (dLeft < forkHalfWidth && dLeft > forkHalfWidth - 4f) nearBank = true;
                    else if (dRight < forkHalfWidth && dRight > forkHalfWidth - 4f) nearBank = true;
                }

                if (nearBank)
                {
                    string path = hyacinthPaths[Random.Range(0, hyacinthPaths.Length)];
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab != null)
                    {
                        GameObject hyacinthInstance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                        if (hyacinthInstance != null)
                        {
                            hyacinthInstance.name = "WaterHyacinth_" + i;
                            hyacinthInstance.transform.SetParent(envGroup.transform);
                            
                            // Đặt cao độ nổi mấp mé trên mặt nước (Y = 3.9f)
                            hyacinthInstance.transform.position = new Vector3(posX, 3.9f, posZ);
                            hyacinthInstance.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                            float scale = Random.Range(1.2f, 1.8f);
                            if (path.Contains("lucbinhdonle")) scale *= 0.5f;
                            hyacinthInstance.transform.localScale = Vector3.one * scale;

                            // Gắn AmbientBob để lục bình bập bềnh nhấp nhô sống động
                            AmbientBob bob = hyacinthInstance.AddComponent<AmbientBob>();
                            SetPrivate(bob, "bobAxis", new Vector3(0f, 0.08f, 0f));
                            SetPrivate(bob, "swayAxis", new Vector3(4f, 0f, 4f));
                            SetPrivate(bob, "bobSpeed", Random.Range(0.6f, 1.2f));
                            SetPrivate(bob, "phaseOffset", Random.Range(0f, Mathf.PI * 2f));
                        }
                    }
                }
            }
        }

        private static GameObject BuildTerrainAndRiver(Transform parent)
        {
            TerrainData terrainData = new TerrainData
            {
                heightmapResolution = 513,
                size = new Vector3(180f, 18f, 180f)
            };

            float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
            int res = terrainData.heightmapResolution;
            Vector2 mainStart = new Vector2(90f, 0f);
            Vector2 junction = new Vector2(90f, 82f);
            Vector2 leftForkEnd = new Vector2(20f, 180f);
            Vector2 rightForkEnd = new Vector2(160f, 180f);
            float baseHeight = 0.30f;
            float riverBed = 0.08f;
            float mainHalfWidth = 21f;
            float forkHalfWidth = 13f;

            for (int row = 0; row < res; row++)
            {
                for (int col = 0; col < res; col++)
                {
                    Vector2 point = new Vector2((float)col / (res - 1) * 180f, (float)row / (res - 1) * 180f);
                    float dMain = DistanceToSegment(point, mainStart, junction);
                    float dLeft = DistanceToSegment(point, junction, leftForkEnd);
                    float dRight = DistanceToSegment(point, junction, rightForkEnd);
                    float carve = 0f;
                    if (dMain < mainHalfWidth) carve = Mathf.Max(carve, (1f - dMain / mainHalfWidth) * (baseHeight - riverBed));
                    if (dLeft < forkHalfWidth) carve = Mathf.Max(carve, (1f - dLeft / forkHalfWidth) * (baseHeight - riverBed));
                    if (dRight < forkHalfWidth) carve = Mathf.Max(carve, (1f - dRight / forkHalfWidth) * (baseHeight - riverBed));
                    heights[row, col] = baseHeight - carve;
                }
            }

            terrainData.SetHeights(0, 0, heights);
            GameObject terrainRoot = Terrain.CreateTerrainGameObject(terrainData);
            terrainRoot.name = "RiverTerrain";
            terrainRoot.transform.SetParent(parent);
            terrainRoot.layer = 1;
            return terrainRoot;
        }

        private static GameObject BuildWater(Transform parent)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterSurface";
            water.transform.SetParent(parent);
            water.transform.position = new Vector3(90f, 4f, 90f);
            water.transform.localScale = new Vector3(18f, 1f, 18f);

            // MeshCollider from Primitive Plane is concave and doesn't support trigger, disable it
            MeshCollider meshCol = water.GetComponent<MeshCollider>();
            if (meshCol != null) meshCol.enabled = false;

            Renderer renderer = water.GetComponent<Renderer>();
            Material waterMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            waterMat.color = new Color(0.13f, 0.45f, 0.56f, 0.65f);
            renderer.sharedMaterial = waterMat;

            BoxCollider box = water.GetComponent<BoxCollider>();
            if (box == null) box = water.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, -0.04f, 0f);
            box.size = new Vector3(10f, 0.1f, 10f);
            box.isTrigger = true; // Must be trigger so it doesn't block boat physics
            return water;
        }

        private static void BuildObstacles(Transform parent)
        {
            GameObject obstacles = new GameObject("RiverObstacles");
            obstacles.transform.SetParent(parent);

            CreateObstacle(obstacles.transform, "HyacinthPatch", PrimitiveType.Sphere, new Vector3(74f, 2.3f, 105f), new Vector3(3f, 0.35f, 2.5f), new Color(0.20f, 0.55f, 0.25f), true);
            CreateObstacle(obstacles.transform, "WoodPost", PrimitiveType.Cylinder, new Vector3(112f, 2.7f, 76f), new Vector3(0.4f, 2.4f, 0.4f), new Color(0.36f, 0.23f, 0.13f), false);
            CreateObstacle(obstacles.transform, "BrokenBoat", PrimitiveType.Cube, new Vector3(58f, 2.5f, 136f), new Vector3(4f, 0.8f, 1.6f), new Color(0.25f, 0.25f, 0.25f), false);
        }

        private static void BuildNpcBoats(Transform parent)
        {
            GameObject npcRoot = new GameObject("NpcBoats");
            npcRoot.transform.SetParent(parent);

            BuildNpcBoat(npcRoot.transform, "MerchantLargeBoat", new Vector3(106f, 4.35f, 108f), new Vector3(3.6f, 0.9f, 8.4f), new Color(0.48f, 0.20f, 0.12f), true);
            BuildNpcBoat(npcRoot.transform, "FoodVendorSmallBoat", new Vector3(64f, 4.2f, 86f), new Vector3(2.0f, 0.6f, 4.2f), new Color(0.20f, 0.34f, 0.52f), false);
        }

        private static void BuildNpcBoat(Transform parent, string name, Vector3 position, Vector3 hullScale, Color hullColor, bool isLargeBoat)
        {
            GameObject boatRoot = new GameObject(name);
            boatRoot.transform.SetParent(parent);
            boatRoot.transform.position = position;
            boatRoot.transform.rotation = Quaternion.Euler(0f, isLargeBoat ? -20f : 35f, 0f);
            boatRoot.AddComponent<AmbientBob>();

            GameObject hull = CreatePrimitiveChild(boatRoot.transform, "Hull", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), hullScale, hullColor);
            hull.GetComponent<Collider>().enabled = false;
            GameObject canopy = CreatePrimitiveChild(boatRoot.transform, "Canopy", PrimitiveType.Cube, new Vector3(0f, 1.0f, 0f), new Vector3(hullScale.x * 0.85f, 0.14f, hullScale.z * 0.5f), isLargeBoat ? new Color(0.82f, 0.72f, 0.32f) : new Color(0.85f, 0.42f, 0.22f));
            canopy.GetComponent<Collider>().enabled = false;
            GameObject mast = CreatePrimitiveChild(boatRoot.transform, "Pole", PrimitiveType.Cylinder, new Vector3(hullScale.x * 0.28f, 1.2f, 0.6f), new Vector3(0.08f, 1.15f, 0.08f), new Color(0.58f, 0.46f, 0.26f));
            mast.GetComponent<Collider>().enabled = false;

            if (isLargeBoat)
            {
                GameObject cargoA = CreatePrimitiveChild(boatRoot.transform, "CargoCrateA", PrimitiveType.Cube, new Vector3(-0.9f, 0.7f, -1.2f), new Vector3(0.8f, 0.6f, 0.8f), new Color(0.62f, 0.42f, 0.18f));
                GameObject cargoB = CreatePrimitiveChild(boatRoot.transform, "CargoCrateB", PrimitiveType.Cube, new Vector3(0.9f, 0.7f, -0.4f), new Vector3(0.8f, 0.6f, 0.8f), new Color(0.62f, 0.42f, 0.18f));
                cargoA.GetComponent<Collider>().enabled = false;
                cargoB.GetComponent<Collider>().enabled = false;
            }
            else
            {
                GameObject soupPot = CreatePrimitiveChild(boatRoot.transform, "SoupPot", PrimitiveType.Sphere, new Vector3(0f, 0.7f, -0.4f), new Vector3(0.8f, 0.45f, 0.8f), new Color(0.16f, 0.16f, 0.18f));
                soupPot.GetComponent<Collider>().enabled = false;
            }
        }

        private static void BuildRiverLife(Transform parent)
        {
            GameObject lifeRoot = new GameObject("RiverLife");
            lifeRoot.transform.SetParent(parent);

            CreateReedCluster(lifeRoot.transform, "LeftBankReeds", new Vector3(49f, 3.1f, 92f), 5);
            CreateReedCluster(lifeRoot.transform, "RightBankReeds", new Vector3(132f, 3.1f, 68f), 6);
            CreateMistBand(lifeRoot.transform, "MorningMist", new Vector3(92f, 5.1f, 118f), new Vector3(14f, 0.7f, 5f));
        }

        private static void CreateReedCluster(Transform parent, string name, Vector3 center, int count)
        {
            GameObject cluster = new GameObject(name);
            cluster.transform.SetParent(parent);
            cluster.transform.position = center;

            for (int index = 0; index < count; index++)
            {
                float offsetX = (index % 3 - 1) * 1.15f;
                float offsetZ = (index / 3) * 1.15f;
                GameObject reed = CreatePrimitiveChild(cluster.transform, $"Reed_{index}", PrimitiveType.Cylinder, new Vector3(offsetX, 0.9f, offsetZ), new Vector3(0.05f, 1.2f + index * 0.08f, 0.05f), new Color(0.45f, 0.60f, 0.20f));
                reed.GetComponent<Collider>().enabled = false;
                AmbientBob ambient = reed.AddComponent<AmbientBob>();
                SetPrivate(ambient, "bobAxis", new Vector3(0f, 0.05f, 0f));
                SetPrivate(ambient, "swayAxis", new Vector3(6f, 0f, 6f));
                SetPrivate(ambient, "phaseOffset", index * 0.4f);
            }
        }

        private static void CreateMistBand(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject mist = CreatePrimitiveChild(parent, name, PrimitiveType.Sphere, position, scale, new Color(0.86f, 0.90f, 0.94f, 0.35f));
            Collider collider = mist.GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            AmbientBob ambient = mist.AddComponent<AmbientBob>();
            SetPrivate(ambient, "bobAxis", new Vector3(0f, 0.18f, 0f));
            SetPrivate(ambient, "swayAxis", new Vector3(0f, 4f, 0f));
            SetPrivate(ambient, "bobSpeed", 0.35f);
        }

        private static GameObject BuildBoat(Transform parent, BoatStats boatStats, Transform waterSurface)
        {
            GameObject boat = new GameObject("PlayerBoat");
            boat.transform.SetParent(parent);
            boat.transform.position = new Vector3(90f, 4.6f, 24f);
            boat.AddComponent<BoxCollider>().size = new Vector3(1.8f, 1.2f, 5.4f);

            Rigidbody rb = boat.AddComponent<Rigidbody>();
            rb.useGravity = true;
            rb.mass = 1.2f;
            rb.linearDamping = 0.6f;
            rb.angularDamping = 0.85f;

            PCBoatInput input = boat.AddComponent<PCBoatInput>();
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputActionsPath);
            SetPrivate(input, "inputActions", inputActions);

            BoatController controller = boat.AddComponent<BoatController>();
            SetPrivate(controller, "boatStats", boatStats);
            SetPrivate(controller, "riverbedLayer", (LayerMask)(1 << 1));

            BoatBuoyancy buoyancy = boat.AddComponent<BoatBuoyancy>();
            SetPrivate(buoyancy, "waterSurface", waterSurface);

            GameObject visualRoot = new GameObject("BoatVisualRoot");
            visualRoot.transform.SetParent(boat.transform, false);
            GameObject boatModel = AssetDatabase.LoadAssetAtPath<GameObject>(BoatModelPath);
            if (boatModel != null)
            {
                GameObject modelInstance = PrefabUtility.InstantiatePrefab(boatModel) as GameObject;
                if (modelInstance != null)
                {
                    modelInstance.name = "BoatModel";
                    modelInstance.transform.SetParent(visualRoot.transform, false);
                    modelInstance.transform.localScale = new Vector3(458f, 458f, 458f);
                    modelInstance.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                }
            }
            else
            {
                CreatePrimitiveChild(visualRoot.transform, "BoatHull", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.8f, 5.4f), new Color(0.49f, 0.31f, 0.18f));
            }

            return boat;
        }

        private static void SetupBoatVisualModules(BoatCampManager boatCampManager, Transform boat)
        {
            Transform visualRoot = boat.Find("BoatVisualRoot");
            if (visualRoot == null) return;

            GameObject storage = CreatePrimitiveChild(visualRoot, "StorageModule", PrimitiveType.Cube, new Vector3(0f, 0.95f, -0.4f), new Vector3(1.4f, 0.5f, 1.4f), new Color(0.58f, 0.42f, 0.22f));
            GameObject roof = CreatePrimitiveChild(visualRoot, "RoofModule", PrimitiveType.Cube, new Vector3(0f, 1.8f, -0.5f), new Vector3(2.1f, 0.18f, 2.8f), new Color(0.26f, 0.36f, 0.22f));
            GameObject engine1 = CreatePrimitiveChild(visualRoot, "EngineTier1", PrimitiveType.Cylinder, new Vector3(0.6f, 0.5f, -2.5f), new Vector3(0.25f, 0.35f, 0.25f), new Color(0.32f, 0.32f, 0.35f));
            GameObject engine2 = CreatePrimitiveChild(visualRoot, "EngineTier2", PrimitiveType.Cylinder, new Vector3(-0.6f, 0.5f, -2.5f), new Vector3(0.30f, 0.45f, 0.30f), new Color(0.18f, 0.18f, 0.20f));
            GameObject bamboo1 = CreatePrimitiveChild(visualRoot, "BambooPoleTier1", PrimitiveType.Cylinder, new Vector3(1.0f, 1.8f, 0.4f), new Vector3(0.06f, 1.6f, 0.06f), new Color(0.63f, 0.58f, 0.30f));
            GameObject bamboo2 = CreatePrimitiveChild(visualRoot, "BambooPoleTier2", PrimitiveType.Cylinder, new Vector3(-1.0f, 1.8f, 0.4f), new Vector3(0.06f, 1.7f, 0.06f), new Color(0.63f, 0.58f, 0.30f));

            storage.SetActive(false);
            roof.SetActive(false);
            engine1.SetActive(false);
            engine2.SetActive(false);
            bamboo1.SetActive(false);
            bamboo2.SetActive(false);

            SetPrivate(boatCampManager, "storageModuleObject", storage);
            SetPrivate(boatCampManager, "roofObject", roof);
            SetPrivate(boatCampManager, "bambooPoleObject", bamboo1);
            SetPrivate(boatCampManager, "bambooPoleLevelObjects", new List<GameObject> { bamboo1, bamboo2 });
            SetPrivate(boatCampManager, "engineLevelObjects", new List<GameObject> { engine1, engine2 });
        }

        private static void ConfigureMainCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            cam.transform.position = new Vector3(90f, 18f, 2f);
            cam.transform.rotation = Quaternion.Euler(28f, 0f, 0f);
        }

        private static void AttachFollowCamera(Transform boat)
        {
            Camera cam = Camera.main;
            if (cam == null || boat == null) return;
            BoatFollowCamera followCamera = cam.GetComponent<BoatFollowCamera>();
            if (followCamera == null)
            {
                followCamera = cam.gameObject.AddComponent<BoatFollowCamera>();
            }

            followCamera.Configure(boat);
        }

        private static void ConfigureSaveLoad(
            SaveLoadManager saveLoadManager,
            PlayerStats playerStats,
            InventoryManager inventoryManager,
            BoatCampManager boatCampManager,
            BambooPoleManager bambooPoleManager,
            DurabilityManager durabilityManager,
            TimeManager timeManager,
            List<ItemData> marketItems)
        {
            if (saveLoadManager == null)
                return;

            SetPrivate(saveLoadManager, "playerStats", playerStats);
            SetPrivate(saveLoadManager, "inventoryManager", inventoryManager);
            SetPrivate(saveLoadManager, "boatCampManager", boatCampManager);
            SetPrivate(saveLoadManager, "bambooPoleManager", bambooPoleManager);
            SetPrivate(saveLoadManager, "durabilityManager", durabilityManager);
            SetPrivate(saveLoadManager, "timeManager", timeManager);
            SetPrivate(saveLoadManager, "masterItemDatabase", marketItems);
        }

        private static Gradient BuildGradient()
        {
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.38f, 0.52f, 0.76f), 0f),
                    new GradientColorKey(new Color(1.0f, 0.68f, 0.35f), 0.25f),
                    new GradientColorKey(new Color(1f, 0.97f, 0.82f), 0.5f),
                    new GradientColorKey(new Color(0.97f, 0.58f, 0.32f), 0.75f),
                    new GradientColorKey(new Color(0.22f, 0.28f, 0.44f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }

        private static void FillUpgradeArray(SerializedProperty property, (string displayName, int costMoney, float capacityBonus)[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("displayName").stringValue = values[i].displayName;
                item.FindPropertyRelative("costMoney").intValue = values[i].costMoney;
                item.FindPropertyRelative("capacityBonus").floatValue = values[i].capacityBonus;
            }
        }

        private static void FillEngineArray(SerializedProperty property, (string displayName, int costMoney, float thrustMultiplier)[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("displayName").stringValue = values[i].displayName;
                item.FindPropertyRelative("costMoney").intValue = values[i].costMoney;
                item.FindPropertyRelative("thrustMultiplier").floatValue = values[i].thrustMultiplier;
            }
        }

        private static void FillRoofArray(SerializedProperty property, (string displayName, int costMoney)[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("displayName").stringValue = values[i].displayName;
                item.FindPropertyRelative("costMoney").intValue = values[i].costMoney;
            }
        }

        private static void FillBambooArray(SerializedProperty property, (string displayName, int costMoney, float hagglingBonusRatio)[] values)
        {
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                SerializedProperty item = property.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("displayName").stringValue = values[i].displayName;
                item.FindPropertyRelative("costMoney").intValue = values[i].costMoney;
                item.FindPropertyRelative("hagglingBonusRatio").floatValue = values[i].hagglingBonusRatio;
            }
        }

        private static void WriteNewsEntry(SerializedProperty property, int day, string name, Sprite avatar, string headline, string rumor)
        {
            property.FindPropertyRelative("dayNumber").intValue = day;
            property.FindPropertyRelative("npcName").stringValue = name;
            property.FindPropertyRelative("npcAvatar").objectReferenceValue = avatar;
            property.FindPropertyRelative("headline").stringValue = headline;
            property.FindPropertyRelative("marketRumor").stringValue = rumor;
        }

        private static GameObject CreateObstacle(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, bool flatten)
        {
            GameObject obstacle = GameObject.CreatePrimitive(type);
            obstacle.name = name;
            obstacle.transform.SetParent(parent);
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            if (flatten) obstacle.transform.rotation = Quaternion.Euler(0f, 0f, 12f);
            ApplyColorMaterial(obstacle.GetComponent<Renderer>(), color);
            return obstacle;
        }

        private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color)
        {
            GameObject child = GameObject.CreatePrimitive(type);
            child.name = name;
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localScale = localScale;
            ApplyColorMaterial(child.GetComponent<Renderer>(), color);
            return child;
        }

        private static void ApplyColorMaterial(Renderer renderer, Color color)
        {
            if (renderer == null) return;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
            Material material = new Material(shader)
            {
                color = color
            };
            renderer.sharedMaterial = material;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float t = Vector2.Dot(point - a, ab) / Mathf.Max(ab.sqrMagnitude, 0.0001f);
            t = Mathf.Clamp01(t);
            return Vector2.Distance(point, a + ab * t);
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
        }
    }
}
#endif
