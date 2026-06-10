using UnityEngine;
using ChoNoiMienTay.Data;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.Systems;

namespace ChoNoiMienTay.UI
{
    public class BargainingPrototypeBootstrap : MonoBehaviour
    {
        [SerializeField] private BargainingEconomyConfig economyConfig;
        [SerializeField] private bool seedInventoryOnStart = true;

        private void Awake()
        {
            PlayerStats playerStats = GetOrAddComponent<PlayerStats>();
            InventoryManager inventoryManager = GetOrAddComponent<InventoryManager>();
            EconomyManager economyManager = GetOrAddComponent<EconomyManager>();
            BargainingSystem bargainingSystem = GetOrAddComponent<BargainingSystem>();
            BargainingPrototypeUI prototypeUI = GetOrAddComponent<BargainingPrototypeUI>();

            economyManager.playerStats = playerStats;
            bargainingSystem.Configure(economyConfig, playerStats, inventoryManager, economyManager);
            prototypeUI.Configure(bargainingSystem, playerStats, inventoryManager);

            if (seedInventoryOnStart && economyConfig != null && inventoryManager.Inventory.Count == 0)
            {
                foreach (BargainingItemEconomyEntry entry in economyConfig.AgriculturalItems)
                {
                    if (entry != null && entry.item != null && entry.startingInventoryAmount > 0)
                    {
                        inventoryManager.BuyItem(entry.item, entry.startingInventoryAmount);
                    }
                }
            }
        }

        public void SetEconomyConfig(BargainingEconomyConfig config)
        {
            economyConfig = config;
        }

        private T GetOrAddComponent<T>() where T : Component
        {
            T component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }

            return component;
        }
    }
}
