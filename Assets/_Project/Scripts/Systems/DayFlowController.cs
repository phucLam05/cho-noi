using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using ChoNoi.Application;
using ChoNoi.Domain;
using ChoNoi.Infrastructure;
using ChoNoi.Presentation;
using ChoNoi.Presentation.NPC;
using ChoNoi.Presentation.Player;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.UI;
using ChoNoi.UI;

namespace ChoNoi.Systems
{
    public class DayFlowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private DurabilityManager durabilityManager;
        [SerializeField] private SaveLoadManager saveLoadManager;

        [Header("Home Pier Area Settings")]
        [SerializeField] private Vector3 homePierCenter = new Vector3(104f, 3.75f, 30f);
        [SerializeField] private float homeInteractionRadius = 15f;

        private RiverMarketHUD riverMarketHUD;
        private FullSimulatorUI fullSimulatorUI;

        private float nightWarningTimer;
        private GameObject summaryPanel;
        private bool isSummaryOpen;

        private void Start()
        {
            if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
            if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
            if (durabilityManager == null) durabilityManager = FindAnyObjectByType<DurabilityManager>();
            if (saveLoadManager == null) saveLoadManager = FindAnyObjectByType<SaveLoadManager>();
            
            riverMarketHUD = FindAnyObjectByType<RiverMarketHUD>();
            fullSimulatorUI = FindAnyObjectByType<FullSimulatorUI>();

            if (timeManager != null)
            {
                timeManager.OnPhaseChanged += HandlePhaseChanged;
            }

            nightWarningTimer = 30f; // Warn every 30s
        }

        private void OnDestroy()
        {
            if (timeManager != null)
            {
                timeManager.OnPhaseChanged -= HandlePhaseChanged;
            }
        }

        private void Update()
        {
            if (timeManager == null || isSummaryOpen) return;

            // Handle night phase specifics
            if (timeManager.CurrentPhase == GamePhase.Night)
            {
                HandleNightPhase();
            }
            else
            {
                // Hide home prompt if not night
                HideHomePrompt();
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            string msg = "";
            switch (phase)
            {
                case GamePhase.Dawn:
                    msg = "Hừng đông lên rồi! Treo nông sản lên Cây Bẹo (B) để gọi khách đến mua.";
                    ResetAllNpcTradeStates();
                    break;
                case GamePhase.Day:
                    msg = "Mặt trời lên cao, chợ tan rồi. Hãy chèo vào các kênh rạch thu mua nông sản hoặc sửa ghe.";
                    break;
                case GamePhase.Dusk:
                    msg = "Chiều tà rồi. Hãy chuẩn bị dọn dẹp hàng và lái ghe về bến nhà.";
                    break;
                case GamePhase.Night:
                    msg = "Trời tối nguy hiểm! Hãy chèo ghe về Bến Nhà để ngủ nghỉ qua ngày mới.";
                    break;
            }

            ShowNotification(msg);
        }

        private void ResetAllNpcTradeStates()
        {
            var npcTargets = FindObjectsByType<NpcTradeTarget>(FindObjectsSortMode.None);
            foreach (var npc in npcTargets)
            {
                npc.HasTraded = false;
            }
            Debug.Log("[DayFlowController] Reset trade state for all NPCs.");
        }

        private void HandleNightPhase()
        {
            GameObject playerBoat = GameObject.Find("PlayerBoat");
            if (playerBoat == null) return;

            float distToHome = Vector3.Distance(playerBoat.transform.position, homePierCenter);

            if (distToHome <= homeInteractionRadius)
            {
                // Player is near home. Prompt to sleep
                ShowHomePrompt();

                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // Check if any UI is currently open. If so, don't open sleep
                    if (fullSimulatorUI != null && (fullSimulatorUI.IsDialogueOpen || fullSimulatorUI.IsMarketingOpen || fullSimulatorUI.IsYardOpen))
                    {
                        return;
                    }
                    
                    TriggerSleepSummary();
                }
            }
            else
            {
                HideHomePrompt();

                // Stamina penalty out at night
                nightWarningTimer -= Time.deltaTime;
                if (nightWarningTimer <= 0f)
                {
                    nightWarningTimer = 30f;
                    if (playerStats != null)
                    {
                        playerStats.ConsumeStamina(2f);
                        ShowNotification("Đi đêm ngoài sông lớn hao tổn sức lực! (-2 Thể lực)");

                        if (playerStats.CurrentStamina <= 0f)
                        {
                            FaintPenalty();
                        }
                    }
                }
            }
        }

        private void FaintPenalty()
        {
            ShowNotification("Bạn kiệt sức và ngất đi! Dân làng chèo ghe đưa bạn về nhà. (-5,000 VNĐ)");
            
            if (playerStats != null)
            {
                playerStats.DeductMoney(5000);
                playerStats.RestoreStamina(30f); // Restores some stamina
            }

            if (durabilityManager != null)
            {
                durabilityManager.ReduceDurability(10f); // Damage boat slightly
            }

            // Teleport boat back home
            GameObject playerBoat = GameObject.Find("PlayerBoat");
            if (playerBoat != null)
            {
                playerBoat.transform.position = new Vector3(118f, 3.75f, 34f);
                var rb = playerBoat.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }

            TriggerSleepSummary();
        }

        private void TriggerSleepSummary()
        {
            isSummaryOpen = true;
            Time.timeScale = 0f; // Pause game

            HideHomePrompt();

            // Lock controls
            var playerController = FindAnyObjectByType<ShorePlayerController>();
            if (playerController != null) playerController.CanMove = false;

            var boarding = FindAnyObjectByType<BoatBoardingController>();
            if (boarding != null) boarding.SetBoatControlActive(false);

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            BuildAndShowSummaryPanel();
        }

        private void BuildAndShowSummaryPanel()
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            summaryPanel = new GameObject("DayEndSummaryPanel", typeof(RectTransform), typeof(Image));
            summaryPanel.transform.SetParent(canvas.transform, false);

            RectTransform rt = summaryPanel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.2f, 0.15f);
            rt.anchorMax = new Vector2(0.8f, 0.85f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            summaryPanel.GetComponent<Image>().color = new Color(0.06f, 0.08f, 0.1f, 0.95f);

            // Add Border / Frame
            GameObject border = new GameObject("Border", typeof(RectTransform), typeof(Image));
            border.transform.SetParent(summaryPanel.transform, false);
            RectTransform borderRt = border.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.offsetMin = new Vector2(10f, 10f);
            borderRt.offsetMax = new Vector2(-10f, -10f);
            border.GetComponent<Image>().color = new Color(0.12f, 0.16f, 0.2f, 0.4f);

            // Font loading
            Font font = FontHelper.GameBoldFont;
            Font regularFont = FontHelper.GameFont;

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleObj.transform.SetParent(summaryPanel.transform, false);
            Text titleTxt = titleObj.GetComponent<Text>();
            titleTxt.font = font;
            titleTxt.fontSize = 42;
            titleTxt.color = new Color(0.92f, 0.82f, 0.55f, 1f); // Gold
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.text = "TỔNG KẾT NGÀY";
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.05f, 0.82f);
            titleRt.anchorMax = new Vector2(0.95f, 0.95f);
            titleRt.offsetMin = titleRt.offsetMax = Vector2.zero;

            // Content
            GameObject contentObj = new GameObject("Content", typeof(RectTransform), typeof(Text));
            contentObj.transform.SetParent(summaryPanel.transform, false);
            Text contentTxt = contentObj.GetComponent<Text>();
            contentTxt.font = regularFont;
            contentTxt.fontSize = 24;
            contentTxt.color = Color.white;
            contentTxt.alignment = TextAnchor.UpperCenter;

            int day = timeManager != null ? timeManager.CurrentDay : 1;
            string money = playerStats != null ? playerStats.CurrentMoney.ToString("N0") : "0";
            string stamina = playerStats != null ? $"{playerStats.CurrentStamina:0}/{playerStats.MaxStamina:0}" : "0/0";
            string durability = durabilityManager != null ? $"{durabilityManager.CurrentDurability:0}/{durabilityManager.MaxDurability:0}" : "0/0";

            contentTxt.text = $"\n\n<b>Kết thúc Ngày {day}</b>\n\n" +
                               $"Số dư tài khoản: <color=#ffd700><b>{money} VNĐ</b></color>\n\n" +
                               $"Độ bền ghe còn lại: <b>{durability}</b>\n\n" +
                               $"Thể lực hiện tại: <b>{stamina}</b>\n\n" +
                               $"<i>Hệ thống đã tự động lưu dữ liệu trò chơi.</i>";

            RectTransform contentRt = contentObj.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.05f, 0.25f);
            contentRt.anchorMax = new Vector2(0.95f, 0.80f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;

            // Button "Tiếp Tục"
            GameObject btnObj = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(summaryPanel.transform, false);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.35f, 0.08f);
            btnRt.anchorMax = new Vector2(0.65f, 0.18f);
            btnRt.offsetMin = btnRt.offsetMax = Vector2.zero;

            Image btnImg = btnObj.GetComponent<Image>();
            btnImg.color = new Color(0.88f, 0.44f, 0.12f, 1f); // Orange

            // Assign standard UI sprite if available
            if (fullSimulatorUI != null && fullSimulatorUI.buttonSpriteNormal != null)
            {
                btnImg.sprite = fullSimulatorUI.buttonSpriteNormal;
                btnImg.type = Image.Type.Sliced;
            }

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(ContinueToNextDay);

            GameObject btnTextObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            btnTextObj.transform.SetParent(btnObj.transform, false);
            Text btnText = btnTextObj.GetComponent<Text>();
            btnText.font = font;
            btnText.fontSize = 24;
            btnText.color = Color.white;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.text = "SANG NGÀY MỚI";
            RectTransform btnTextRt = btnTextObj.GetComponent<RectTransform>();
            btnTextRt.anchorMin = Vector2.zero;
            btnTextRt.anchorMax = Vector2.one;
            btnTextRt.offsetMin = btnTextRt.offsetMax = Vector2.zero;
        }

        private void ContinueToNextDay()
        {
            if (summaryPanel != null)
            {
                Destroy(summaryPanel);
            }

            isSummaryOpen = false;
            Time.timeScale = 1f; // Resume time

            if (timeManager != null)
            {
                timeManager.Sleep(); // advances day and triggers auto-save
            }

            // Restore some stamina automatically on sleep
            if (playerStats != null)
            {
                playerStats.RestoreStamina(80f);
            }

            // Release player movement if not on boat
            var boarding = FindAnyObjectByType<BoatBoardingController>();
            bool onBoat = boarding != null && boarding.IsBoarded;

            var playerController = FindAnyObjectByType<ShorePlayerController>();
            if (playerController != null)
            {
                playerController.CanMove = !onBoat;
            }

            if (boarding != null && onBoat)
            {
                boarding.SetBoatControlActive(true);
            }

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            ShowNotification("Ngày mới bắt đầu! Hãy chèo ghe ra ngã ba sông bán hàng.");
        }

        private void ShowHomePrompt()
        {
            if (fullSimulatorUI != null)
            {
                // We hijack or use the leftPromptPanel to show Sleep prompt
                var promptPanel = fullSimulatorUI.transform.Find("FullSimulatorCanvas/LeftPromptPanel");
                if (promptPanel != null)
                {
                    promptPanel.gameObject.SetActive(true);
                    var txt = promptPanel.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.text = "[E] Nghỉ ngơi\n(Đi Ngủ)";
                    }
                }
            }
        }

        private void HideHomePrompt()
        {
            // Only hide if left interactor is not also trying to show something
            var interactor = FindAnyObjectByType<PlayerNpcTradeInteractor>();
            if (interactor != null && interactor.CurrentTarget != null) return;

            if (fullSimulatorUI != null)
            {
                var promptPanel = fullSimulatorUI.transform.Find("FullSimulatorCanvas/LeftPromptPanel");
                if (promptPanel != null)
                {
                    var txt = promptPanel.GetComponentInChildren<Text>();
                    if (txt != null && txt.text.Contains("Nghỉ ngơi"))
                    {
                        promptPanel.gameObject.SetActive(false);
                    }
                }
            }
        }

        private void ShowNotification(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (riverMarketHUD != null)
            {
                // RiverMarketHUD has notification capability or status refresh
                Debug.Log($"[DayFlowController Notification] {message}");
            }
            
            // Expose notification popup directly onto HUD if possible, or print in console
            var hud = FindAnyObjectByType<RiverMarketHUD>();
            if (hud != null)
            {
                // Check if HUD has statusText or similar to append notification
                var statusText = hud.transform.Find("RiverMarketHUD/TopBar/StatusText")?.GetComponent<Text>();
                if (statusText != null)
                {
                    statusText.text += $" | {message}";
                }
            }
        }
    }
}
