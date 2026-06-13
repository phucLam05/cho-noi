using UnityEngine;
using System.Collections.Generic;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Presentation
{
    public class BoatCampManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private ChoNoi.Presentation.BoatController boatController;
        [SerializeField] private BoatUpgradeCatalogSO upgradeCatalog;
        
        [Header("Visuals")]
        [SerializeField] private GameObject storageModuleObject;
        [SerializeField] private GameObject roofObject;
        [SerializeField] private GameObject bambooPoleObject;
        [SerializeField] private List<GameObject> bambooPoleLevelObjects = new List<GameObject>();
        [SerializeField] private List<GameObject> engineLevelObjects = new List<GameObject>();

        [Header("Saved States")]
        public int storageLevel = 0;
        public bool hasRoof = false;
        public int bambooPoleLevel = 0;
        public int engineLevel = 0;

        public int StorageLevel => storageLevel;
        public BoatUpgradeCatalogSO UpgradeCatalog => upgradeCatalog;

        private void Awake()
        {
            if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
            if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
            if (boatController == null) boatController = FindAnyObjectByType<ChoNoi.Presentation.BoatController>();
            ApplyVisualState();
            ApplyEngineTierStats();
        }

        public bool BuyStorageUpgrade(int cost, float capacityBonus)
        {
            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                storageLevel++;
                inventoryManager.UpgradeCapacity(capacityBonus);
                ApplyVisualState();
                Debug.Log($"[BoatCamp] Đã nâng cấp khoang chứa thêm {capacityBonus}kg!");
                return true;
            }

            return false;
        }

        public bool BuyEngineUpgrade(int cost, float thrustMultiplier)
        {
            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                engineLevel++;
                if (boatController != null)
                {
                    boatController.EngineThrustMultiplier = thrustMultiplier;
                }
                ApplyVisualState();
                Debug.Log($"[BoatCamp] Đã nâng cấp động cơ cấp {engineLevel} (Multiplier: {thrustMultiplier})!");
                return true;
            }

            return false;
        }

        public bool BuyRoofUpgrade(int cost)
        {
            if (hasRoof) return false;

            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                hasRoof = true;
                ApplyVisualState();
                Debug.Log("[BoatCamp] Đã lợp mái che thành công!");
                return true;
            }

            return false;
        }

        public bool BuyBambooPoleUpgrade(int cost, float bonusRatio)
        {
            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                bambooPoleLevel++;
                playerStats.UpgradeHagglingBonus(bonusRatio);
                ApplyVisualState();
                Debug.Log($"[BoatCamp] Đã nâng cấp Cây Bẹo cấp {bambooPoleLevel}!");
                return true;
            }

            return false;
        }

        public bool TryBuyNextStorageUpgrade()
        {
            StorageUpgradeTier tier = upgradeCatalog != null ? upgradeCatalog.GetStorageTier(storageLevel) : null;
            if (tier == null) return false;
            return BuyStorageUpgrade(tier.costMoney, tier.capacityBonus);
        }

        public bool TryBuyNextEngineUpgrade()
        {
            EngineUpgradeTier tier = upgradeCatalog != null ? upgradeCatalog.GetEngineTier(engineLevel) : null;
            if (tier == null) return false;
            return BuyEngineUpgrade(tier.costMoney, tier.thrustMultiplier);
        }

        public bool TryBuyRoofUpgrade()
        {
            int roofLevel = hasRoof ? 1 : 0;
            RoofUpgradeTier tier = upgradeCatalog != null ? upgradeCatalog.GetRoofTier(roofLevel) : null;
            if (tier == null || hasRoof) return false;
            return BuyRoofUpgrade(tier.costMoney);
        }

        public bool TryBuyNextBambooPoleUpgrade()
        {
            BambooPoleUpgradeTier tier = upgradeCatalog != null ? upgradeCatalog.GetBambooPoleTier(bambooPoleLevel) : null;
            if (tier == null) return false;
            return BuyBambooPoleUpgrade(tier.costMoney, tier.hagglingBonusRatio);
        }

        public void LoadData(int savedStorageLevel, bool savedHasRoof, int savedBambooLevel, int savedEngineLevel, float savedThrust)
        {
            storageLevel = Mathf.Max(0, savedStorageLevel);
            hasRoof = savedHasRoof;
            bambooPoleLevel = savedBambooLevel;
            engineLevel = savedEngineLevel;
            if (boatController != null) boatController.EngineThrustMultiplier = savedThrust;
            ApplyVisualState();
        }

        private void ApplyVisualState()
        {
            if (storageModuleObject != null)
            {
                storageModuleObject.SetActive(storageLevel > 0);
            }

            if (roofObject != null)
            {
                roofObject.SetActive(hasRoof);
            }

            if (bambooPoleObject != null)
            {
                bambooPoleObject.SetActive(bambooPoleLevel > 0);
            }

            for (int index = 0; index < bambooPoleLevelObjects.Count; index++)
            {
                if (bambooPoleLevelObjects[index] != null)
                {
                    bambooPoleLevelObjects[index].SetActive(index < bambooPoleLevel);
                }
            }

            for (int index = 0; index < engineLevelObjects.Count; index++)
            {
                if (engineLevelObjects[index] != null)
                {
                    engineLevelObjects[index].SetActive(index == Mathf.Clamp(engineLevel - 1, -1, engineLevelObjects.Count - 1));
                }
            }
        }

        private void ApplyEngineTierStats()
        {
            if (boatController == null || upgradeCatalog == null || engineLevel <= 0)
            {
                return;
            }

            EngineUpgradeTier currentTier = upgradeCatalog.GetEngineTier(engineLevel - 1);
            if (currentTier != null)
            {
                boatController.EngineThrustMultiplier = currentTier.thrustMultiplier;
            }
        }
    }
}
