using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.Presentation;
using ChoNoi.Presentation;
using ChoNoi.UI;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private BambooPoleManager bambooPoleManager;
        [SerializeField] private TooltipUI tooltipUI;

        [Header("UI Panels")]
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Transform boatInventoryGrid;
        [SerializeField] private Transform bambooPoleGrid;

        [Header("Prefabs")]
        [SerializeField] private GameObject inventorySlotPrefab;
        [SerializeField] private GameObject bambooSlotPrefab;

        [Header("Texts")]
        [SerializeField] private TMP_Text capacityText;
        [SerializeField] private TMP_Text instructionText;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        private List<GameObject> activeInventorySlots = new List<GameObject>();
        private List<GameObject> activeBambooSlots = new List<GameObject>();
        
        private ItemData selectedItemForEquip;

        public bool IsOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        private void Start()
        {
            FindReferencesIfNeeded();
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(false);
            }
        }

        private void FindReferencesIfNeeded()
        {
            if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
            if (bambooPoleManager == null)
            {
                var boat = GameObject.Find("PlayerBoat");
                if (boat != null)
                {
                    bambooPoleManager = boat.GetComponent<BambooPoleManager>();
                }
                if (bambooPoleManager == null)
                {
                    bambooPoleManager = FindAnyObjectByType<BambooPoleManager>();
                }
            }
            if (tooltipUI == null) tooltipUI = FindAnyObjectByType<TooltipUI>();
        }

        public void Open()
        {
            FindReferencesIfNeeded();
            if (inventoryPanel != null)
            {
                inventoryPanel.SetActive(true);
                inventoryPanel.transform.localScale = Vector3.zero;
                inventoryPanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
                RefreshUI();
            }
        }

        public void Close()
        {
            if (inventoryPanel != null && inventoryPanel.activeSelf)
            {
                if (tooltipUI != null) tooltipUI.Hide();
                inventoryPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    inventoryPanel.SetActive(false);
                });
            }
        }

        public void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public void RefreshUI()
        {
            if (inventoryManager == null || bambooPoleManager == null) return;

            // Update Capacity Text
            if (capacityText != null)
            {
                capacityText.text = $"Khối lượng: {inventoryManager.CurrentTotalWeight:0}/{inventoryManager.MaxWeightCapacity:0} kg";
            }

            // 1. Refresh Boat Inventory Slots
            foreach (var slot in activeInventorySlots)
            {
                if (slot != null) Destroy(slot);
            }
            activeInventorySlots.Clear();

            foreach (var kvp in inventoryManager.Inventory)
            {
                ItemData item = kvp.Key;
                int count = kvp.Value;

                if (item != null && count > 0)
                {
                    GameObject slotObj = Instantiate(inventorySlotPrefab, boatInventoryGrid);
                    activeInventorySlots.Add(slotObj);

                    // Setup item icon
                    Image iconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();
                    if (iconImage != null && item.icon != null)
                    {
                        iconImage.sprite = item.icon;
                        iconImage.gameObject.SetActive(true);
                    }

                    // Setup item count text
                    TMP_Text countText = slotObj.transform.Find("Count")?.GetComponent<TMP_Text>();
                    if (countText != null)
                    {
                        countText.text = count.ToString();
                    }

                    // Add Button component click for Click-to-Equip
                    Button btn = slotObj.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => OnInventorySlotClicked(item));
                    }

                    // Tooltip trigger
                    var tooltipTrigger = slotObj.AddComponent<TooltipTrigger>();
                    tooltipTrigger.Setup(item, count, tooltipUI);
                }
            }

            // 2. Refresh Bamboo Pole Slots
            foreach (var slot in activeBambooSlots)
            {
                if (slot != null) Destroy(slot);
            }
            activeBambooSlots.Clear();

            int maxSlots = bambooPoleManager.MaxDisplayedItems;
            List<ItemData> displayed = bambooPoleManager.DisplayedItems;

            for (int i = 0; i < maxSlots; i++)
            {
                int index = i;
                GameObject slotObj = Instantiate(bambooSlotPrefab, bambooPoleGrid);
                activeBambooSlots.Add(slotObj);

                TMP_Text slotLabel = slotObj.transform.Find("Label")?.GetComponent<TMP_Text>();
                Image itemIconImage = slotObj.transform.Find("Icon")?.GetComponent<Image>();
                Button removeBtn = slotObj.transform.Find("RemoveButton")?.GetComponent<Button>();

                if (index < displayed.Count)
                {
                    ItemData item = displayed[index];
                    if (slotLabel != null) slotLabel.text = item.itemName;
                    if (itemIconImage != null && item.icon != null)
                    {
                        itemIconImage.sprite = item.icon;
                        itemIconImage.gameObject.SetActive(true);
                    }
                    if (removeBtn != null)
                    {
                        removeBtn.gameObject.SetActive(true);
                        removeBtn.onClick.RemoveAllListeners();
                        removeBtn.onClick.AddListener(() => {
                            bambooPoleManager.RemoveItem(item);
                            RefreshUI();
                        });
                    }
                }
                else
                {
                    // Empty slot
                    if (slotLabel != null) slotLabel.text = "Trống (Bấm hàng để treo)";
                    if (itemIconImage != null) itemIconImage.gameObject.SetActive(false);
                    if (removeBtn != null) removeBtn.gameObject.SetActive(false);

                    // Allow assigning item to this slot
                    Button slotBtn = slotObj.GetComponent<Button>();
                    if (slotBtn != null)
                    {
                        slotBtn.onClick.RemoveAllListeners();
                        slotBtn.onClick.AddListener(() => OnBambooSlotClicked(index));
                    }
                }
            }
        }

        private void OnInventorySlotClicked(ItemData item)
        {
            // Set selected item for equipping onto the bamboo pole
            selectedItemForEquip = item;
            if (instructionText != null)
            {
                instructionText.text = $"Chọn một ô Trống trên Cây Bẹo để treo {item.itemName}.";
            }
            
            // Auto equip if there is only 1 slot or free slot available to make it super fast
            if (bambooPoleManager != null && bambooPoleManager.DisplayedItems.Count < bambooPoleManager.MaxDisplayedItems)
            {
                if (!bambooPoleManager.DisplayedItems.Contains(item))
                {
                    bambooPoleManager.HangItem(item);
                    selectedItemForEquip = null;
                    if (instructionText != null)
                    {
                        instructionText.text = "Đã treo quả thành công lên Cây Bẹo!";
                    }
                    RefreshUI();
                }
                else
                {
                    if (instructionText != null)
                    {
                        instructionText.text = "Mặt hàng này đã được treo rồi.";
                    }
                }
            }
        }

        private void OnBambooSlotClicked(int slotIndex)
        {
            if (selectedItemForEquip == null) return;

            if (bambooPoleManager != null)
            {
                // Remove whatever is in that slot first (already handled by index < displayed.Count check)
                if (!bambooPoleManager.DisplayedItems.Contains(selectedItemForEquip))
                {
                    bambooPoleManager.HangItem(selectedItemForEquip);
                }
                selectedItemForEquip = null;
                if (instructionText != null)
                {
                    instructionText.text = "Đã treo quả thành công lên Cây Bẹo!";
                }
                RefreshUI();
            }
        }

        public void ClearPole()
        {
            if (bambooPoleManager != null)
            {
                bambooPoleManager.ClearPole();
                RefreshUI();
            }
        }
    }

    // Helper class for tooltips
    public class TooltipTrigger : MonoBehaviour, UnityEngine.EventSystems.IPointerEnterHandler, UnityEngine.EventSystems.IPointerExitHandler
    {
        private ItemData item;
        private int count;
        private TooltipUI tooltip;

        public void Setup(ItemData itemData, int itemCount, TooltipUI tooltipUI)
        {
            item = itemData;
            count = itemCount;
            tooltip = tooltipUI;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (tooltip != null && item != null)
            {
                tooltip.Show(item, count);
            }
        }

        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData eventData)
        {
            if (tooltip != null)
            {
                tooltip.Hide();
            }
        }
    }
}
