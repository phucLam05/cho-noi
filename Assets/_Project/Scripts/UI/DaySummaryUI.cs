using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChoNoiMienTay.Presentation;
using ChoNoi.Application;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class DaySummaryUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject summaryPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text daySubtitleText;
        
        [Header("Metrics")]
        [SerializeField] private TMP_Text incomeText;
        [SerializeField] private TMP_Text repairText;
        [SerializeField] private TMP_Text lateFeeText;
        [SerializeField] private TMP_Text totalProfitText;

        [Header("Buttons")]
        [SerializeField] private Button continueButton;

        [Header("Color Config")]
        [SerializeField] private Color positiveColor = new Color(0.2f, 0.8f, 0.2f); // Green
        [SerializeField] private Color negativeColor = new Color(0.9f, 0.2f, 0.2f); // Red
        [SerializeField] private Color defaultColor = Color.white;

        private System.Action onContinueCallback;
        private EconomyManager economyManager;
        private TimeManager timeManager;

        public bool IsOpen => summaryPanel != null && summaryPanel.activeSelf;

        private void Start()
        {
            if (continueButton != null)
            {
                continueButton.onClick.AddListener(OnContinuePressed);
            }
            if (summaryPanel != null)
            {
                summaryPanel.SetActive(false);
            }
        }

        public void Open(EconomyManager economy, TimeManager time, System.Action onContinue)
        {
            economyManager = economy;
            timeManager = time;
            onContinueCallback = onContinue;

            if (summaryPanel == null) return;

            summaryPanel.SetActive(true);
            summaryPanel.transform.localScale = Vector3.zero;
            summaryPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true); // setUpdate(true) so it animates when Time.timeScale = 0

            int day = timeManager != null ? timeManager.CurrentDay : 1;
            daySubtitleText.text = $"Kết thúc Ngày {day}";

            int income = 0;
            int repair = 0;
            int lateFee = 0;

            if (economyManager != null)
            {
                income = economyManager.DailyIncome;
                repair = economyManager.DailyRepairCost;
                lateFee = economyManager.DailyLateFee;
            }

            int totalProfit = income - repair - lateFee;

            // Update text and colors
            incomeText.text = $"+{income:N0} VNĐ";
            incomeText.color = income > 0 ? positiveColor : defaultColor;

            repairText.text = repair > 0 ? $"-{repair:N0} VNĐ" : "0 VNĐ";
            repairText.color = repair > 0 ? negativeColor : defaultColor;

            lateFeeText.text = lateFee > 0 ? $"-{lateFee:N0} VNĐ" : "0 VNĐ";
            lateFeeText.color = lateFee > 0 ? negativeColor : defaultColor;

            if (totalProfit >= 0)
            {
                totalProfitText.text = $"+{totalProfit:N0} VNĐ";
                totalProfitText.color = positiveColor;
            }
            else
            {
                totalProfitText.text = $"-{Mathf.Abs(totalProfit):N0} VNĐ";
                totalProfitText.color = negativeColor;
            }
        }

        private void OnContinuePressed()
        {
            // Trigger animation and call continuation callback
            summaryPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                summaryPanel.SetActive(false);
                onContinueCallback?.Invoke();
            });
        }
    }
}
