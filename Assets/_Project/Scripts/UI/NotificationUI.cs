using UnityEngine;
using TMPro;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class NotificationUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject notificationPanel;
        [SerializeField] private TMP_Text notificationText;

        [Header("Animation Settings")]
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float stayDuration = 2.0f;
        [SerializeField] private float fadeDuration = 0.4f;

        public static NotificationUI Instance { get; private set; }

        private RectTransform panelRect;
        private CanvasGroup panelCanvasGroup;
        private Vector2 originalPosition;
        private Vector2 offscreenPosition;
        private Tween activeSequence;

        private void Awake()
        {
            Instance = this;

            if (notificationPanel != null)
            {
                panelRect = notificationPanel.GetComponent<RectTransform>();
                panelCanvasGroup = notificationPanel.GetComponent<CanvasGroup>();
                if (panelCanvasGroup == null)
                {
                    panelCanvasGroup = notificationPanel.AddComponent<CanvasGroup>();
                }
                
                originalPosition = panelRect.anchoredPosition;
                // Position offscreen above
                offscreenPosition = new Vector2(originalPosition.x, originalPosition.y + 150f);
                panelRect.anchoredPosition = offscreenPosition;
                notificationPanel.SetActive(false);
            }
        }

        public void ShowNotification(string message)
        {
            if (notificationPanel == null || notificationText == null) return;

            // Kill any currently running animation sequence
            if (activeSequence != null)
            {
                activeSequence.Kill();
            }

            notificationText.text = message;
            notificationPanel.SetActive(true);
            
            panelRect.anchoredPosition = offscreenPosition;
            panelCanvasGroup.alpha = 1f;

            // Construct new animation sequence
            Sequence seq = DOTween.Sequence();
            seq.Append(panelRect.DOAnchorPos(originalPosition, slideDuration).SetEase(Ease.OutBack));
            seq.AppendInterval(stayDuration);
            seq.Append(panelCanvasGroup.DOFade(0f, fadeDuration).SetEase(Ease.InSine));
            seq.OnComplete(() =>
            {
                notificationPanel.SetActive(false);
            });

            activeSequence = seq;
        }
    }
}
