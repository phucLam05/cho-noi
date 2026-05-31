using UnityEngine;
using ChoNoiMienTay.Gameplay;

namespace ChoNoiMienTay.Core
{
    public class EconomyManager : MonoBehaviour
    {
        [Header("References")]
        public PlayerStats playerStats;
        
        private void Awake()
        {
            if (playerStats == null)
            {
                playerStats = FindObjectOfType<PlayerStats>();
            }
        }

        /// <summary>
        /// Mua một dịch vụ (Bún riêu, sửa ghe...)
        /// </summary>
        public bool BuyService(ServiceData service)
        {
            if (service == null || playerStats == null) return false;

            if (playerStats.DeductMoney(service.costMoney))
            {
                if (service.staminaRestoreAmount > 0)
                {
                    playerStats.RestoreStamina(service.staminaRestoreAmount);
                }
                
                if (service.durabilityRestoreAmount > 0)
                {
                    // Tương lai: gọi BoatStats.RestoreDurability()
                    Debug.Log($"[EconomyManager] Đã phục hồi {service.durabilityRestoreAmount} độ bền cho Ghe.");
                }

                Debug.Log($"[EconomyManager] Giao dịch Dịch vụ thành công: {service.serviceName}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mua hàng hóa bình thường (VD: Mua nông sản từ nhà vườn)
        /// </summary>
        public bool BuyItemToInventory(ItemData item, int amount, InventoryManager inventory)
        {
            if (item == null || inventory == null || playerStats == null || amount <= 0) return false;

            int totalCost = item.basePrice * amount;

            // Kiểm tra ví tiền trước
            if (playerStats.CurrentMoney >= totalCost)
            {
                // Thử thêm vào kho (kiểm tra sức chứa)
                if (inventory.BuyItem(item, amount)) 
                {
                    // Thêm thành công thì mới trừ tiền
                    playerStats.DeductMoney(totalCost);
                    Debug.Log($"[EconomyManager] Đã mua {amount}x {item.itemName} với giá {totalCost:N0} VNĐ");
                    return true;
                }
            }
            else
            {
                Debug.LogWarning($"[EconomyManager] Không đủ tiền mua {amount}x {item.itemName}. Cần {totalCost:N0}");
            }
            return false;
        }

        /// <summary>
        /// Bán hàng hóa sau khi chốt đơn (Thường được gọi từ HagglingSystem)
        /// </summary>
        public bool SellItemWholesale(ItemData item, int amount, InventoryManager inventory, int finalTotalRevenue)
        {
            if (item == null || inventory == null || playerStats == null || amount <= 0) return false;

            // Thử trừ đồ trong kho
            if (inventory.SellItem(item, amount))
            {
                // Trừ đồ thành công thì cộng tiền chốt đơn
                playerStats.AddMoney(finalTotalRevenue);
                Debug.Log($"[EconomyManager] Đã CHỐT ĐƠN BÁN {amount}x {item.itemName} thu về {finalTotalRevenue:N0} VNĐ");
                return true;
            }
            return false;
        }
    }
}
