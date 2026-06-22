using System;
using UnityEngine;
using ChoNoiMienTay.Data;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.Presentation;

namespace ChoNoiMienTay.Systems
{
    public class BargainingSystem : MonoBehaviour
    {
        [SerializeField] private BargainingEconomyConfig economyConfig;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private EconomyManager economyManager;

        public event Action OnStateChanged;

        public ItemData SelectedItem { get; private set; }
        public BargainingNpcProfile CurrentNpc { get; private set; }
        public int CurrentAskPrice { get; private set; }
        public int NpcOpeningPrice { get; private set; }
        public int NpcMaxAcceptPrice { get; private set; }
        public int CurrentMarketPrice { get; private set; }
        public int BargainQuantity { get; private set; } = 1;
        public int NegotiationTurns { get; private set; } = 0;
        public int PlayerProposedPrice { get; private set; } = 0;
        public int NpcLastOfferedPrice { get; private set; } = 0;
        public bool NpcWalkedAway { get; private set; } = false;
        public int MaxTurns => 3;
        public string CurrentMessage { get; private set; } = "Chọn hàng trên ghe để bắt đầu vào chợ.";
        public bool HasActiveSession => CurrentNpc != null && SelectedItem != null;
        public BargainingEconomyConfig EconomyConfig => economyConfig;

        public void Configure(
            BargainingEconomyConfig config,
            PlayerStats player,
            InventoryManager inventory,
            EconomyManager economy)
        {
            economyConfig = config;
            playerStats = player;
            inventoryManager = inventory;
            economyManager = economy;
            NotifyStateChanged();
        }

        public int GetInventoryCount(ItemData item)
        {
            if (item == null || inventoryManager == null)
            {
                return 0;
            }

            return inventoryManager.Inventory.TryGetValue(item, out int amount) ? amount : 0;
        }

        public void SelectInventoryItem(ItemData item)
        {
            if (item == null)
            {
                CurrentMessage = "Vật phẩm không hợp lệ.";
                NotifyStateChanged();
                return;
            }

            if (GetInventoryCount(item) <= 0)
            {
                CurrentMessage = $"Bạn không còn {item.itemName} trên ghe.";
                SelectedItem = null;
                EndSession(false);
                NotifyStateChanged();
                return;
            }

            SelectedItem = item;
            EndSession(false);
            CurrentMessage = $"Đã chọn {item.itemName}. Chuyển sang Shop để tìm người mua.";
            NotifyStateChanged();
        }

        public bool StartSession(BargainingNpcProfile npcProfile, int quantity)
        {
            if (economyConfig == null || npcProfile == null || SelectedItem == null)
            {
                CurrentMessage = "Thiếu dữ liệu để bắt đầu mặc cả.";
                NotifyStateChanged();
                return false;
            }

            BargainingItemEconomyEntry entry = economyConfig.FindItemEntry(SelectedItem);
            if (entry == null)
            {
                CurrentMessage = $"Chưa cấu hình kinh tế cho {SelectedItem.itemName}.";
                NotifyStateChanged();
                return false;
            }

            int available = GetInventoryCount(SelectedItem);
            if (available <= 0)
            {
                CurrentMessage = $"Bạn không còn {SelectedItem.itemName} trong kho.";
                NotifyStateChanged();
                return false;
            }

            BargainQuantity = Mathf.Clamp(quantity, 1, available);
            CurrentNpc = npcProfile;
            CurrentMarketPrice = CalculateMarketPrice(entry);
            NpcOpeningPrice = RoundToStep(Mathf.RoundToInt(CurrentMarketPrice * npcProfile.openingPriceMultiplier));
            NpcMaxAcceptPrice = RoundToStep(Mathf.Max(NpcOpeningPrice, Mathf.RoundToInt(CurrentMarketPrice * npcProfile.maxAcceptPriceMultiplier)));
            CurrentAskPrice = NpcOpeningPrice;

            CurrentMessage =
                $"{npcProfile.displayName} mở giá {NpcOpeningPrice:N0} VNĐ cho mỗi quả {SelectedItem.itemName} (Mặc cả sỉ cho {BargainQuantity} quả). " +
                "Tăng hoặc giảm giá đề nghị rồi chốt kèo.";
            NotifyStateChanged();
            return true;
        }

        public bool AdjustOffer(int direction)
        {
            if (!HasActiveSession)
            {
                CurrentMessage = "Hãy chọn NPC trước khi trả giá.";
                NotifyStateChanged();
                return false;
            }

            if (playerStats == null || !playerStats.ConsumeStamina(economyConfig.StaminaCostPerNegotiation))
            {
                CurrentMessage = "Không đủ thể lực để tiếp tục mặc cả.";
                NotifyStateChanged();
                return false;
            }

            CurrentAskPrice = Mathf.Max(economyConfig.OfferStep, CurrentAskPrice + direction * economyConfig.OfferStep);
            CurrentMessage = BuildNegotiationReaction();
            NotifyStateChanged();
            return true;
        }

        public bool TryAcceptDeal()
        {
            if (!HasActiveSession || economyManager == null || inventoryManager == null)
            {
                CurrentMessage = "Phiên mặc cả chưa sẵn sàng để chốt.";
                NotifyStateChanged();
                return false;
            }

            if (CurrentAskPrice > NpcMaxAcceptPrice)
            {
                CurrentMessage =
                    $"{CurrentNpc.displayName} lắc đầu: giá {CurrentAskPrice:N0} VNĐ còn quá cao. " +
                    $"Mốc họ chịu tối đa là khoảng {NpcMaxAcceptPrice:N0} VNĐ.";
                NotifyStateChanged();
                return false;
            }

            int totalRevenue = CurrentAskPrice * BargainQuantity;
            bool success = economyManager.SellItemWholesale(SelectedItem, BargainQuantity, inventoryManager, totalRevenue);
            if (!success)
            {
                CurrentMessage = "Không thể hoàn tất giao dịch. Kiểm tra lại hàng trong kho.";
                NotifyStateChanged();
                return false;
            }

            string itemName = SelectedItem.itemName;
            CurrentMessage = $"Chốt đơn thành công. {CurrentNpc.displayName} mua {BargainQuantity} quả {itemName} với giá {CurrentAskPrice:N0} VNĐ/quả (Tổng: {totalRevenue:N0} VNĐ).";

            if (GetInventoryCount(SelectedItem) <= 0)
            {
                SelectedItem = null;
            }

            EndSession(false);
            NotifyStateChanged();
            return true;
        }

        public void RejectDeal()
        {
            if (CurrentNpc != null)
            {
                CurrentMessage = $"{CurrentNpc.displayName} từ chối thương lượng thêm. Phiên mặc cả kết thúc.";
            }
            else
            {
                CurrentMessage = "Bạn đã hủy phiên giao dịch.";
            }

            EndSession(false);
            NotifyStateChanged();
        }

        private int CalculateMarketPrice(BargainingItemEconomyEntry entry)
        {
            int basePrice = entry.item != null ? entry.item.basePrice : 0;
            int minVariation = Mathf.Min(entry.minPriceVariation, entry.maxPriceVariation);
            int maxVariation = Mathf.Max(entry.minPriceVariation, entry.maxPriceVariation);
            int variation = UnityEngine.Random.Range(minVariation, maxVariation + 1);
            return Mathf.Max(economyConfig.OfferStep, basePrice + variation);
        }

        private string BuildNegotiationReaction()
        {
            int difference = CurrentAskPrice - NpcMaxAcceptPrice;
            if (difference <= 0)
            {
                return $"{CurrentNpc.displayName} gật gù: mức {CurrentAskPrice:N0} VNĐ nghe hợp lý đấy.";
            }

            if (difference <= economyConfig.OfferStep)
            {
                return $"{CurrentNpc.displayName} cân nhắc: bớt chút nữa là chốt được.";
            }

            return $"{CurrentNpc.displayName} nhăn mặt: giá {CurrentAskPrice:N0} VNĐ còn cao quá.";
        }

        private int RoundToStep(int value)
        {
            int step = economyConfig != null ? economyConfig.OfferStep : 500;
            return Mathf.Max(step, Mathf.RoundToInt(value / (float)step) * step);
        }

        private void EndSession(bool clearSelectedItem)
        {
            CurrentNpc = null;
            NpcOpeningPrice = 0;
            NpcMaxAcceptPrice = 0;
            CurrentAskPrice = 0;
            CurrentMarketPrice = 0;
            BargainQuantity = 1;
            NegotiationTurns = 0;
            PlayerProposedPrice = 0;
            NpcLastOfferedPrice = 0;
            NpcWalkedAway = false;

            if (clearSelectedItem)
            {
                SelectedItem = null;
            }
        }

        public void StartBargainingSession(BargainingNpcProfile npcProfile, ItemData item, int quantity)
        {
            SelectedItem = item;
            CurrentNpc = npcProfile;
            BargainQuantity = quantity;
            NegotiationTurns = 0;
            NpcWalkedAway = false;

            BargainingItemEconomyEntry entry = economyConfig.FindItemEntry(item);
            CurrentMarketPrice = entry != null ? CalculateMarketPrice(entry) : item.basePrice;

            NpcOpeningPrice = RoundToStep(Mathf.RoundToInt(CurrentMarketPrice * npcProfile.openingPriceMultiplier));
            NpcMaxAcceptPrice = RoundToStep(Mathf.Max(NpcOpeningPrice, Mathf.RoundToInt(CurrentMarketPrice * npcProfile.maxAcceptPriceMultiplier)));
            NpcLastOfferedPrice = NpcOpeningPrice;
            CurrentAskPrice = NpcOpeningPrice;

            CurrentMessage = $"{npcProfile.displayName} muốn mua sỉ {BargainQuantity} quả {item.itemName}. Họ đề xuất giá ban đầu là {NpcOpeningPrice:N0} VNĐ/quả, mời chú em đưa ra đơn giá đề xuất của mình.";
            NotifyStateChanged();
        }

        public bool ProposePrice(int proposedPrice)
        {
            if (!HasActiveSession) return false;

            // Consumes 5 stamina
            if (playerStats == null || !playerStats.ConsumeStamina(5))
            {
                CurrentMessage = "Không đủ thể lực để mặc cả.";
                NotifyStateChanged();
                return false;
            }

            PlayerProposedPrice = proposedPrice;

            if (RollAcceptance(PlayerProposedPrice))
            {
                // Accept deal immediately!
                CurrentAskPrice = PlayerProposedPrice;
                int totalRevenue = CurrentAskPrice * BargainQuantity;
                bool success = economyManager.SellItemWholesale(SelectedItem, BargainQuantity, inventoryManager, totalRevenue);
                if (success)
                {
                    CurrentMessage = $"Chốt đơn thành công! Khách đồng ý mua sỉ {BargainQuantity} quả {SelectedItem.itemName} với đơn giá đề xuất {CurrentAskPrice:N0} VNĐ/quả (Tổng: {totalRevenue:N0} VNĐ).";
                    if (GetInventoryCount(SelectedItem) <= 0)
                    {
                        SelectedItem = null;
                    }
                    EndSession(false);
                }
                else
                {
                    CurrentMessage = "Không thể hoàn tất giao dịch. Hãy kiểm tra lại khoang chứa.";
                }
                NotifyStateChanged();
                return true;
            }
            else
            {
                // Roll walk away on first turn too (turn index = 0)
                if (RollWalkAway(PlayerProposedPrice, 0))
                {
                    NpcWalkedAway = true;
                    CurrentMessage = $"{CurrentNpc.displayName} nhăn mặt rồi quay lưng bỏ đi: 'Giá gì mà đắt thế chú em! Tui không mua đâu.' Chú em có muốn kêu lại để chốt bán nhanh với giá cuối {NpcLastOfferedPrice:N0} VNĐ/quả không?";
                }
                else
                {
                    // Reject, start haggling
                    CurrentMessage = $"{CurrentNpc.displayName} nhăn mặt: 'Chém giá ác thế chú em! {PlayerProposedPrice:N0} VNĐ/quả thì đắt quá. Tui chỉ mua được tầm {NpcLastOfferedPrice:N0} VNĐ/quả thôi.'";
                }
                NotifyStateChanged();
                return false;
            }
        }

        public bool CounterOffer(int counterPrice)
        {
            if (!HasActiveSession) return false;

            // Consumes 5 stamina
            if (playerStats == null || !playerStats.ConsumeStamina(5))
            {
                CurrentMessage = "Không đủ thể lực để đôi co tiếp.";
                NotifyStateChanged();
                return false;
            }

            NegotiationTurns++;
            PlayerProposedPrice = counterPrice;

            if (RollAcceptance(PlayerProposedPrice))
            {
                // Accept deal!
                CurrentAskPrice = PlayerProposedPrice;
                int totalRevenue = CurrentAskPrice * BargainQuantity;
                bool success = economyManager.SellItemWholesale(SelectedItem, BargainQuantity, inventoryManager, totalRevenue);
                if (success)
                {
                    CurrentMessage = $"Chốt đơn thành công! Sau khi đôi co, khách đồng ý mua {BargainQuantity} quả {SelectedItem.itemName} với giá {CurrentAskPrice:N0} VNĐ/quả (Tổng: {totalRevenue:N0} VNĐ).";
                    if (GetInventoryCount(SelectedItem) <= 0)
                    {
                        SelectedItem = null;
                    }
                    EndSession(false);
                }
                else
                {
                    CurrentMessage = "Không thể hoàn tất giao dịch. Hãy kiểm tra lại khoang chứa.";
                }
                NotifyStateChanged();
                return true;
            }

            // Reject - Check if walk away
            if (RollWalkAway(PlayerProposedPrice, NegotiationTurns))
            {
                NpcWalkedAway = true;
                CurrentMessage = $"{CurrentNpc.displayName} nhún vai quay lưng đi: 'Trả giá kiểu này tui không mua đâu!'. Chú em có muốn kêu lại để chốt bán nhanh với giá cuối {NpcLastOfferedPrice:N0} VNĐ/quả không?";
            }
            else
            {
                // Increase NPC offer slightly to make progress
                int step = (NpcMaxAcceptPrice - NpcLastOfferedPrice) / (MaxTurns - NegotiationTurns + 1);
                step = Mathf.Max(economyConfig.OfferStep, RoundToStep(step));
                NpcLastOfferedPrice = Mathf.Min(NpcMaxAcceptPrice, NpcLastOfferedPrice + step);

                CurrentMessage = $"{CurrentNpc.displayName} lắc đầu: 'Giá {PlayerProposedPrice:N0} VNĐ/quả vẫn đắt quá. Tui chỉ trả thêm được tới {NpcLastOfferedPrice:N0} VNĐ/quả thôi!' (Lượt thương thảo: {NegotiationTurns}/3).";
            }

            NotifyStateChanged();
            return false;
        }

        public bool AcceptNpcLastOffer()
        {
            if (!HasActiveSession) return false;

            CurrentAskPrice = NpcLastOfferedPrice;
            int totalRevenue = CurrentAskPrice * BargainQuantity;
            bool success = economyManager.SellItemWholesale(SelectedItem, BargainQuantity, inventoryManager, totalRevenue);
            if (success)
            {
                CurrentMessage = $"Đã kêu khách lại! Chốt đơn bán {BargainQuantity} quả {SelectedItem.itemName} với giá cuối {CurrentAskPrice:N0} VNĐ/quả (Tổng: {totalRevenue:N0} VNĐ).";
                if (GetInventoryCount(SelectedItem) <= 0)
                {
                    SelectedItem = null;
                }
                EndSession(false);
            }
            else
            {
                CurrentMessage = "Không thể hoàn tất giao dịch. Hãy kiểm tra lại khoang chứa.";
            }

            NotifyStateChanged();
            return success;
        }

        private bool RollAcceptance(int proposedPrice)
        {
            if (proposedPrice <= NpcMaxAcceptPrice)
            {
                return true;
            }

            // Exponential decay probability for markup
            float ratio = (proposedPrice - NpcMaxAcceptPrice) / (float)NpcMaxAcceptPrice;
            float acceptProbability = Mathf.Exp(-5.0f * ratio);
            
            float rng = UnityEngine.Random.value;
            return rng < acceptProbability;
        }

        private bool RollWalkAway(int proposedPrice, int turns)
        {
            if (turns >= MaxTurns)
            {
                return true;
            }

            float ratio = (proposedPrice - NpcMaxAcceptPrice) / (float)NpcMaxAcceptPrice;
            // Exponential saturation for walk away probability based on markup and turn count
            float baseFloor = 0f;
            if (turns == 1) baseFloor = 0.10f;
            else if (turns == 2) baseFloor = 0.30f;

            float walkAwayProbability = 1.0f - Mathf.Exp(-3.0f * ratio * (turns + 1));
            walkAwayProbability = Mathf.Clamp01(Mathf.Max(baseFloor, walkAwayProbability));

            float rng = UnityEngine.Random.value;
            return rng < walkAwayProbability;
        }

        private void NotifyStateChanged()
        {
            OnStateChanged?.Invoke();
        }
    }
}
