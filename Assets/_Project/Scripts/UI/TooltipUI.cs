using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChoNoiMienTay.Infrastructure;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class TooltipUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject tooltipPanel;
        [SerializeField] private Image itemIcon;
        [SerializeField] private TMP_Text itemNameText;
        [SerializeField] private TMP_Text itemDescriptionText;
        [SerializeField] private TMP_Text sellPriceText;
        [SerializeField] private TMP_Text countText;

        [Header("Offset")]
        [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

        private RectTransform canvasRect;
        private RectTransform tooltipRect;
        private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (tooltipPanel != null)
            {
                tooltipRect = tooltipPanel.GetComponent<RectTransform>();
                canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = tooltipPanel.AddComponent<CanvasGroup>();
                }
                canvasRect = GetComponentInParent<Canvas>().GetComponent<RectTransform>();
                tooltipPanel.SetActive(false);
            }
        }

        private void Update()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                FollowMouse();
            }
        }

        public void Show(ItemData item, int inventoryCount)
        {
            if (tooltipPanel == null || item == null) return;

            itemNameText.text = item.itemName;
            itemDescriptionText.text = $"Khối lượng: {item.weight:0.##} kg\nMột loại trái cây nhiệt đới từ miền Tây Nam Bộ.";
            
            sellPriceText.text = $"Giá trị: {item.basePrice:N0} VNĐ";
            countText.text = $"Đang có: {inventoryCount} quả";

            if (itemIcon != null)
            {
                if (item.icon != null)
                {
                    itemIcon.gameObject.SetActive(true);
                    itemIcon.sprite = item.icon;
                }
                else
                {
                    itemIcon.gameObject.SetActive(false);
                }
            }

            tooltipPanel.SetActive(true);
            canvasGroup.alpha = 0f;
            canvasGroup.DOFade(1f, 0.15f).SetEase(Ease.OutSine);
            FollowMouse();
        }

        public void Hide()
        {
            if (tooltipPanel != null && tooltipPanel.activeSelf)
            {
                canvasGroup.DOFade(0f, 0.12f).SetEase(Ease.InSine).OnComplete(() =>
                {
                    tooltipPanel.SetActive(false);
                });
            }
        }

        private void FollowMouse()
        {
            if (tooltipRect == null || canvasRect == null) return;

            Vector2 mousePos = Vector2.zero;
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            }
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out localPoint);

            // Add offset
            localPoint += offset;

            // Clamp position so tooltip stays within screen bounds
            float width = tooltipRect.rect.width;
            float height = tooltipRect.rect.height;

            float minX = canvasRect.rect.xMin + width * 0.5f;
            float maxX = canvasRect.rect.xMax - width * 0.5f;
            float minY = canvasRect.rect.yMin + height * 0.5f;
            float maxY = canvasRect.rect.yMax - height * 0.5f;

            // Offset the pivot
            Vector2 clampedPos = new Vector2(
                Mathf.Clamp(localPoint.x + width * 0.5f, minX, maxX) - width * 0.5f,
                Mathf.Clamp(localPoint.y - height * 0.5f, minY, maxY) + height * 0.5f
            );

            tooltipRect.anchoredPosition = clampedPos;
        }
    }
}
