#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using ChoNoiMienTay.Data;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.UI;

namespace ChoNoiMienTay.Editor
{
    public static class BargainingPrototypeBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Sandbox/BargainingPrototype.unity";
        private const string ConfigPath = "Assets/_Project/ScriptableObjects/Bargaining/BargainingEconomyConfig.asset";
        private const string ItemFolderPath = "Assets/_Project/ScriptableObjects/Bargaining/Items";
        private const string MerchantAvatarPath = "Assets/_Project/Art/Avatars/merchant.png";
        private const string VillagerAvatarPath = "Assets/_Project/Art/Avatars/villager.png";

        [MenuItem("ChoNoi/Scenes/Build Bargaining Prototype")]
        public static void BuildPrototype()
        {
            EnsureFolders();

            BargainingEconomyConfig config = EnsureEconomyConfig();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.backgroundColor = new Color(0.10f, 0.16f, 0.18f);
                mainCamera.clearFlags = CameraClearFlags.SolidColor;
            }

            GameObject prototypeRoot = new GameObject("BargainingPrototype");
            BargainingPrototypeBootstrap bootstrap = prototypeRoot.AddComponent<BargainingPrototypeBootstrap>();
            bootstrap.SetEconomyConfig(config);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = prototypeRoot;
            Debug.Log($"[BargainingPrototypeBuilder] Prototype scene created at {ScenePath}");
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory("Assets/_Project/Scenes/Sandbox");
            Directory.CreateDirectory("Assets/_Project/ScriptableObjects/Bargaining");
            Directory.CreateDirectory(ItemFolderPath);
        }

        private static BargainingEconomyConfig EnsureEconomyConfig()
        {
            BargainingEconomyConfig config = AssetDatabase.LoadAssetAtPath<BargainingEconomyConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BargainingEconomyConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            Sprite merchantAvatar = EnsureSpriteAvatar(MerchantAvatarPath);
            Sprite villagerAvatar = EnsureSpriteAvatar(VillagerAvatarPath);

            List<BargainingItemEconomyEntry> itemEntries = new List<BargainingItemEconomyEntry>
            {
                new BargainingItemEconomyEntry
                {
                    item = EnsureItem("ITM_KHOM", "Khóm", 15000, 5f),
                    minPriceVariation = -2000,
                    maxPriceVariation = 4000,
                    startingInventoryAmount = 3
                },
                new BargainingItemEconomyEntry
                {
                    item = EnsureItem("ITM_BIDAO", "Bí Đao", 18000, 6f),
                    minPriceVariation = -2500,
                    maxPriceVariation = 4500,
                    startingInventoryAmount = 3
                },
                new BargainingItemEconomyEntry
                {
                    item = EnsureItem("ITM_XOAI", "Xoài", 22000, 4f),
                    minPriceVariation = -3000,
                    maxPriceVariation = 5500,
                    startingInventoryAmount = 2
                },
                new BargainingItemEconomyEntry
                {
                    item = EnsureItem("ITM_DUAHAU", "Dưa Hấu", 26000, 8f),
                    minPriceVariation = -3500,
                    maxPriceVariation = 6500,
                    startingInventoryAmount = 2
                },
                new BargainingItemEconomyEntry
                {
                    item = EnsureItem("ITM_CAM", "Cam", 20000, 3f),
                    minPriceVariation = -1500,
                    maxPriceVariation = 3500,
                    startingInventoryAmount = 4
                }
            };

            List<BargainingNpcProfile> npcProfiles = new List<BargainingNpcProfile>
            {
                new BargainingNpcProfile
                {
                    npcId = "merchant",
                    displayName = "Merchant NPC",
                    avatar = merchantAvatar,
                    openingPriceMultiplier = 0.92f,
                    maxAcceptPriceMultiplier = 1.10f
                },
                new BargainingNpcProfile
                {
                    npcId = "villager",
                    displayName = "Villager NPC",
                    avatar = villagerAvatar,
                    openingPriceMultiplier = 0.85f,
                    maxAcceptPriceMultiplier = 1.02f
                }
            };

            config.ReplacePrototypeData(8, 500, itemEntries, npcProfiles);
            EditorUtility.SetDirty(config);
            return config;
        }

        private static ItemData EnsureItem(string itemId, string itemName, int basePrice, float weight)
        {
            string assetPath = $"{ItemFolderPath}/{itemId}.asset";
            ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(assetPath);
            if (item == null)
            {
                item = ScriptableObject.CreateInstance<ItemData>();
                AssetDatabase.CreateAsset(item, assetPath);
            }

            item.itemID = itemId;
            item.itemName = itemName;
            item.basePrice = basePrice;
            item.weight = weight;
            EditorUtility.SetDirty(item);
            return item;
        }

        private static Sprite EnsureSpriteAvatar(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
#endif
