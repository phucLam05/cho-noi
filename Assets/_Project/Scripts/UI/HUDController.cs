using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChoNoi.Application;
using ChoNoiMienTay.Presentation;
using ChoNoi.Presentation;
using ChoNoi.Presentation.Player;
using ChoNoi.Domain;
using DG.Tweening;

namespace ChoNoiMienTay.UI
{
    public class HUDController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private DurabilityManager durabilityManager;

        [Header("UI Elements - Top Left (Time & Day)")]
        [SerializeField] private TMP_Text dayText;
        [SerializeField] private TMP_Text timeText;
        [SerializeField] private TMP_Text phaseText;

        [Header("UI Elements - Top Right (Money)")]
        [SerializeField] private TMP_Text moneyText;

        [Header("UI Elements - Bottom Left (Inventory Shortcut)")]
        [SerializeField] private GameObject inventoryPrompt; // Contains keycap [B] + text

        [Header("UI Elements - Bottom Right (Interaction Prompt)")]
        [SerializeField] private GameObject interactionPrompt; // Contains keycap [E] + text
        [SerializeField] private TMP_Text interactionDescriptionText;

        [Header("Optional Stats Displays")]
        [SerializeField] private Slider staminaBar;
        [SerializeField] private TMP_Text staminaText;
        [SerializeField] private Slider durabilityBar;
        [SerializeField] private TMP_Text durabilityText;
        [SerializeField] private TMP_Text weightText;

        private int lastCachedMoney = -1;

        private void OnEnable()
        {
            if (timeManager != null)
            {
                timeManager.OnTimeChanged += HandleTimeChanged;
                timeManager.OnPhaseChanged += HandlePhaseChanged;
                timeManager.OnDayChanged += HandleDayChanged;
            }
        }

        private void OnDisable()
        {
            if (timeManager != null)
            {
                timeManager.OnTimeChanged -= HandleTimeChanged;
                timeManager.OnPhaseChanged -= HandlePhaseChanged;
                timeManager.OnDayChanged -= HandleDayChanged;
            }
        }

        private void Start()
        {
            FindReferencesIfNeeded();
            RefreshAll();
        }

        public void Initialize(PlayerStats stats, InventoryManager inv, DurabilityManager dur, TimeManager time)
        {
            playerStats = stats;
            inventoryManager = inv;
            durabilityManager = dur;
            timeManager = time;

            if (timeManager != null)
            {
                timeManager.OnTimeChanged -= HandleTimeChanged;
                timeManager.OnPhaseChanged -= HandlePhaseChanged;
                timeManager.OnDayChanged -= HandleDayChanged;

                timeManager.OnTimeChanged += HandleTimeChanged;
                timeManager.OnPhaseChanged += HandlePhaseChanged;
                timeManager.OnDayChanged += HandleDayChanged;
            }

            RefreshAll();
        }

        private void Update()
        {
            // Regularly update stats like Money, Stamina, Weight, Durability
            UpdateStats();

            var boarding = FindAnyObjectByType<BoatBoardingController>();
            bool isBoarded = boarding != null && boarding.IsBoarded;
            if (inventoryPrompt != null)
            {
                inventoryPrompt.SetActive(isBoarded);
            }
        }

        private void FindReferencesIfNeeded()
        {
            if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
            if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
            if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
            if (durabilityManager == null) durabilityManager = FindAnyObjectByType<DurabilityManager>();
        }

        public void RefreshAll()
        {
            HandleDayChanged(timeManager != null ? timeManager.CurrentDay : 1);
            HandleTimeChanged(timeManager != null ? timeManager.CurrentHour : 3, timeManager != null ? timeManager.CurrentMinute : 0);
            HandlePhaseChanged(timeManager != null ? timeManager.CurrentPhase : GamePhase.Dawn);
            UpdateStats(true);
        }

        private void HandleTimeChanged(int hour, int minute)
        {
            if (timeText != null)
            {
                timeText.text = $"{hour:00}:{minute:00}";
            }
        }

        private void HandlePhaseChanged(GamePhase phase)
        {
            if (phaseText != null)
            {
                bool isEn = false; // Fallback
                string phaseName = "Bình Minh";
                switch (phase)
                {
                    case GamePhase.Dawn: phaseName = isEn ? "Dawn" : "Bình Minh"; break;
                    case GamePhase.Day: phaseName = isEn ? "Daytime" : "Ban Ngày"; break;
                    case GamePhase.Dusk: phaseName = isEn ? "Dusk" : "Chiều Tà"; break;
                    case GamePhase.Night: phaseName = isEn ? "Night" : "Ban Đêm"; break;
                }
                phaseText.text = phaseName;
            }
        }

        private void HandleDayChanged(int day)
        {
            if (dayText != null)
            {
                dayText.text = $"Ngày {day}";
            }
        }

        private void UpdateStats(bool force = false)
        {
            FindReferencesIfNeeded();
            
            if (playerStats != null)
            {
                // Update money with a count-up animation if changed
                int currentMoney = playerStats.CurrentMoney;
                if (currentMoney != lastCachedMoney || force)
                {
                    if (moneyText != null)
                    {
                        if (lastCachedMoney == -1 || force)
                        {
                            moneyText.text = $"{currentMoney:N0} VNĐ";
                            lastCachedMoney = currentMoney;
                        }
                        else
                        {
                            // Animate money counting up/down using DOTween
                            int startMoney = lastCachedMoney;
                            DOTween.To(() => startMoney, x => {
                                startMoney = x;
                                moneyText.text = $"{startMoney:N0} VNĐ";
                            }, currentMoney, 0.5f).SetEase(Ease.OutQuad);
                            
                            // Punch scale money text for feedback
                            moneyText.transform.DOPunchScale(Vector3.one * 0.15f, 0.3f, 5, 1f);
                            lastCachedMoney = currentMoney;
                        }
                    }
                }

                // Update Stamina
                if (staminaBar != null)
                {
                    staminaBar.maxValue = playerStats.MaxStamina;
                    staminaBar.value = playerStats.CurrentStamina;
                }
                if (staminaText != null)
                {
                    staminaText.text = $"{playerStats.CurrentStamina:0}/{playerStats.MaxStamina:0}";
                }
            }

            // Update Durability
            if (durabilityManager != null)
            {
                if (durabilityBar != null)
                {
                    durabilityBar.maxValue = durabilityManager.MaxDurability;
                    durabilityBar.value = durabilityManager.CurrentDurability;
                }
                if (durabilityText != null)
                {
                    durabilityText.text = $"{durabilityManager.CurrentDurability:0}/{durabilityManager.MaxDurability:0}";
                }
            }

            // Update Weight
            if (inventoryManager != null && weightText != null)
            {
                weightText.text = $"{inventoryManager.CurrentTotalWeight:0}/{inventoryManager.MaxWeightCapacity:0} kg";
            }
        }

        /// <summary>
        /// Show or hide contextual interaction prompts in the bottom right corner (e.g. "[E] Trò chuyện")
        /// </summary>
        public void SetInteractionPrompt(bool active, string description = "")
        {
            if (interactionPrompt != null)
            {
                if (active)
                {
                    if (interactionDescriptionText != null)
                    {
                        interactionDescriptionText.text = description;
                    }
                    if (!interactionPrompt.activeSelf)
                    {
                        interactionPrompt.SetActive(true);
                        interactionPrompt.transform.localScale = Vector3.zero;
                        interactionPrompt.transform.DOScale(Vector3.one, 0.25f).SetEase(Ease.OutBack);
                    }
                }
                else
                {
                    if (interactionPrompt.activeSelf)
                    {
                        interactionPrompt.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack).OnComplete(() => {
                            interactionPrompt.SetActive(false);
                        });
                    }
                }
            }
        }
    }
}
