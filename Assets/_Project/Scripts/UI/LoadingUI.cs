using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class LoadingUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject loadingPanel;
        [SerializeField] private Slider progressBar;
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private TMP_Text tipText;

        [Header("Tips Config")]
        [SerializeField] private string[] gameplayTips = new string[]
        {
            "Treo nông sản lên Cây Bẹo để thu hút nhiều thương lái ghé ghe của bạn hơn.",
            "Hãy di chuyển về bến nhà trước 18:00 hàng ngày để ngủ, nếu không bạn sẽ bị phạt tiền vì cảm lạnh.",
            "Cọc gỗ WoodPost ở trại ghe phía Bắc là nơi giúp bạn sửa chữa và nâng cấp ghe buôn.",
            "Trả giá với thương lái sẽ tiêu tốn thể lực. Hãy ăn bún riêu/cà phê từ các ghe bán dạo để phục hồi sức.",
            "Thương lái lớn mua sỉ số lượng nhiều, còn dân làng mua lẻ giá cao nhưng số lượng ít."
        };

        private void Start()
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(false);
            }
        }

        public void Show(float initialProgress = 0f)
        {
            if (loadingPanel == null) return;

            // Pick random tip
            if (tipText != null && gameplayTips.Length > 0)
            {
                tipText.text = gameplayTips[Random.Range(0, gameplayTips.Length)];
            }

            if (progressBar != null)
            {
                progressBar.value = initialProgress;
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(initialProgress * 100)}%";
            }

            loadingPanel.SetActive(true);
            loadingPanel.GetComponent<CanvasGroup>().alpha = 1f;
        }

        public void UpdateProgress(float value)
        {
            if (!loadingPanel.activeSelf) return;

            if (progressBar != null)
            {
                progressBar.DOValue(value, 0.2f).SetEase(Ease.OutSine);
            }

            if (progressText != null)
            {
                progressText.text = $"{Mathf.RoundToInt(value * 100)}%";
            }
        }

        public void Hide()
        {
            if (loadingPanel != null && loadingPanel.activeSelf)
            {
                var canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.DOFade(0f, 0.4f).SetEase(Ease.InSine).OnComplete(() =>
                    {
                        loadingPanel.SetActive(false);
                    });
                }
                else
                {
                    loadingPanel.SetActive(false);
                }
            }
        }
    }
}
