using System.Collections.Generic;
using UnityEngine;
using ChoNoi.Domain;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Presentation
{
    [System.Serializable]
    public class CargoSlot
    {
        public ItemData item;
        public int amount;
    }

    public class InventoryManager : MonoBehaviour, IWeightProvider
    {
        public const int StackLimit = 10;

        [Header("Inventory Status")]
        [SerializeField] private float maxWeightCapacity = 100f; // Sức chứa mặc định ban đầu
        [SerializeField] private float currentTotalWeight = 0f;

        [Header("Cargo Grid Slots")]
        [SerializeField] private List<CargoSlot> cargoSlots = new List<CargoSlot>();

        // Quản lý số lượng vật phẩm trong khoang (giữ đồng bộ để tương thích ngược)
        private Dictionary<ItemData, int> inventory = new Dictionary<ItemData, int>();

        public float MaxWeightCapacity => maxWeightCapacity;
        public float CurrentTotalWeight => currentTotalWeight;
        public Dictionary<ItemData, int> Inventory => inventory;

        public int MaxSlots
        {
            get
            {
                if (maxWeightCapacity <= 100f) return 12; // 3x4 Grid
                if (maxWeightCapacity <= 150f) return 16; // 4x4 Grid
                return 20;                                // 4x5 Grid
            }
        }

        public List<CargoSlot> CargoSlots
        {
            get
            {
                EnsureSlotsSize();
                return cargoSlots;
            }
        }

        private void Awake()
        {
            EnsureSlotsSize();
            SyncInventoryFromSlots();
        }

        public void EnsureSlotsSize()
        {
            int target = MaxSlots;
            while (cargoSlots.Count < target)
            {
                cargoSlots.Add(new CargoSlot { item = null, amount = 0 });
            }
        }

        public void SyncInventoryFromSlots()
        {
            inventory.Clear();
            currentTotalWeight = 0f;
            foreach (var slot in cargoSlots)
            {
                if (slot != null && slot.item != null && slot.amount > 0)
                {
                    if (inventory.ContainsKey(slot.item))
                    {
                        inventory[slot.item] += slot.amount;
                    }
                    else
                    {
                        inventory.Add(slot.item, slot.amount);
                    }
                    currentTotalWeight += slot.item.weight * slot.amount;
                }
            }
        }

        public bool CanFitItem(ItemData item, int amount)
        {
            EnsureSlotsSize();
            int remaining = amount;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (cargoSlots[i].item == item && cargoSlots[i].amount < StackLimit)
                {
                    remaining -= (StackLimit - cargoSlots[i].amount);
                    if (remaining <= 0) return true;
                }
            }
            for (int i = 0; i < MaxSlots; i++)
            {
                if (cargoSlots[i].item == null || cargoSlots[i].amount <= 0)
                {
                    remaining -= StackLimit;
                    if (remaining <= 0) return true;
                }
            }
            return false;
        }

        public bool AddItemToSlots(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;
            EnsureSlotsSize();

            int remaining = amount;
            // 1. Stack trong các ô đã có cùng loại item và chưa đầy
            for (int i = 0; i < MaxSlots; i++)
            {
                if (cargoSlots[i].item == item && cargoSlots[i].amount < StackLimit)
                {
                    int addQty = Mathf.Min(remaining, StackLimit - cargoSlots[i].amount);
                    cargoSlots[i].amount += addQty;
                    remaining -= addQty;
                    if (remaining <= 0) break;
                }
            }

            // 2. Đặt vào các ô trống mới
            if (remaining > 0)
            {
                for (int i = 0; i < MaxSlots; i++)
                {
                    if (cargoSlots[i].item == null || cargoSlots[i].amount <= 0)
                    {
                        cargoSlots[i].item = item;
                        int addQty = Mathf.Min(remaining, StackLimit);
                        cargoSlots[i].amount = addQty;
                        remaining -= addQty;
                        if (remaining <= 0) break;
                    }
                }
            }

            return remaining == 0;
        }

        public bool RemoveItemFromSlots(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;
            EnsureSlotsSize();

            // Kiểm tra tổng số lượng có đủ không
            int total = 0;
            for (int i = 0; i < MaxSlots; i++)
            {
                if (cargoSlots[i].item == item)
                {
                    total += cargoSlots[i].amount;
                }
            }
            if (total < amount) return false;

            int remaining = amount;
            // Trừ dần từ các ô chứa item
            for (int i = MaxSlots - 1; i >= 0; i--)
            {
                if (cargoSlots[i].item == item)
                {
                    if (cargoSlots[i].amount <= remaining)
                    {
                        remaining -= cargoSlots[i].amount;
                        cargoSlots[i].item = null;
                        cargoSlots[i].amount = 0;
                    }
                    else
                    {
                        cargoSlots[i].amount -= remaining;
                        remaining = 0;
                        break;
                    }
                }
            }

            return remaining == 0;
        }

        public void SwapSlots(int indexA, int indexB)
        {
            EnsureSlotsSize();
            if (indexA < 0 || indexA >= MaxSlots || indexB < 0 || indexB >= MaxSlots) return;

            CargoSlot temp = cargoSlots[indexA];
            cargoSlots[indexA] = cargoSlots[indexB];
            cargoSlots[indexB] = temp;

            SyncInventoryFromSlots();
            Debug.Log($"[InventoryManager] Đã hoán đổi vị trí ô {indexA} và {indexB}.");
        }

        public float GetCurrentWeightRatio()
        {
            if (maxWeightCapacity <= 0f) return 0f;
            return Mathf.Clamp01(currentTotalWeight / maxWeightCapacity);
        }

        public void UpgradeCapacity(float additionalWeight)
        {
            maxWeightCapacity += additionalWeight;
            EnsureSlotsSize();
            SyncInventoryFromSlots();
            Debug.Log($"[InventoryManager] Đã nâng cấp sức chứa ghe. Sức chứa mới: {maxWeightCapacity} kg, số ô mới: {MaxSlots}");
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

            // Kiểm tra xem các ô chứa có xếp vừa không
            if (!CanFitItem(item, amount))
            {
                Debug.LogWarning($"[InventoryManager] KHÔNG THỂ MUA: Không đủ ô chứa trống hoặc đầy dung lượng ô (Stack Limit = {StackLimit})!");
                return false;
            }

            AddItemToSlots(item, amount);
            SyncInventoryFromSlots();
            
            Debug.Log($"[InventoryManager] ĐÃ MUA THÀNH CÔNG: {amount}x {item.itemName}. Khối lượng hiện tại: {currentTotalWeight}/{maxWeightCapacity} kg.");
            return true;
        }

        public bool SellItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return false;

            if (RemoveItemFromSlots(item, amount))
            {
                SyncInventoryFromSlots();
                Debug.Log($"[InventoryManager] ĐÃ BÁN THÀNH CÔNG: {amount}x {item.itemName}. Khối lượng hiện tại: {currentTotalWeight}/{maxWeightCapacity} kg.");
                return true;
            }
            else
            {
                Debug.LogWarning($"[InventoryManager] LỖI BÁN HÀNG: Không đủ {item.itemName} trong ô chứa để bán!");
                return false;
            }
        }

        public void ClearInventory()
        {
            cargoSlots.Clear();
            EnsureSlotsSize();
            SyncInventoryFromSlots();
        }

        public void SetItemAmount(ItemData item, int amount)
        {
            if (item == null || amount <= 0) return;
            EnsureSlotsSize();

            // Xóa tất cả vị trí cũ của item này trước
            for (int i = 0; i < MaxSlots; i++)
            {
                if (cargoSlots[i].item == item)
                {
                    cargoSlots[i].item = null;
                    cargoSlots[i].amount = 0;
                }
            }

            AddItemToSlots(item, amount);
            SyncInventoryFromSlots();
        }

        public void LoadCapacity(float capacity)
        {
            maxWeightCapacity = capacity;
            EnsureSlotsSize();
            SyncInventoryFromSlots();
        }
    }
}
