#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using ChoNoi.Application;
using ChoNoi.Infrastructure;
using ChoNoi.Presentation;
using ChoNoi.Presentation.Environment;
using ChoNoi.Presentation.NPC;
using ChoNoi.Presentation.Player;
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
        private const string PaddlingBoatModelPath = "Assets/_Project/Animation/Boat_with_padd/boat_with_pad.fbx";
        private const string PaddlingBoatControllerPath = "Assets/_Project/Animation/Boat_with_padd/BoatAnimatorController.controller";
        private const string BoatModelPath = "Assets/_Project/Art/Models/thuyencoban.fbx";
        private const string NpcModel1Path = "Assets/_Project/Animation/Character_1/Neutral Idle.fbx";
        private const string NpcModel2Path = "Assets/_Project/Animation/Character_2/Neutral Idle.fbx";
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

            ConfigureAnimations();

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
            BuildPier(worldRoot.transform);
            BuildNpcBoats(worldRoot.transform);
            BuildRiverLife(worldRoot.transform);
            BuildEnvironmentAssets(worldRoot.transform, terrain);
            BuildAmbientNpcCrowd(worldRoot.transform, terrain);

            GameObject systemsRoot = new GameObject("GameSystems");
            TimeManager timeManager = systemsRoot.AddComponent<TimeManager>();
            PlayerStats playerStats = systemsRoot.AddComponent<PlayerStats>();
            InventoryManager inventoryManager = systemsRoot.AddComponent<InventoryManager>();
            EconomyManager economyManager = systemsRoot.AddComponent<EconomyManager>();
            DurabilityManager durabilityManager = systemsRoot.AddComponent<DurabilityManager>();
            MarketNewsController marketNewsController = systemsRoot.AddComponent<MarketNewsController>();
            SaveLoadManager saveLoadManager = systemsRoot.AddComponent<SaveLoadManager>();

            GameObject boat = BuildBoat(worldRoot.transform, boatStats, waterPlane.transform);
            GameObject shorePlayer = BuildShorePlayer(worldRoot.transform, boat);
            BoatFollowCamera followCamera = AttachFollowCamera(shorePlayer.transform);
            BoatCampManager boatCampManager = boat.AddComponent<BoatCampManager>();
            BambooPoleManager bambooPoleManager = boat.AddComponent<BambooPoleManager>();
            boatCampManager.GetType().GetField("upgradeCatalog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, upgradeCatalog);
            boatCampManager.GetType().GetField("inventoryManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, inventoryManager);
            boatCampManager.GetType().GetField("playerStats", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, playerStats);
            boatCampManager.GetType().GetField("boatController", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(boatCampManager, boat.GetComponent<BoatController>());
            bambooPoleManager.GetType().GetField("boatCampManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bambooPoleManager, boatCampManager);
            bambooPoleManager.GetType().GetField("inventoryManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(bambooPoleManager, inventoryManager);

            SetupBoatVisualModules(boatCampManager, boat.transform);
            SetupBoardingFlow(shorePlayer, boat, followCamera);

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
            SetPrivate(profile, "maxWaterHeight", 3.1f);
            SetPrivate(profile, "minWaterHeight", 3.1f);
            SetPrivate(profile, "waterLevelOverDay", new AnimationCurve(
                new Keyframe(0f, 1f),
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
            Vector2 mainStart = new Vector2(120f, 0f);
            Vector2 junction = new Vector2(120f, 92f);
            Vector2 leftForkEnd = new Vector2(36f, 220f);
            Vector2 rightForkEnd = new Vector2(204f, 220f);
            float mainHalfWidth = 18f;
            float forkHalfWidth = 14f;

            // Rải cây cối trên đất liền dọc theo 2 bên bờ sông
            for (float x = 12f; x <= 228f; x += 18f)
            {
                for (float z = 12f; z <= 208f; z += 18f)
                {
                    // Thêm độ lệch ngẫu nhiên nhẹ để phân bố trông tự nhiên
                    float posX = x + Random.Range(-4f, 4f);
                    float posZ = z + Random.Range(-4f, 4f);
                    Vector2 point = new Vector2(posX, posZ);

                    float dMain = DistanceToSegment(point, mainStart, junction);
                    float dLeft = DistanceToSegment(point, junction, leftForkEnd);
                    float dRight = DistanceToSegment(point, junction, rightForkEnd);

                    // Nếu vị trí là đất liền (nằm ngoài phạm vi sông + biên an toàn)
                    if (dMain > mainHalfWidth + 8f && dLeft > forkHalfWidth + 8f && dRight > forkHalfWidth + 8f)
                    {
                        // Giu khu spawn va hanh lang camera thong thoang, tranh cay moc sat mat nguoi choi.
                        if (posX > 48f && posX < 108f && posZ < 72f)
                            continue;

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
                                ApplyTreeOrientationFix(treeInstance, path);

                                // Điều chỉnh tỉ lệ tương thích cho từng loại cây
                                float scaleMultiplier = 1f;
                                if (path.Contains("palm_trees")) scaleMultiplier = Random.Range(0.62f, 0.86f);
                                else if (path.Contains("mango_tree")) scaleMultiplier = Random.Range(0.38f, 0.58f);
                                else if (path.Contains("small_trees")) scaleMultiplier = Random.Range(0.55f, 0.82f);

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
                float posX = Random.Range(30f, 210f);
                float posZ = Random.Range(25f, 205f);
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
                size = new Vector3(240f, 18f, 220f)
            };

            float[,] heights = new float[terrainData.heightmapResolution, terrainData.heightmapResolution];
            int res = terrainData.heightmapResolution;
            Vector2 mainStart = new Vector2(120f, 0f);
            Vector2 junction = new Vector2(120f, 92f);
            Vector2 leftForkEnd = new Vector2(36f, 220f);
            Vector2 rightForkEnd = new Vector2(204f, 220f);
            float baseHeight = 0.23f;
            float riverBed = 0.015f;
            float mainHalfWidth = 18f;
            float forkHalfWidth = 14f;
            float bankBlendWidth = 6f;

            for (int row = 0; row < res; row++)
            {
                for (int col = 0; col < res; col++)
                {
                    Vector2 point = new Vector2((float)col / (res - 1) * terrainData.size.x, (float)row / (res - 1) * terrainData.size.z);
                    float dMain = DistanceToSegment(point, mainStart, junction);
                    float dLeft = DistanceToSegment(point, junction, leftForkEnd);
                    float dRight = DistanceToSegment(point, junction, rightForkEnd);
                    float riverBlend = 0f;
                    riverBlend = Mathf.Max(riverBlend, 1f - Mathf.SmoothStep(0f, bankBlendWidth, Mathf.Max(0f, dMain - mainHalfWidth)));
                    riverBlend = Mathf.Max(riverBlend, 1f - Mathf.SmoothStep(0f, bankBlendWidth, Mathf.Max(0f, dLeft - forkHalfWidth)));
                    riverBlend = Mathf.Max(riverBlend, 1f - Mathf.SmoothStep(0f, bankBlendWidth, Mathf.Max(0f, dRight - forkHalfWidth)));

                    // Multi-octave fractal Perlin noise for rugged terrain
                    float n1 = Mathf.PerlinNoise(point.x * 0.015f, point.y * 0.015f) * 0.05f;   // Low frequency hills
                    float n2 = Mathf.PerlinNoise(point.x * 0.05f, point.y * 0.05f) * 0.018f;    // Mid frequency bumps
                    float n3 = Mathf.PerlinNoise(point.x * 0.12f, point.y * 0.12f) * 0.006f;    // High frequency details
                    float landNoise = n1 + n2 + n3;

                    // Smoothly suppress noise on the riverbed and banks to keep them clean
                    float height = Mathf.Lerp(baseHeight + (1f - riverBlend) * landNoise, riverBed, riverBlend);

                    // Khu bờ spawn phẳng, rộng để người chơi bắt đầu đi bộ trước khi lên ghe.
                    if (point.y < 52f && point.x < 112f)
                        height = Mathf.Lerp(height, baseHeight - 0.02f, 0.92f);

                    // Mo them hanh lang tam nhin o khu spawn de camera khong bi vach dat xam chan mat.
                    if (point.y < 78f && point.x > 70f && point.x < 132f)
                        height = Mathf.Min(height, baseHeight - 0.035f);

                    heights[row, col] = height;
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
            water.transform.position = new Vector3(120f, 3.1f, 110f);
            water.transform.localScale = new Vector3(24f, 1f, 22f);

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

            CreateObstacle(obstacles.transform, "HyacinthPatch", PrimitiveType.Sphere, new Vector3(103f, 3.25f, 122f), new Vector3(3f, 0.35f, 2.5f), new Color(0.20f, 0.55f, 0.25f), true);
            GameObject woodPost = CreateObstacle(obstacles.transform, "WoodPost", PrimitiveType.Cylinder, new Vector3(135f, 3.4f, 115f), new Vector3(0.8f, 3.5f, 0.8f), new Color(0.36f, 0.23f, 0.13f), false);
            CreatePrimitiveChild(woodPost.transform, "RedMarker", PrimitiveType.Cylinder, new Vector3(0f, 0.7f, 0f), new Vector3(1.2f, 0.25f, 1.2f), new Color(0.9f, 0.1f, 0.1f));
            CreateObstacle(obstacles.transform, "BrokenBoat", PrimitiveType.Cube, new Vector3(73f, 3.35f, 154f), new Vector3(4f, 0.8f, 1.6f), new Color(0.25f, 0.25f, 0.25f), false);
        }

        private static void BuildPier(Transform parent)
        {
            GameObject pierRoot = new GameObject("WoodenPier");
            pierRoot.transform.SetParent(parent);

            // Horizontal deck: cube from X = 94 to 114, Y = 4.25f, Z = 30f.
            // Length in X is 20f. Width in Z is 3f. Thickness in Y is 0.2f.
            GameObject deck = CreatePrimitiveChild(pierRoot.transform, "Deck", PrimitiveType.Cube, new Vector3(104f, 4.25f, 30f), new Vector3(20f, 0.2f, 3f), new Color(0.45f, 0.33f, 0.22f));

            // Support pillars: vertical cylinders.
            // Support pillars at X = 99, 104, 109, 113.5 on both Z sides (Z = 28.8 and Z = 31.2).
            // Pillars height from riverbed (approx Y = 1.0f) to deck Y = 4.25f. Height = 3.5f, Y-pos = 2.5f.
            float[] pillarXs = { 99f, 104f, 109f, 113.5f };
            float[] pillarZs = { 28.8f, 31.2f };
            int i = 0;
            foreach (float px in pillarXs)
            {
                foreach (float pz in pillarZs)
                {
                    CreatePrimitiveChild(pierRoot.transform, $"Pillar_{i++}", PrimitiveType.Cylinder, new Vector3(px, 2.5f, pz), new Vector3(0.3f, 3.5f, 0.3f), new Color(0.35f, 0.25f, 0.15f));
                }
            }
        }

        private static void BuildNpcBoats(Transform parent)
        {
            GameObject npcRoot = new GameObject("NpcBoats");
            npcRoot.transform.SetParent(parent);

            BuildNpcBoat(npcRoot.transform, "MerchantLargeBoat", new Vector3(140f, 3.55f, 118f), new Vector3(3.6f, 0.9f, 8.4f), new Color(0.48f, 0.20f, 0.12f), true);
            BuildNpcBoat(npcRoot.transform, "FoodVendorSmallBoat", new Vector3(111f, 3.45f, 84f), new Vector3(2.0f, 0.6f, 4.2f), new Color(0.20f, 0.34f, 0.52f), false);
            
            // Build the paddling NPC boat
            BuildPaddlingNpcBoat(npcRoot.transform);
        }

        private static void BuildPaddlingNpcBoat(Transform parent)
        {
            GameObject boatRoot = new GameObject("PaddlingNpcBoat");
            boatRoot.transform.SetParent(parent, false);
            boatRoot.transform.position = new Vector3(120f, 3.1f, 40f);

            var patrol = boatRoot.AddComponent<NpcBoatPatrol>();
            
            Vector3[] points = new Vector3[]
            {
                new Vector3(120f, 3.1f, 40f),
                new Vector3(120f, 3.1f, 85f),
                new Vector3(108f, 3.1f, 100f),
                new Vector3(108f, 3.1f, 115f),
                new Vector3(120f, 3.1f, 95f),
                new Vector3(120f, 3.1f, 65f)
            };

            GameObject visualRoot = new GameObject("BoatVisualRoot");
            visualRoot.transform.SetParent(boatRoot.transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            GameObject boatModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PaddlingBoatModelPath);
            Animator npcAnimator = null;

            if (boatModelPrefab != null)
            {
                GameObject model = PrefabUtility.InstantiatePrefab(boatModelPrefab) as GameObject;
                if (model != null)
                {
                    model.name = "BoatModel";
                    model.transform.SetParent(visualRoot.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // Rotate 180 so it travels pointy bow first
                    model.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f); // Scale boat down to fit the NPC

                    CleanImportedModel(model);
                    DisableColliders(model);
                    ApplyBoatMaterials(model);

                    // Setup Animator for the boat paddles (and disable it)
                    Animator boatAnimator = model.GetComponent<Animator>();
                    if (boatAnimator == null)
                    {
                        boatAnimator = model.AddComponent<Animator>();
                    }
                    boatAnimator.applyRootMotion = false;
                    boatAnimator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(PaddlingBoatControllerPath);
                    boatAnimator.enabled = true;

                    var bob = model.AddComponent<AmbientBob>();
                    SetPrivate(bob, "bobAxis", new Vector3(0f, 0.08f, 0f));
                    SetPrivate(bob, "swayAxis", new Vector3(0f, 0f, 2.5f));
                    SetPrivate(bob, "bobSpeed", 0.9f);
                    SetPrivate(bob, "swaySpeed", 1.1f);

                    // Parent NPC to visualRoot so its scale remains 1.0f (not scaled down with the boat)
                    // Place it at Z = -0.6f to align with the middle seat.
                    // Instantiate Paddling.fbx directly (which has the oar built-in)
                    GameObject npc = CreateNpcAvatar(
                        visualRoot.transform, 
                        "PaddlingNpc", 
                        new Vector3(0f, 0.15f, -0.6f), 
                        new Color(0.15f, 0.45f, 0.25f), 
                        false, 
                        "Assets/_Project/Animation/Character_1/Paddling.fbx"
                    );

                    if (npc != null)
                    {
                        Transform modelTrans = npc.transform.Find("NpcModel");
                        if (modelTrans != null)
                        {
                            npcAnimator = modelTrans.GetComponent<Animator>();
                        }
                    }
                }
            }
            else
            {
                GameObject hull = CreatePrimitiveChild(visualRoot.transform, "BoatHull", PrimitiveType.Cube, Vector3.zero, new Vector3(1.6f, 0.6f, 4.5f), new Color(0.5f, 0.35f, 0.2f));
                hull.GetComponent<Collider>().enabled = false;
                
                GameObject npc = CreateNpcAvatar(visualRoot.transform, "PaddlingNpc", new Vector3(0f, 0.4f, -0.3f), new Color(0.15f, 0.45f, 0.25f), false);
                if (npc != null)
                {
                    Transform modelTrans = npc.transform.Find("NpcModel");
                    if (modelTrans != null)
                    {
                        npcAnimator = modelTrans.GetComponent<Animator>();
                    }
                }
            }

            patrol.Configure(points, 1.8f, npcAnimator);
        }

        private static void BuildNpcBoat(Transform parent, string name, Vector3 position, Vector3 hullScale, Color hullColor, bool isLargeBoat)
        {
            GameObject boatRoot = new GameObject(name);
            boatRoot.transform.SetParent(parent);
            boatRoot.transform.position = position;
            boatRoot.transform.rotation = Quaternion.Euler(0f, isLargeBoat ? -20f : 35f, 0f);
            boatRoot.AddComponent<AmbientBob>();

            string modelPath = isLargeBoat
                ? "Assets/_Project/Art/model_mau/tauchokhach/tauchokhach (1).glb"
                : "Assets/_Project/Art/model_mau/taubanhang/hủ tiếu/hủ tiếu (1).glb";
            GameObject modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
                if (modelPrefab != null)
                {
                    GameObject model = PrefabUtility.InstantiatePrefab(modelPrefab) as GameObject;
                    if (model != null)
                    {
                        model.name = isLargeBoat ? "LargeBoatModel" : "VendorBoatModel";
                        model.transform.SetParent(boatRoot.transform, false);
                        model.transform.localPosition = Vector3.zero;
                        model.transform.localRotation = Quaternion.identity;
                        model.transform.localScale = Vector3.one * (isLargeBoat ? 1.6f : 1.25f);
                        ApplyBoatOrientationFix(model);
                        DisableColliders(model);
                        CleanImportedModel(model);
                    }
                }
            else
            {
                GameObject hull = CreatePrimitiveChild(boatRoot.transform, "Hull", PrimitiveType.Cube, new Vector3(0f, 0f, 0f), hullScale, hullColor);
                hull.GetComponent<Collider>().enabled = false;
            }

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

            GameObject npc = CreateNpcAvatar(boatRoot.transform, isLargeBoat ? "MerchantNpcOnBoat" : "VendorNpcOnBoat", new Vector3(0f, 1.35f, 0.9f), new Color(0.88f, 0.68f, 0.48f), !isLargeBoat);
            SimpleNpcWander wander = npc.AddComponent<SimpleNpcWander>();
            wander.Configure(new[] { new Vector3(-0.55f, 0f, -0.65f), new Vector3(0.55f, 0f, 0.65f) }, isLargeBoat ? 0.45f : 0.35f);
            AddTradeTarget(npc, isLargeBoat ? "Thuong Lai" : "Ghe Ban Hang", 3.1f);
        }

        private static void BuildRiverLife(Transform parent)
        {
            GameObject lifeRoot = new GameObject("RiverLife");
            lifeRoot.transform.SetParent(parent);

            CreateReedCluster(lifeRoot.transform, "LeftBankReeds", new Vector3(83f, 3.2f, 88f), 5);
            CreateReedCluster(lifeRoot.transform, "RightBankReeds", new Vector3(157f, 3.2f, 74f), 6);
            CreateMistBand(lifeRoot.transform, "MorningMist", new Vector3(121f, 4.6f, 128f), new Vector3(14f, 0.7f, 5f));
        }

        private static void BuildAmbientNpcCrowd(Transform parent, Terrain terrain)
        {
            GameObject npcRoot = new GameObject("AmbientNpcs");
            npcRoot.transform.SetParent(parent);

            CreateAmbientNpc(npcRoot.transform, terrain, "DockWorker_A", new Vector3(70f, 0f, 52f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(6f, 0f, 2f),
                new Vector3(3f, 0f, 7f),
                new Vector3(-2f, 0f, 4f)
            }, 1.15f, false);

            CreateAmbientNpc(npcRoot.transform, terrain, "DockWorker_B", new Vector3(88f, 0f, 60f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(5f, 0f, -1f),
                new Vector3(7f, 0f, 5f),
                new Vector3(1f, 0f, 6f)
            }, 1.05f, true);

            CreateAmbientNpc(npcRoot.transform, terrain, "Trader_A", new Vector3(104f, 0f, 72f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 3f),
                new Vector3(-3f, 0f, 5f),
                new Vector3(-5f, 0f, -2f)
            }, 1.10f, false);

            CreateAmbientNpc(npcRoot.transform, terrain, "Trader_B", new Vector3(132f, 0f, 72f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(4f, 0f, 4f),
                new Vector3(-4f, 0f, 6f),
                new Vector3(-2f, 0f, -3f)
            }, 1.08f, true);

            CreateAmbientNpc(npcRoot.transform, terrain, "Villager_A", new Vector3(154f, 0f, 66f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(6f, 0f, 3f),
                new Vector3(2f, 0f, 8f),
                new Vector3(-4f, 0f, 5f)
            }, 0.96f, false);

            CreateAmbientNpc(npcRoot.transform, terrain, "Villager_B", new Vector3(170f, 0f, 96f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(5f, 0f, 2f),
                new Vector3(2f, 0f, 7f),
                new Vector3(-3f, 0f, 4f)
            }, 0.92f, true);

            CreateAmbientNpc(npcRoot.transform, terrain, "Villager_C", new Vector3(58f, 0f, 110f), new[]
            {
                new Vector3(0f, 0f, 0f),
                new Vector3(7f, 0f, 2f),
                new Vector3(5f, 0f, 7f),
                new Vector3(-1f, 0f, 8f)
            }, 1.02f, false);
        }

        private static void CreateAmbientNpc(Transform parent, Terrain terrain, string name, Vector3 worldSeedPosition, Vector3[] localWaypoints, float speed, bool useModel2)
        {
            float y = terrain != null ? terrain.SampleHeight(worldSeedPosition) : worldSeedPosition.y;
            GameObject npc = CreateNpcAvatar(parent, name, new Vector3(worldSeedPosition.x, y, worldSeedPosition.z), new Color(0.72f, 0.42f, 0.32f), useModel2);
            SimpleNpcWander wander = npc.AddComponent<SimpleNpcWander>();
            wander.Configure(localWaypoints, speed);
            AddTradeTarget(npc, name.Replace("_", " "), 3f);
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

        internal static GameObject BuildBoat(Transform parent, BoatStats boatStats, Transform waterSurface)
        {
            GameObject boat = new GameObject("PlayerBoat");
            boat.transform.SetParent(parent);
            boat.transform.position = new Vector3(118f, 3.75f, 34f);
            boat.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
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
                    ApplyBoatOrientationFix(modelInstance);
                    CleanImportedModel(modelInstance);
                }
            }
            else
            {
                CreatePrimitiveChild(visualRoot.transform, "BoatHull", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.8f, 5.4f), new Color(0.49f, 0.31f, 0.18f));
            }

            return boat;
        }

        internal static GameObject BuildShorePlayer(Transform parent, GameObject boat)
        {
            GameObject player = new GameObject("PlayerOnFoot");
            player.transform.SetParent(parent);
            player.transform.position = new Vector3(92f, 4.25f, 25f);
            player.transform.rotation = Quaternion.Euler(0f, 80f, 0f);
            player.transform.localScale = Vector3.one;

            CharacterController controller = player.AddComponent<CharacterController>();
            controller.height = 9.0f;
            controller.radius = 1.75f;
            controller.center = new Vector3(0f, 4.5f, 0f);

            ShorePlayerController shoreController = player.AddComponent<ShorePlayerController>();
            player.AddComponent<PlayerNpcTradeInteractor>();

            GameObject visualRoot = new GameObject("PlayerVisualRoot");
            visualRoot.transform.SetParent(player.transform, false);
            visualRoot.transform.localScale = new Vector3(5f, 5f, 5f);

            GameObject playerModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NpcModel1Path);
            if (playerModelPrefab != null)
            {
                GameObject model = PrefabUtility.InstantiatePrefab(playerModelPrefab) as GameObject;
                if (model != null)
                {
                    model.name = "PlayerModel";
                    model.transform.SetParent(visualRoot.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    model.transform.localScale = Vector3.one;
                    DisableColliders(model);
                    CleanImportedModel(model);

                    // Assign Character_1 textures explicitly so the player model does not stay flat gray in URP.
                    var skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinnedRenderer != null)
                    {
                        Material sourceMat = skinnedRenderer.sharedMaterial;
                        if (sourceMat != null)
                        {
                            string texturePath = "Assets/_Project/Animation/Character_1/Textures/Image_0.jpg";
                            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                            if (texture != null)
                            {
                                Material instanceMat = new Material(sourceMat);
                                instanceMat.mainTexture = texture;
                                if (instanceMat.HasProperty("_BaseMap"))
                                {
                                    instanceMat.SetTexture("_BaseMap", texture);
                                }

                                string normalPath = "Assets/_Project/Animation/Character_1/Textures/Image_2.jpg";
                                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                                if (normalTex != null)
                                {
                                    instanceMat.SetTexture("_BumpMap", normalTex);
                                    instanceMat.EnableKeyword("_NORMALMAP");
                                }

                                skinnedRenderer.sharedMaterial = instanceMat;
                            }
                        }
                    }

                    // Setup Animator component
                    Animator animator = model.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = model.AddComponent<Animator>();
                    }
                    animator.applyRootMotion = false;
                    animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/_Project/Animation/Character_1/Character_1_Controller.controller");
                }
            }
            else
            {
                CreatePrimitiveChild(visualRoot.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.9f, 0f), new Vector3(0.55f, 0.9f, 0.55f), new Color(0.24f, 0.36f, 0.50f));
                CreatePrimitiveChild(visualRoot.transform, "Hat", PrimitiveType.Cylinder, new Vector3(0f, 1.85f, 0f), new Vector3(0.42f, 0.08f, 0.42f), new Color(0.82f, 0.68f, 0.36f));
            }

            GameObject shoreVillager = CreateNpcAvatar(parent, "ShoreVillagerNpc", new Vector3(78f, 4.25f, 38f), new Color(0.72f, 0.42f, 0.32f), true);
            shoreVillager.AddComponent<SimpleNpcWander>()
                .Configure(new[] { new Vector3(0f, 0f, 0f), new Vector3(8f, 0f, 3f), new Vector3(2f, 0f, 8f), new Vector3(-5f, 0f, 4f) }, 1.1f);
            AddTradeTarget(shoreVillager, "Dan Lang", 3.2f);

            Transform standPoint = CreateMarker(boat.transform, "PlayerStandPoint", new Vector3(0f, 1.35f, -0.4f), Quaternion.identity);
            
            GameObject dismountObj = new GameObject("DismountPoint");
            dismountObj.transform.SetParent(parent, false);
            dismountObj.transform.position = new Vector3(110f, 5.2f, 30f);
            dismountObj.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            Transform dismountPoint = dismountObj.transform;

            BoatBoardingController boarding = player.AddComponent<BoatBoardingController>();
            boarding.Configure(
                shoreController,
                visualRoot.transform,
                boat.transform,
                standPoint,
                dismountPoint,
                boat.GetComponent<BoatController>(),
                boat.GetComponent<PCBoatInput>(),
                Camera.main != null ? Camera.main.GetComponent<BoatFollowCamera>() : null);

            return player;
        }

        internal static void SetupBoardingFlow(GameObject player, GameObject boat, BoatFollowCamera followCamera)
        {
            if (player == null || boat == null)
                return;

            BoatBoardingController boarding = player.GetComponent<BoatBoardingController>();
            if (boarding == null)
                return;

            GameObject dismountObj = GameObject.Find("DismountPoint");
            Transform dismountPoint = dismountObj != null ? dismountObj.transform : null;

            boarding.Configure(
                player.GetComponent<ShorePlayerController>(),
                player.transform.Find("PlayerVisualRoot"),
                boat.transform,
                boat.transform.Find("PlayerStandPoint"),
                dismountPoint,
                boat.GetComponent<BoatController>(),
                boat.GetComponent<PCBoatInput>(),
                followCamera);
        }

        internal static void SetupBoatVisualModules(BoatCampManager boatCampManager, Transform boat)
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
            cam.transform.position = new Vector3(92f, 8.5f, 12f);
            cam.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
            cam.fieldOfView = 58f;
            cam.nearClipPlane = 0.1f;
        }

        private static BoatFollowCamera AttachFollowCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null || target == null) return null;
            BoatFollowCamera followCamera = cam.GetComponent<BoatFollowCamera>();
            if (followCamera == null)
            {
                followCamera = cam.gameObject.AddComponent<BoatFollowCamera>();
            }

            followCamera.Configure(target);
            return followCamera;
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

        private static GameObject CreateNpcAvatar(Transform parent, string name, Vector3 localPosition, Color tunicColor, bool useModel2 = false, string customModelPath = null)
        {
            GameObject npc = new GameObject(name);
            npc.transform.SetParent(parent, false);
            npc.transform.localPosition = localPosition;

            string path = string.IsNullOrEmpty(customModelPath) ? (useModel2 ? NpcModel2Path : NpcModel1Path) : customModelPath;
            GameObject npcModelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (npcModelPrefab != null)
            {
                GameObject model = PrefabUtility.InstantiatePrefab(npcModelPrefab) as GameObject;
                if (model != null)
                {
                    model.name = "NpcModel";
                    model.transform.SetParent(npc.transform, false);
                    model.transform.localPosition = Vector3.zero;
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    model.transform.localScale = Vector3.one; // Standard scale for human models from Animation folders
                    DisableColliders(model);
                    CleanImportedModel(model);

                    // Manually assign textures to ensure proper rendering in URP
                    var skinnedRenderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (skinnedRenderer != null)
                    {
                        Material sourceMat = skinnedRenderer.sharedMaterial;
                        if (sourceMat != null)
                        {
                            string texturePath = "Assets/_Project/Animation/Character_2/Textures/Image_0.jpg";
                            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                            if (texture != null)
                            {
                                Material instanceMat = new Material(sourceMat);
                                instanceMat.mainTexture = texture;
                                string normalPath = "Assets/_Project/Animation/Character_2/Textures/Image_2.jpg";
                                Texture2D normalTex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
                                if (normalTex != null)
                                {
                                    instanceMat.SetTexture("_BumpMap", normalTex);
                                    instanceMat.EnableKeyword("_NORMALMAP");
                                }
                                skinnedRenderer.sharedMaterial = instanceMat;
                            }
                        }
                    }

                    // Automatically apply wood color to any child oar/paddle meshes
                    var renderers = model.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        if (r.gameObject.name.ToLower().Contains("paddle") || r.gameObject.name.ToLower().Contains("object_22"))
                        {
                            ApplyColorMaterial(r, new Color(0.62f, 0.45f, 0.28f)); // Light wood oar
                        }
                    }

                    // Setup Animator component
                    Animator animator = model.GetComponent<Animator>();
                    if (animator == null)
                    {
                        animator = model.AddComponent<Animator>();
                    }
                    animator.applyRootMotion = false;

                    string controllerPath = useModel2 
                        ? "Assets/_Project/Animation/Character_2/Character_2_Controller.controller" 
                        : "Assets/_Project/Animation/Character_1/Character_1_Controller.controller";
                    RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);
                    if (controller != null)
                    {
                        animator.runtimeAnimatorController = controller;
                    }
                    else
                    {
                        Debug.LogWarning($"[RiverMarketSceneBuilder] AnimatorController not found at: {controllerPath}");
                    }

                    return npc;
                }
            }

            GameObject body = CreatePrimitiveChild(npc.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.8f, 0f), new Vector3(0.42f, 0.75f, 0.42f), tunicColor);
            GameObject head = CreatePrimitiveChild(npc.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0f), new Vector3(0.34f, 0.34f, 0.34f), new Color(0.88f, 0.68f, 0.48f));
            GameObject hat = CreatePrimitiveChild(npc.transform, "ConicalHat", PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0f), new Vector3(0.55f, 0.07f, 0.55f), new Color(0.84f, 0.72f, 0.44f));

            body.GetComponent<Collider>().enabled = false;
            head.GetComponent<Collider>().enabled = false;
            hat.GetComponent<Collider>().enabled = false;
            return npc;
        }

        private static void ConfigureFbxImporters(string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;
            string[] fbxFiles = Directory.GetFiles(folderPath, "*.fbx");
            foreach (string file in fbxFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(file).ToLower();
                if (fileName.Contains("walk") || fileName.Contains("paddling") || fileName.Contains("run") || fileName.Contains("move"))
                {
                    ModelImporter modelImporter = AssetImporter.GetAtPath(file) as ModelImporter;
                    if (modelImporter != null)
                    {
                        ModelImporterClipAnimation[] clips = modelImporter.clipAnimations;
                        if (clips == null || clips.Length == 0)
                        {
                            clips = modelImporter.defaultClipAnimations;
                        }

                        if (clips != null && clips.Length > 0)
                        {
                            bool modified = false;
                            for (int i = 0; i < clips.Length; i++)
                            {
                                clips[i].loopTime = true;
                                clips[i].keepOriginalOrientation = true;
                                clips[i].keepOriginalPositionXZ = true;
                                clips[i].keepOriginalPositionY = true;
                                clips[i].lockRootRotation = true;
                                clips[i].lockRootHeightY = true;
                                clips[i].lockRootPositionXZ = true;
                                modified = true;
                            }

                            if (modified)
                            {
                                modelImporter.clipAnimations = clips;
                                modelImporter.SaveAndReimport();
                            }
                        }
                    }
                }
            }
        }

        private static void ConfigureAnimations()
        {
            ConfigureFbxImporters("Assets/_Project/Animation/Character_1");
            ConfigureFbxImporters("Assets/_Project/Animation/Character_2");

            // Create Animator Controllers
            CreateFolderAnimatorController("Assets/_Project/Animation/Character_1", "Assets/_Project/Animation/Character_1/Character_1_Controller.controller");
            CreateFolderAnimatorController("Assets/_Project/Animation/Character_2", "Assets/_Project/Animation/Character_2/Character_2_Controller.controller");
            CreateBoatAnimatorController(PaddlingBoatControllerPath, PaddlingBoatModelPath);

            AssetDatabase.Refresh();
        }

        private static void CreateFolderAnimatorController(string folderPath, string savePath)
        {
            if (!Directory.Exists(folderPath)) return;

            // Delete old controller to avoid duplication/issues
            if (File.Exists(savePath))
            {
                AssetDatabase.DeleteAsset(savePath);
            }

            var controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);
            var stateMachine = controller.layers[0].stateMachine;

            string[] fbxFiles = Directory.GetFiles(folderPath, "*.fbx");
            foreach (string file in fbxFiles)
            {
                AnimationClip clip = GetAnimationClipFromFBX(file);
                if (clip != null)
                {
                    string stateName = Path.GetFileNameWithoutExtension(file);
                    if (stateName.ToLower() == "user") continue; // Skip the user model file itself

                    var state = stateMachine.AddState(stateName);
                    state.motion = clip;

                    if (stateName.ToLower() == "paddling")
                    {
                        state.speed = 2.0f; // Speed up paddling animation to match oar frequency
                    }

                    if (stateName.ToLower().Contains("neutral idle") || stateName.ToLower().Contains("idle"))
                    {
                        stateMachine.defaultState = state;
                    }
                }
            }
        }

        private static AnimationClip GetAnimationClipFromFBX(string path)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    return clip;
                }
            }
            return null;
        }

        private static Transform CreateMarker(Transform parent, string name, Vector3 localPosition, Quaternion localRotation)
        {
            GameObject marker = new GameObject(name);
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localRotation = localRotation;
            return marker.transform;
        }

        private static void DisableColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
                collider.enabled = false;
        }

        private static void CleanImportedModel(GameObject model)
        {
            if (model == null) return;

            // Unpack prefab instance to allow structural changes (deleting child game objects)
            if (PrefabUtility.IsPartOfPrefabInstance(model))
            {
                PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }

            // Find and destroy all cameras in the imported model
            Camera[] cameras = model.GetComponentsInChildren<Camera>(true);
            foreach (var cam in cameras)
            {
                if (cam.gameObject != Camera.main?.gameObject)
                {
                    Object.DestroyImmediate(cam.gameObject);
                }
            }

            // Find and destroy all lights in the imported model
            Light[] lights = model.GetComponentsInChildren<Light>(true);
            foreach (var light in lights)
            {
                Object.DestroyImmediate(light.gameObject);
            }

            // Find and destroy all AudioListeners in the imported model
            AudioListener[] listeners = model.GetComponentsInChildren<AudioListener>(true);
            foreach (var listener in listeners)
            {
                Object.DestroyImmediate(listener);
            }
        }

        private static void AddTradeTarget(GameObject npcRoot, string displayName, float radius)
        {
            if (npcRoot == null)
                return;

            NpcTradeTarget target = npcRoot.GetComponent<NpcTradeTarget>();
            if (target == null)
                target = npcRoot.AddComponent<NpcTradeTarget>();

            target.Configure(displayName, radius);
        }

        private static void ApplyTreeOrientationFix(GameObject treeInstance, string assetPath)
        {
            if (treeInstance == null)
                return;

            Quaternion correction = Quaternion.identity;
            if (assetPath.Contains("palm_trees") || assetPath.Contains("mango_tree") || assetPath.Contains("tree_elm") || assetPath.Contains("small_trees"))
                correction = Quaternion.Euler(-90f, 0f, 0f);

            treeInstance.transform.rotation = treeInstance.transform.rotation * correction;
        }

        private static void ApplyBoatOrientationFix(GameObject boatInstance)
        {
            if (boatInstance == null)
                return;
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

        private static void ApplyBoatMaterials(GameObject boatInstance)
        {
            if (boatInstance == null) return;
            
            var renderers = boatInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                Material[] materials = r.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    
                    Material mat = new Material(materials[i]);
                    string matName = materials[i].name.ToLower();
                    
                    if (matName.Contains("paddle"))
                    {
                        mat.color = new Color(0.62f, 0.45f, 0.28f); // Light wood oar
                    }
                    else if (matName.Contains("metal"))
                    {
                        mat.color = new Color(0.3f, 0.3f, 0.3f); // Metal ring
                        mat.SetFloat("_Metallic", 0.8f);
                        mat.SetFloat("_Smoothness", 0.6f);
                    }
                    else if (matName.Contains("rail"))
                    {
                        mat.color = new Color(0.28f, 0.16f, 0.08f); // Dark wood trim
                    }
                    else
                    {
                        mat.color = new Color(0.48f, 0.31f, 0.18f); // Standard wood hull
                    }
                    
                    materials[i] = mat;
                }
                r.sharedMaterials = materials;
            }
        }

        private static void CreateBoatAnimatorController(string savePath, string fbxPath)
        {
            if (File.Exists(savePath))
            {
                AssetDatabase.DeleteAsset(savePath);
            }

            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(savePath);
            
            AnimationClip leftClip = null;
            AnimationClip rightClip = null;
            AnimationClip trackedClip = null;

            var subAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var sub in subAssets)
            {
                if (sub is AnimationClip)
                {
                    if (sub.name == "Bone.001_9|row_forward") leftClip = sub as AnimationClip;
                    else if (sub.name == "Bone.002_11|row_forward") rightClip = sub as AnimationClip;
                    else if (sub.name == "tracked_5|row_forward") trackedClip = sub as AnimationClip;
                }
            }

            if (leftClip == null || rightClip == null || trackedClip == null)
            {
                Debug.LogWarning("[RiverMarketSceneBuilder] Failed to find some row_forward clips in boat FBX.");
                return;
            }

            // Layer 0: Left Paddle
            controller.layers[0].name = "LeftPaddle";
            var leftSM = controller.layers[0].stateMachine;
            var leftState = leftSM.AddState("RowLeft");
            leftState.motion = leftClip;
            leftState.speed = 0.68f; // Slow down oar animation to match character paddling cycle (3.6s total)
            leftSM.defaultState = leftState;

            // Layer 1: Right Paddle
            controller.AddLayer("RightPaddle");
            var layers = controller.layers;
            layers[1].defaultWeight = 1f;
            var rightSM = layers[1].stateMachine;
            var rightState = rightSM.AddState("RowRight");
            rightState.motion = rightClip;
            rightState.speed = 0.68f; // Slow down oar animation to match character paddling cycle (3.6s total)
            rightSM.defaultState = rightState;

            // Layer 2: Tracked
            controller.AddLayer("Tracked");
            layers = controller.layers;
            layers[2].defaultWeight = 1f;
            var trackedSM = layers[2].stateMachine;
            var trackedState = trackedSM.AddState("RowTracked");
            trackedState.motion = trackedClip;
            trackedState.speed = 0.68f; // Slow down oar animation to match character paddling cycle (3.6s total)
            trackedSM.defaultState = trackedState;

            controller.layers = layers;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
