using System.Collections.Generic;
using UnityEngine;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Presentation
{
    public class BambooPoleManager : MonoBehaviour
    {
        [SerializeField] private BoatCampManager boatCampManager;
        [SerializeField] private InventoryManager inventoryManager;

        private List<ItemData> displayedItems = new List<ItemData>();
        
        public List<ItemData> DisplayedItems => displayedItems;

        public int MaxDisplayedItems => 1 + (boatCampManager != null ? boatCampManager.bambooPoleLevel : 0);

        public bool HangItem(ItemData item)
        {
            if (displayedItems.Count >= MaxDisplayedItems)
            {
                Debug.LogWarning("[BambooPole] Không thể treo thêm. Cây Bẹo đã đầy!");
                return false;
            }

            // Must have at least 1 in inventory to hang it
            if (!inventoryManager.Inventory.ContainsKey(item) || inventoryManager.Inventory[item] <= 0)
            {
                Debug.LogWarning("[BambooPole] Không có vật phẩm này trong kho để treo!");
                return false;
            }

            if (!displayedItems.Contains(item))
            {
                displayedItems.Add(item);
                Debug.Log($"[BambooPole] Đã treo {item.itemName} lên Cây Bẹo!");
                return true;
            }
            
            return false;
        }

        public void RemoveItem(ItemData item)
        {
            if (displayedItems.Contains(item))
            {
                displayedItems.Remove(item);
                Debug.Log($"[BambooPole] Đã gỡ {item.itemName} khỏi Cây Bẹo!");
            }
        }

        public void ClearPole()
        {
            displayedItems.Clear();
            Debug.Log("[BambooPole] Đã dọn sạch Cây Bẹo.");
        }

        public void LoadDisplayedItems(List<ItemData> items)
        {
            displayedItems = new List<ItemData>(items);
        }
    }
}
