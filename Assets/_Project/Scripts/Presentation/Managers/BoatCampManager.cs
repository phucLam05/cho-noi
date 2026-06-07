using UnityEngine;

namespace ChoNoiMienTay.Presentation
{
    public class BoatCampManager : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private ChoNoi.Presentation.BoatController boatController;
        
        [Header("Visuals")]
        [SerializeField] private GameObject roofObject;
        [SerializeField] private GameObject bambooPoleObject;

        [Header("Saved States")]
        public bool hasRoof = false;
        public int bambooPoleLevel = 0;
        public int engineLevel = 0;

        public void BuyStorageUpgrade(int cost, float capacityBonus)
        {
            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                inventoryManager.UpgradeCapacity(capacityBonus);
                Debug.Log($"[BoatCamp] Đã nâng cấp khoang chứa thêm {capacityBonus}kg!");
            }
        }

        public void BuyEngineUpgrade(int cost, float thrustMultiplier)
        {
            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                boatController.EngineThrustMultiplier = thrustMultiplier;
                engineLevel++;
                Debug.Log($"[BoatCamp] Đã nâng cấp động cơ cấp {engineLevel} (Multiplier: {thrustMultiplier})!");
            }
        }

        public void BuyRoofUpgrade(int cost)
        {
            if (hasRoof) return;

            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                hasRoof = true;
                if (roofObject != null) roofObject.SetActive(true);
                Debug.Log("[BoatCamp] Đã lợp mái che thành công!");
            }
        }

        public void BuyBambooPoleUpgrade(int cost, float bonusRatio)
        {
            if (playerStats.CurrentMoney >= cost && playerStats.DeductMoney(cost))
            {
                bambooPoleLevel++;
                playerStats.UpgradeHagglingBonus(bonusRatio);
                if (bambooPoleObject != null && !bambooPoleObject.activeSelf) 
                {
                    bambooPoleObject.SetActive(true);
                }
                Debug.Log($"[BoatCamp] Đã nâng cấp Cây Bẹo cấp {bambooPoleLevel}!");
            }
        }

        public void LoadData(bool savedHasRoof, int savedBambooLevel, int savedEngineLevel, float savedThrust)
        {
            hasRoof = savedHasRoof;
            bambooPoleLevel = savedBambooLevel;
            engineLevel = savedEngineLevel;

            if (roofObject != null) roofObject.SetActive(hasRoof);
            if (bambooPoleLevel > 0 && bambooPoleObject != null) bambooPoleObject.SetActive(true);
            if (boatController != null) boatController.EngineThrustMultiplier = savedThrust;
        }
    }
}
