using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject settingsPanel;

        [Header("Settings Controls")]
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private TMP_Text volumeLabel;
        [SerializeField] private Button languageButton;
        [SerializeField] private TMP_Text languageLabel;
        [SerializeField] private Toggle fullscreenToggle;

        [Header("Menu Buttons")]
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button openSettingsButton;
        [SerializeField] private Button closeSettingsButton;
        [SerializeField] private Button quitButton;

        public GameObject PausePanel => pausePanel;
        public GameObject SettingsPanel => settingsPanel;

        private float soundVolume = 1f;
        private string currentLanguage = "vi";
        private bool isPaused = false;

        public bool IsPauseOpen => pausePanel != null && pausePanel.activeSelf;
        public bool IsSettingsOpen => settingsPanel != null && settingsPanel.activeSelf;

        private void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Hook up listeners
            if (resumeButton != null) resumeButton.onClick.AddListener(ResumeGame);
            if (openSettingsButton != null) openSettingsButton.onClick.AddListener(OpenSettings);
            if (closeSettingsButton != null) closeSettingsButton.onClick.AddListener(CloseSettings);
            if (quitButton != null) quitButton.onClick.AddListener(QuitGame);

            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
                volumeSlider.value = soundVolume;
            }

            if (languageButton != null) languageButton.onClick.AddListener(ToggleLanguage);
            if (fullscreenToggle != null)
            {
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggle);
                fullscreenToggle.isOn = Screen.fullScreen;
            }
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (IsPauseOpen) ResumeGame();
            else PauseGame();
        }

        public void PauseGame()
        {
            if (pausePanel == null) return;

            isPaused = true;
            pausePanel.SetActive(true);
            pausePanel.transform.localScale = Vector3.zero;
            pausePanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);

            Time.timeScale = 0f; // Pause physics & time
            UpdateCursorState();
        }

        public void ResumeGame()
        {
            if (pausePanel == null) return;

            pausePanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                pausePanel.SetActive(false);
                if (!IsSettingsOpen)
                {
                    Time.timeScale = 1f; // Resume physics & time
                    isPaused = false;
                    UpdateCursorState();
                }
            });
        }

        private void OpenSettings()
        {
            if (settingsPanel == null) return;

            if (pausePanel != null) pausePanel.SetActive(false);

            settingsPanel.SetActive(true);
            settingsPanel.transform.localScale = Vector3.zero;
            settingsPanel.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack).SetUpdate(true);
        }

        private void CloseSettings()
        {
            if (settingsPanel == null) return;

            settingsPanel.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).SetUpdate(true).OnComplete(() =>
            {
                settingsPanel.SetActive(false);
                if (isPaused)
                {
                    if (pausePanel != null) pausePanel.SetActive(true);
                }
                else
                {
                    Time.timeScale = 1f;
                    UpdateCursorState();
                }
            });
        }

        private void OnVolumeChanged(float value)
        {
            soundVolume = value;
            if (volumeLabel != null)
            {
                volumeLabel.text = $"Âm Lượng: {Mathf.RoundToInt(value * 100)}%";
            }
            // Notify game volume manager
            AudioListener.volume = value;
        }

        private void ToggleLanguage()
        {
            currentLanguage = currentLanguage == "vi" ? "en" : "vi";
            if (languageLabel != null)
            {
                languageLabel.text = currentLanguage == "vi" ? "Ngôn Ngữ: TIẾNG VIỆT" : "Language: ENGLISH";
            }
            ChoNoi.UI.FullSimulatorUI.CurrentLanguage = currentLanguage;
            
            // Trigger refresh on all UI elements
            var hud = FindAnyObjectByType<RiverMarketHUD>();
            if (hud != null) hud.RefreshAll();
        }

        private void OnFullscreenToggle(bool isOn)
        {
            Screen.fullScreen = isOn;
        }

        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            UnityEngine.Application.Quit();
#endif
        }

        private void UpdateCursorState()
        {
            bool showCursor = IsPauseOpen || IsSettingsOpen;
            Cursor.visible = showCursor;
            Cursor.lockState = showCursor ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}
