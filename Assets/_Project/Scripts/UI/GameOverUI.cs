using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button mainMenuButton;

        public bool IsOpen => gameOverPanel != null && gameOverPanel.activeSelf;

        private void Start()
        {
            if (retryButton != null) retryButton.onClick.AddListener(OnRetryPressed);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OnMainMenuPressed);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
        }

        public void Show(string message = "Bạn không còn đủ tiền để bảo trì ghe của mình nữa.")
        {
            if (gameOverPanel == null) return;

            if (messageText != null)
            {
                messageText.text = message;
            }

            gameOverPanel.SetActive(true);
            gameOverPanel.transform.localScale = Vector3.zero;
            gameOverPanel.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack).SetUpdate(true);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            Time.timeScale = 0f; // Freeze game
        }

        private void OnRetryPressed()
        {
            gameOverPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                gameOverPanel.SetActive(false);
                Time.timeScale = 1f;
                
                // Trigger reload of the current scene
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
        }

        private void OnMainMenuPressed()
        {
            gameOverPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                gameOverPanel.SetActive(false);
                Time.timeScale = 1f;
                
                // Load main menu scene or reload scene for now since it's a prototype
                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            });
        }
    }
}
