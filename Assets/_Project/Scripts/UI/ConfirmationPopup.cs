using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class ConfirmationPopup : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private System.Action onYesCallback;
        private System.Action onNoCallback;

        private void Start()
        {
            if (yesButton != null) yesButton.onClick.AddListener(OnYesPressed);
            if (noButton != null) noButton.onClick.AddListener(OnNoPressed);
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        public void Show(string title, string description, System.Action onYes, System.Action onNo = null)
        {
            if (popupPanel == null) return;

            titleText.text = title;
            descriptionText.text = description;
            onYesCallback = onYes;
            onNoCallback = onNo;

            popupPanel.SetActive(true);
            popupPanel.transform.localScale = Vector3.zero;
            popupPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
            
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnYesPressed()
        {
            popupPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                popupPanel.SetActive(false);
                onYesCallback?.Invoke();
            });
        }

        private void OnNoPressed()
        {
            popupPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                popupPanel.SetActive(false);
                onNoCallback?.Invoke();
            });
        }
    }
}
