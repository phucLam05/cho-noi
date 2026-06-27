using UnityEngine;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Presentation
{
    public class HagglingSystem : MonoBehaviour
    {
        [Header("References")]
        public PlayerStats playerStats;
        public EconomyManager economyManager;
        public InventoryManager inventoryManager;

        [Header("Session State")]
        [SerializeField] private float affectionLevel = 0f;
        private const float MAX_AFFECTION = 100f;

        [Header("Config (Có thể chỉnh sửa)")]
        public float sweetTalkStaminaCost = 10f;
        public float sweetTalkAffectionGain = 15f;
        
        private void Awake()
        {
            if (playerStats == null) playerStats = FindFirstObjectByType<PlayerStats>();
            if (economyManager == null) economyManager = FindFirstObjectByType<EconomyManager>();
            if (inventoryManager == null) inventoryManager = FindFirstObjectByType<InventoryManager>();
        }

        public void StartSession()
        {
            affectionLevel = 0f;
            Debug.Log("[HagglingSystem] Đã bắt đầu phiên giao dịch mới. Thiện cảm reset về 0.");
        }

        public float GetAffectionLevel() => affectionLevel;

        /// <summary>
        /// Hành động "Nói ngọt": Tốn thể lực, tăng thiện cảm
        /// </summary>
        public void SweetTalk()
        {
            float actualCost = Mathf.Max(0, sweetTalkStaminaCost - playerStats.sweetTalkCostReduction);
            
            if (playerStats.ConsumeStamina(actualCost))
            {
                affectionLevel = Mathf.Clamp(affectionLevel + sweetTalkAffectionGain, 0, MAX_AFFECTION);
                Debug.Log($"[HagglingSystem] NÓI NGỌT thành công! Thiện cảm tăng lên {affectionLevel}/{MAX_AFFECTION}.");
            }
            else
            {
                Debug.LogWarning("[HagglingSystem] Không đủ thể lực để Nói ngọt!");
            }
        }

        /// <summary>
        /// Hành động "Tặng quà": Trừ item trong kho (không lấy tiền), tăng mạnh thiện cảm
        /// </summary>
        public void GiveGift(ItemData item)
        {
            if (item == null) return;

            // Dùng hàm SellItem của InventoryManager nhưng không gọi qua EconomyManager để không lấy tiền
            if (inventoryManager.SellItem(item, 1))
            {
                // Tăng thiện cảm dựa vào giá trị món quà (Ví dụ: 1000 VNĐ = 1 điểm thiện cảm)
                float affectionGain = item.basePrice / 1000f;
                affectionLevel = Mathf.Clamp(affectionLevel + affectionGain, 0, MAX_AFFECTION);
                
                Debug.Log($"[HagglingSystem] ĐÃ TẶNG {item.itemName}. Thiện cảm tăng mạnh +{affectionGain:F1}. Hiện tại: {affectionLevel}/{MAX_AFFECTION}");
            }
            else
            {
                Debug.LogWarning($"[HagglingSystem] Không có {item.itemName} trong kho để tặng!");
            }
        }

        /// <summary>
        /// Tính giá chốt đơn dựa trên Thiện cảm
        /// </summary>
        public int CalculateFinalPricePerUnit(ItemData item)
        {
            if (item == null) return 0;
            
            // Công thức: Bonus Ratio = (Affection / 100) * MaxBonusRatio
            float bonusRatio = (affectionLevel / MAX_AFFECTION) * playerStats.maxBonusPriceRatio;
            
            float finalPrice = item.basePrice * (1f + bonusRatio);
            return Mathf.RoundToInt(finalPrice);
        }

        /// <summary>
        /// Chốt đơn bán sỉ toàn bộ mặt hàng đó trong kho (hoặc số lượng cụ thể)
        /// </summary>
        public void FinalizeDeal(ItemData item, int amount)
        {
            int pricePerUnit = CalculateFinalPricePerUnit(item);
            int totalRevenue = pricePerUnit * amount;

            Debug.Log($"[HagglingSystem] Chuẩn bị chốt đơn: Giá gốc {item.basePrice:N0}, Giá chốt {pricePerUnit:N0} (nhờ {affectionLevel} Thiện cảm).");

            bool success = economyManager.SellItemWholesale(item, amount, inventoryManager, totalRevenue);
            if (success)
            {
                StartSession(); // Reset sau khi chốt đơn xong
            }
        }
    }
}
