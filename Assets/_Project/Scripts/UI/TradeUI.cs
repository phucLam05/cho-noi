using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChoNoiMienTay.Systems;
using ChoNoiMienTay.Data;
using ChoNoiMienTay.Infrastructure;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class TradeUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject tradePanel;
        [SerializeField] private TMP_Text customerNameText;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemQuantityText;
        
        [Header("Price Info")]
        [SerializeField] private TMP_Text basePriceText;
        [SerializeField] private TMP_Text currentOfferText;
        [SerializeField] private TMP_Text summaryText;

        [Header("Friendship / Negotiation Meter")]
        [SerializeField] private Slider meterSlider;
        [SerializeField] private Image meterFillImage;
        [SerializeField] private Color greenColor = new Color(0.2f, 0.8f, 0.2f);
        [SerializeField] private Color yellowColor = new Color(0.9f, 0.9f, 0.1f);
        [SerializeField] private Color redColor = new Color(0.9f, 0.2f, 0.2f);

        [Header("Buttons")]
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button walkAwayButton;

        private BargainingSystem bargainingSystem;
        private int proposedPrice = 0;

        public bool IsOpen => tradePanel != null && tradePanel.activeSelf;

        public void Initialize(BargainingSystem system)
        {
            bargainingSystem = system;
            
            increaseButton.onClick.AddListener(IncreasePrice);
            decreaseButton.onClick.AddListener(DecreasePrice);
            acceptButton.onClick.AddListener(SubmitOffer);
            walkAwayButton.onClick.AddListener(CancelNegotiation);
        }

        public void Open(BargainingNpcProfile npc, ItemData item, int quantity)
        {
            if (tradePanel == null || bargainingSystem == null) return;

            tradePanel.SetActive(true);
            tradePanel.transform.localScale = Vector3.zero;
            tradePanel.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            customerNameText.text = npc.displayName;
            itemNameText.text = item.itemName;
            itemQuantityText.text = $"x{quantity}";
            if (itemIcon != null && item.icon != null)
            {
                itemIcon.sprite = item.icon;
                itemIcon.gameObject.SetActive(true);
            }

            proposedPrice = bargainingSystem.NpcOpeningPrice;
            basePriceText.text = $"Giá gốc: {item.basePrice:N0}đ";

            UpdateOfferDisplay();
        }

        public void Close()
        {
            if (tradePanel != null && tradePanel.activeSelf)
            {
                tradePanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    tradePanel.SetActive(false);
                });
            }
        }

        private void IncreasePrice()
        {
            int step = bargainingSystem.EconomyConfig != null ? bargainingSystem.EconomyConfig.OfferStep : 500;
            proposedPrice += step;
            UpdateOfferDisplay();
            
            // Subtle button click pop animation
            increaseButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 5, 1f);
        }

        private void DecreasePrice()
        {
            int step = bargainingSystem.EconomyConfig != null ? bargainingSystem.EconomyConfig.OfferStep : 500;
            proposedPrice = Mathf.Max(step, proposedPrice - step);
            UpdateOfferDisplay();
            
            // Subtle button click pop animation
            decreaseButton.transform.DOPunchScale(Vector3.one * 0.1f, 0.15f, 5, 1f);
        }

        private void UpdateOfferDisplay()
        {
            currentOfferText.text = $"{proposedPrice:N0} VNĐ/Quả";
            if (summaryText != null)
            {
                int total = proposedPrice * bargainingSystem.BargainQuantity;
                summaryText.text = $"Tổng: {total:N0} VNĐ";
            }

            UpdateFriendshipMeter();
        }

        private void UpdateFriendshipMeter()
        {
            if (meterSlider == null || bargainingSystem == null) return;

            // Calculate ratio of current proposed price vs NPC max accept price
            // The higher the price, the lower the friendship/patience
            int npcMax = bargainingSystem.NpcMaxAcceptPrice;
            int npcOpen = bargainingSystem.NpcOpeningPrice;

            float patienceValue = 1f;

            if (proposedPrice <= npcMax)
            {
                // Very safe, patience is high
                patienceValue = 1f;
            }
            else
            {
                // Over the limit: linearly scale patience down as proposed price goes up
                float excessRatio = (float)(proposedPrice - npcMax) / (npcMax * 0.5f); // 50% over limit is zero patience
                patienceValue = Mathf.Clamp01(1f - excessRatio);
            }

            // Animate slider fill using DOTween
            meterSlider.DOValue(patienceValue, 0.25f).SetEase(Ease.OutSine);

            // Animate meter color based on patience level
            Color targetColor = greenColor;
            if (patienceValue < 0.35f)
            {
                targetColor = redColor;
            }
            else if (patienceValue < 0.7f)
            {
                targetColor = yellowColor;
            }

            if (meterFillImage != null)
            {
                meterFillImage.DOColor(targetColor, 0.25f);
            }
        }

        private void SubmitOffer()
        {
            if (bargainingSystem == null) return;

            // Submit proposed price to the BargainingSystem
            bool dealFinished = bargainingSystem.ProposePrice(proposedPrice);
            if (dealFinished)
            {
                // If accepted, close the Haggling UI
                Close();
            }
            else
            {
                // NPC counters or walks away
                // Update display according to current system message
                UpdateOfferDisplay();
                
                if (bargainingSystem.NpcWalkedAway)
                {
                    Close();
                }
            }
        }

        private void CancelNegotiation()
        {
            if (bargainingSystem != null)
            {
                bargainingSystem.RejectDeal();
            }
            Close();
        }
    }
}
