using UnityEngine;
using UnityEngine.UI;
using ChoNoiMienTay.Infrastructure;
using System.Text;

namespace ChoNoiMienTay.Presentation
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("System References")]
        public InventoryManager inventoryManager;
        public EconomyManager economyManager;
        public PlayerStats playerStats;
        public HagglingSystem hagglingSystem;

        [Header("UI Texts")]
        public Text inventoryDisplayText;
        public Text capacityDisplayText;
        public Text statsDisplayText; // Hiển thị Tiền, Thể lực, Thiện cảm

        [Header("Test Data")]
        public ItemData khomItem;
        public ItemData biDaoItem;
        public ItemData quanAoItem;
        public ServiceData bunRieuService;

        private void Start()
        {
            // Khởi tạo Dữ liệu
            InitializeTestData();

            // Tìm references nếu chưa có
            if (inventoryManager == null) inventoryManager = FindObjectOfType<InventoryManager>();
            if (economyManager == null) economyManager = FindObjectOfType<EconomyManager>();
            if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
            if (hagglingSystem == null) hagglingSystem = FindObjectOfType<HagglingSystem>();

            BindButtons();

            UpdateUI();
        }

        private void BindButtons()
        {
            Button[] buttons = GetComponentsInChildren<Button>(true);
            foreach (var btn in buttons)
            {
                btn.onClick.RemoveAllListeners(); // Xóa các listener cũ nếu có để tránh duplicate
                switch (btn.name)
                {
                    case "BtnBunRieu": btn.onClick.AddListener(BuyBunRieu); break;
                    case "BtnSweetTalk": btn.onClick.AddListener(DoSweetTalk); break;
                    case "BtnGift": btn.onClick.AddListener(GiveGiftKhom); break;
                    case "BuyQuanAo": btn.onClick.AddListener(BuyQuanAo); break;
                    case "SellQuanAo": btn.onClick.AddListener(SellQuanAoWholesale); break;
                    case "BuyKhom": btn.onClick.AddListener(BuyKhom); break;
                    case "SellKhom": btn.onClick.AddListener(SellKhomWholesale); break;
                    case "BuyBiDao": btn.onClick.AddListener(BuyBiDao); break;
                    case "SellBiDao": btn.onClick.AddListener(SellBiDaoWholesale); break;
                }
            }
        }

        private void InitializeTestData()
        {
            khomItem = ScriptableObject.CreateInstance<ItemData>();
            khomItem.itemID = "ITM_KHOM"; khomItem.itemName = "Khóm (Dứa)";
            khomItem.weight = 5f; khomItem.basePrice = 15000;

            biDaoItem = ScriptableObject.CreateInstance<ItemData>();
            biDaoItem.itemID = "ITM_BIDAO"; biDaoItem.itemName = "Bí Đao";
            biDaoItem.weight = 15f; biDaoItem.basePrice = 25000;

            quanAoItem = ScriptableObject.CreateInstance<ItemData>();
            quanAoItem.itemID = "ITM_QUANAO"; quanAoItem.itemName = "Quần Áo";
            quanAoItem.weight = 1f; quanAoItem.basePrice = 150000;

            bunRieuService = ScriptableObject.CreateInstance<ServiceData>();
            bunRieuService.serviceID = "SVC_BUNRIEU"; bunRieuService.serviceName = "Tô Bún Riêu";
            bunRieuService.costMoney = 25000;
            bunRieuService.staminaRestoreAmount = 30f;
        }

        private void Update()
        {
            UpdateUI();
        }

        public void UpdateUI()
        {
            if (inventoryManager == null || playerStats == null || hagglingSystem == null) return;

            capacityDisplayText.text = $"Sức chứa ghe: {inventoryManager.CurrentTotalWeight} / {inventoryManager.MaxWeightCapacity} kg";

            statsDisplayText.text = $"Tiền: {playerStats.CurrentMoney:N0} VNĐ | " +
                                    $"Thể lực: {playerStats.CurrentStamina}/{playerStats.MaxStamina} | " +
                                    $"Thiện cảm NPC: {hagglingSystem.GetAffectionLevel()}/100";

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("DANH SÁCH HÀNG TRÊN GHE:");
            sb.AppendLine("-----------------------------");

            if (inventoryManager.Inventory.Count == 0)
            {
                sb.AppendLine("Ghe trống.");
            }
            else
            {
                foreach (var kvp in inventoryManager.Inventory)
                {
                    sb.AppendLine($"- {kvp.Key.itemName} (x{kvp.Value}) - Tổng Nặng: {kvp.Key.weight * kvp.Value} kg");
                }
            }

            inventoryDisplayText.text = sb.ToString();
        }

        // --- CÁC HÀM XỬ LÝ NÚT BẤM ---

        public void BuyKhom() => economyManager.BuyItemToInventory(khomItem, 1, inventoryManager);
        public void BuyBiDao() => economyManager.BuyItemToInventory(biDaoItem, 1, inventoryManager);
        public void BuyQuanAo() => economyManager.BuyItemToInventory(quanAoItem, 1, inventoryManager);

        public void SellKhomWholesale() => hagglingSystem.FinalizeDeal(khomItem, GetItemAmount(khomItem));
        public void SellBiDaoWholesale() => hagglingSystem.FinalizeDeal(biDaoItem, GetItemAmount(biDaoItem));
        public void SellQuanAoWholesale() => hagglingSystem.FinalizeDeal(quanAoItem, GetItemAmount(quanAoItem));

        public void DoSweetTalk() => hagglingSystem.SweetTalk();
        public void GiveGiftKhom() => hagglingSystem.GiveGift(khomItem);
        public void BuyBunRieu() => economyManager.BuyService(bunRieuService);

        private int GetItemAmount(ItemData item)
        {
            if (inventoryManager.Inventory.TryGetValue(item, out int amount))
                return amount;
            return 0; // Trả về 0 nếu không có trong kho (lúc chốt đơn sẽ fail)
        }
    }
}
