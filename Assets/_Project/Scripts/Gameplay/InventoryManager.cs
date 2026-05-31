using System.Collections.Generic;
using UnityEngine;

namespace ChoNoiMienTay.Gameplay
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Inventory Status")]
        [SerializeField] private float maxWeightCapacity = 100f; // Sức chứa mặc định ban đầu
        [SerializeField] private float currentTotalWeight = 0f;

        // Quản lý số lượng vật phẩm trong khoang
        private Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();

        public float MaxWeightCapacity => maxWeightCapacity;
        public float CurrentTotalWeight => currentTotalWeight;
        public Dictionary<ItemData, int> Inventory => inventory;

        /// <summary>
        /// Hàm này sẽ được gọi bởi Hệ thống Nâng cấp ghe sau này.
        /// Người chơi không tự set, mà gọi hàm này khi build thêm module ghe.
        /// </summary>
        public void UpgradeCapacity(float additionalWeight)
        {
            maxWeightCapacity += additionalWeight;
            Debug.Log($"[InventoryManager] Đã nâng cấp sức chứa ghe. Sức chứa mới: {maxWeightCapacity} kg");
        }

        public bool BuyItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            float weightToAdd = item.weight * amount;
            if (currentTotalWeight + weightToAdd > maxWeightCapacity)
            {
                Debug.LogWarning($"[InventoryManager] KHÔNG THỂ MUA: Vượt quá sức chứa của ghe! (Cần thêm {weightToAdd} kg, chỉ còn trống {maxWeightCapacity - currentTotalWeight} kg)");
                return false;
            }

            if (inventory.ContainsKey(item))
            {
                inventory[item] += amount;
            }
            else
            {
                inventory.Add(item, amount);
            }

            currentTotalWeight += weightToAdd;
            
            // In log ra Console như yêu cầu
            Debug.Log($"[InventoryManager] ĐÃ MUA THÀNH CÔNG: {amount}x {item.itemName}. Khối lượng hiện tại: {currentTotalWeight}/{maxWeightCapacity} kg.");
            return true;
        }

        public bool SellItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            if (inventory.ContainsKey(item) && inventory[item] >= amount)
            {
                inventory[item] -= amount;
                float weightToRemove = item.weight * amount;
                currentTotalWeight -= weightToRemove;

                if (inventory[item] == 0)
                {
                    inventory.Remove(item);
                }

                // In log ra Console như yêu cầu
                Debug.Log($"[InventoryManager] ĐÃ BÁN THÀNH CÔNG: {amount}x {item.itemName}. Khối lượng hiện tại: {currentTotalWeight}/{maxWeightCapacity} kg.");
                return true;
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] LỖI BÁN HÀNG: Không đủ {item.itemName} trong kho để bán!");
                return false;
            }
        }
    }
}
