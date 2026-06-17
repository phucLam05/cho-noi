using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using ChoNoi.Application;
using ChoNoi.Domain;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.Infrastructure;
using ChoNoi.Presentation;
using System.Collections.Generic;

namespace ChoNoiMienTay.UI
{
    public class RiverMarketHUD : MonoBehaviour
    {
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private BoatCampManager boatCampManager;
        [SerializeField] private MarketNewsController marketNewsController;
        [SerializeField] private EconomyManager economyManager;
        [SerializeField] private DurabilityManager durabilityManager;
        [SerializeField] private List<ItemData> marketItems = new List<ItemData>();

        private Canvas canvas;
        private Text statsText;
        private Text timeText;
        private Text upgradeText;
        private Text newsText;
        private Text newsNpcNameText;
        private Text tradeText;
        private Text tradeTitleText;
        private Image newsAvatarImage;
        private GameObject upgradePanel;
        private GameObject tradePanel;
        private GameObject newsPanel;
        private int selectedMarketIndex;
        private bool isNpcTradeOpen;
        private string currentTradeNpcName = "Thuong Lai";

        public bool IsNpcTradeOpen => isNpcTradeOpen;

        public void Configure(
            TimeManager timeSource,
            PlayerStats player,
            InventoryManager inventory,
            BoatCampManager camp,
            MarketNewsController newsController,
            EconomyManager economy,
            DurabilityManager durability,
            List<ItemData> items)
        {
            timeManager = timeSource;
            playerStats = player;
            inventoryManager = inventory;
            boatCampManager = camp;
            marketNewsController = newsController;
            economyManager = economy;
            durabilityManager = durability;
            marketItems = items ?? new List<ItemData>();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Start()
        {
            if (timeManager == null) timeManager = FindAnyObjectByType<TimeManager>();
            if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
            if (inventoryManager == null) inventoryManager = FindAnyObjectByType<InventoryManager>();
            if (boatCampManager == null) boatCampManager = FindAnyObjectByType<BoatCampManager>();
            if (marketNewsController == null) marketNewsController = FindAnyObjectByType<MarketNewsController>();
            if (economyManager == null) economyManager = FindAnyObjectByType<EconomyManager>();
            if (durabilityManager == null) durabilityManager = FindAnyObjectByType<DurabilityManager>();

            BuildUIIfNeeded();
            SubscribeEvents();
            RefreshAll();
        }

        private void SubscribeEvents()
        {
            if (timeManager != null)
            {
                timeManager.OnTimeChanged -= HandleTimeChanged;
                timeManager.OnTimeChanged += HandleTimeChanged;
                timeManager.OnPhaseChanged -= HandlePhaseChanged;
                timeManager.OnPhaseChanged += HandlePhaseChanged;
                timeManager.OnDayChanged -= HandleDayChanged;
                timeManager.OnDayChanged += HandleDayChanged;
            }

            if (marketNewsController != null)
            {
                marketNewsController.OnNewsChanged -= HandleNewsChanged;
                marketNewsController.OnNewsChanged += HandleNewsChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (timeManager != null)
            {
                timeManager.OnTimeChanged -= HandleTimeChanged;
                timeManager.OnPhaseChanged -= HandlePhaseChanged;
                timeManager.OnDayChanged -= HandleDayChanged;
            }

            if (marketNewsController != null)
            {
                marketNewsController.OnNewsChanged -= HandleNewsChanged;
            }
        }

        private void BuildUIIfNeeded()
        {
            if (canvas != null) return;

            EnsureEventSystem();

            GameObject canvasObject = new GameObject("RiverMarketHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            GameObject topBar = CreatePanel("TopBar", canvasObject.transform, new Color(0f, 0f, 0f, 0.65f));
            Stretch(topBar.GetComponent<RectTransform>(), new Vector2(0.02f, 0.90f), new Vector2(0.98f, 0.98f));
            
            statsText = CreateText("StatsText", topBar.transform, 24, TextAnchor.MiddleLeft);
            Stretch(statsText.rectTransform, new Vector2(0.02f, 0.15f), new Vector2(0.60f, 0.85f));

            timeText = CreateText("TimeText", topBar.transform, 24, TextAnchor.MiddleRight);
            Stretch(timeText.rectTransform, new Vector2(0.70f, 0.15f), new Vector2(0.98f, 0.85f));

            upgradePanel = CreatePanel("UpgradePanel", canvasObject.transform, new Color(0.08f, 0.16f, 0.18f, 0.86f));
            Stretch(upgradePanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.08f), new Vector2(0.26f, 0.86f));
            CreateText("UpgradeTitle", upgradePanel.transform, 26, TextAnchor.MiddleCenter).text = "TRAI GHE XOM NUOC";
            Stretch(upgradePanel.transform.Find("UpgradeTitle").GetComponent<RectTransform>(), new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f));

            upgradeText = CreateText("UpgradeText", upgradePanel.transform, 22, TextAnchor.UpperLeft);
            Stretch(upgradeText.rectTransform, new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.82f));

            CreateActionButton(upgradePanel.transform, "Khoang Chua", new Vector2(0.08f, 0.33f), new Vector2(0.92f, 0.41f), () =>
            {
                if (boatCampManager != null && boatCampManager.TryBuyNextStorageUpgrade()) RefreshAll();
            });
            CreateActionButton(upgradePanel.transform, "Dong Co", new Vector2(0.08f, 0.23f), new Vector2(0.92f, 0.31f), () =>
            {
                if (boatCampManager != null && boatCampManager.TryBuyNextEngineUpgrade()) RefreshAll();
            });
            CreateActionButton(upgradePanel.transform, "Lop Mai", new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.21f), () =>
            {
                if (boatCampManager != null && boatCampManager.TryBuyRoofUpgrade()) RefreshAll();
            });
            CreateActionButton(upgradePanel.transform, "Cay Beo", new Vector2(0.08f, 0.03f), new Vector2(0.92f, 0.11f), () =>
            {
                if (boatCampManager != null && boatCampManager.TryBuyNextBambooPoleUpgrade()) RefreshAll();
            });

            tradePanel = CreatePanel("TradePanel", canvasObject.transform, new Color(0.12f, 0.12f, 0.08f, 0.88f));
            Stretch(tradePanel.GetComponent<RectTransform>(), new Vector2(0.31f, 0.08f), new Vector2(0.69f, 0.44f));
            tradeTitleText = CreateText("TradeTitle", tradePanel.transform, 24, TextAnchor.MiddleCenter);
            tradeTitleText.text = "CHO NOI TRUNG TAM";
            Stretch(tradeTitleText.rectTransform, new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f));

            tradeText = CreateText("TradeText", tradePanel.transform, 20, TextAnchor.UpperLeft);
            Stretch(tradeText.rectTransform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.82f));

            CreateActionButton(tradePanel.transform, "Mat Hang Truoc", new Vector2(0.08f, 0.34f), new Vector2(0.44f, 0.42f), SelectPreviousItem);
            CreateActionButton(tradePanel.transform, "Mat Hang Sau", new Vector2(0.56f, 0.34f), new Vector2(0.92f, 0.42f), SelectNextItem);
            CreateActionButton(tradePanel.transform, "Mua 1", new Vector2(0.08f, 0.22f), new Vector2(0.44f, 0.30f), BuySelectedItem);
            CreateActionButton(tradePanel.transform, "Ban 1", new Vector2(0.56f, 0.22f), new Vector2(0.92f, 0.30f), SellSelectedItem);
            CreateActionButton(tradePanel.transform, "Sua Ghe", new Vector2(0.08f, 0.10f), new Vector2(0.44f, 0.18f), RepairBoat);
            CreateActionButton(tradePanel.transform, "Hoi The Luc", new Vector2(0.56f, 0.10f), new Vector2(0.92f, 0.18f), RestoreStamina);

            newsPanel = CreatePanel("NewsPanel", canvasObject.transform, new Color(0.14f, 0.10f, 0.06f, 0.88f));
            Stretch(newsPanel.GetComponent<RectTransform>(), new Vector2(0.72f, 0.08f), new Vector2(0.98f, 0.44f));
            CreateText("NewsTitle", newsPanel.transform, 24, TextAnchor.MiddleCenter).text = "TIN DON THI TRUONG";
            Stretch(newsPanel.transform.Find("NewsTitle").GetComponent<RectTransform>(), new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.96f));

            newsAvatarImage = CreateImage("NpcAvatar", newsPanel.transform);
            Stretch(newsAvatarImage.rectTransform, new Vector2(0.06f, 0.46f), new Vector2(0.34f, 0.82f));

            newsNpcNameText = CreateText("NpcName", newsPanel.transform, 20, TextAnchor.MiddleCenter);
            Stretch(newsNpcNameText.rectTransform, new Vector2(0.04f, 0.38f), new Vector2(0.36f, 0.46f));

            newsText = CreateText("NewsText", newsPanel.transform, 20, TextAnchor.UpperLeft);
            Stretch(newsText.rectTransform, new Vector2(0.40f, 0.10f), new Vector2(0.94f, 0.82f));

            CreateActionButton(newsPanel.transform, "Ngu Den Sang", new Vector2(0.06f, 0.08f), new Vector2(0.34f, 0.18f), () =>
            {
                if (timeManager != null)
                {
                    timeManager.Sleep();
                    RefreshAll();
                }
            });

            SetPanelVisible(upgradePanel, false);
            SetPanelVisible(tradePanel, false);
            SetPanelVisible(newsPanel, false);
        }

        private void HandleTimeChanged(int _, int __) => RefreshStatus();
        private void HandlePhaseChanged(GamePhase _) => RefreshStatus();
        private void HandleDayChanged(int _) => RefreshAll();
        private void HandleNewsChanged(Infrastructure.MarketNewsEntry _) => RefreshNews();

        private void RefreshAll()
        {
            RefreshStatus();
            RefreshUpgrades();
            RefreshTrade();
            RefreshNews();
        }

        private void RefreshStatus()
        {
            UpdateTopBarText();
        }

        private void UpdateTopBarText()
        {
            if (statsText == null || timeText == null) return;

            string money = playerStats != null ? playerStats.CurrentMoney.ToString("N0") : "0";
            string stamina = playerStats != null ? $"{playerStats.CurrentStamina:0}/{playerStats.MaxStamina:0}" : "0/0";
            string durability = durabilityManager != null ? $"{durabilityManager.CurrentDurability:0}/{durabilityManager.MaxDurability:0}" : "0/0";
            
            float weight = inventoryManager != null ? inventoryManager.CurrentTotalWeight : 0f;
            float maxWeight = inventoryManager != null ? inventoryManager.MaxWeightCapacity : 100f;

            string dayPhase = timeManager != null ? $"Ngay {timeManager.CurrentDay} | {timeManager.CurrentPhase}" : "Ngay 1 | Dawn";

            statsText.text = $"Tien: {money} | The luc: {stamina} | Tai trong: {weight:0}/{maxWeight:0} kg | Do ben: {durability}";
            timeText.text = $"{dayPhase}";
        }

        private void RefreshUpgrades()
        {
            if (upgradeText == null || boatCampManager == null || boatCampManager.UpgradeCatalog == null) return;

            Infrastructure.StorageUpgradeTier storageTier = boatCampManager.UpgradeCatalog.GetStorageTier(boatCampManager.StorageLevel);
            Infrastructure.EngineUpgradeTier engineTier = boatCampManager.UpgradeCatalog.GetEngineTier(boatCampManager.engineLevel);
            Infrastructure.BambooPoleUpgradeTier bambooTier = boatCampManager.UpgradeCatalog.GetBambooPoleTier(boatCampManager.bambooPoleLevel);
            Infrastructure.RoofUpgradeTier roofTier = boatCampManager.UpgradeCatalog.GetRoofTier(boatCampManager.hasRoof ? 1 : 0);

            upgradeText.text =
                $"Khoang: Cap {boatCampManager.StorageLevel} | Tiep: {(storageTier != null ? storageTier.costMoney.ToString("N0") : "MAX")}\n" +
                $"Dong co: Cap {boatCampManager.engineLevel} | Tiep: {(engineTier != null ? engineTier.costMoney.ToString("N0") : "MAX")}\n" +
                $"Mai che: {(boatCampManager.hasRoof ? "Da lap" : (roofTier != null ? roofTier.costMoney.ToString("N0") : "MAX"))}\n" +
                $"Cay Beo: Cap {boatCampManager.bambooPoleLevel} | Tiep: {(bambooTier != null ? bambooTier.costMoney.ToString("N0") : "MAX")}";
        }

        private void RefreshTrade()
        {
            if (tradeText == null)
                return;

            ItemData currentItem = GetSelectedMarketItem();
            if (currentItem == null)
            {
                tradeText.text = "Chua co du lieu hang hoa de giao dich.";
                return;
            }

            int currentOwned = 0;
            if (inventoryManager != null && inventoryManager.Inventory.TryGetValue(currentItem, out int amount))
            {
                currentOwned = amount;
            }

            int buyPrice = currentItem.basePrice;
            int sellPrice = Mathf.RoundToInt(currentItem.basePrice * GetSaleMultiplier(currentItem));
            int repairCost = GetRepairCost();
            tradeText.text =
                $"{currentItem.itemName} ({currentItem.itemID})\n" +
                $"Gia mua: {buyPrice:N0} | Gia ban: {sellPrice:N0}\n" +
                $"Ton kho: {currentOwned} | Nang: {currentItem.weight:0.0} kg\n" +
                $"Sua ghe: {repairCost:N0} | The luc hoi: 8,000";
        }

        private void RefreshNews()
        {
            if (newsText == null) return;

            Infrastructure.MarketNewsEntry entry = marketNewsController != null ? marketNewsController.CurrentNews : null;
            if (entry == null)
            {
                newsNpcNameText.text = "Khong co NPC";
                newsAvatarImage.sprite = null;
                newsAvatarImage.color = new Color(1f, 1f, 1f, 0.15f);
                newsText.text = "Chua co tin don thi truong duoc cap nhat.";
                return;
            }

            newsNpcNameText.text = entry.npcName;
            newsAvatarImage.sprite = entry.npcAvatar;
            newsAvatarImage.color = Color.white;
            newsText.text = $"{entry.headline}\n\n{entry.marketRumor}";
        }

        private ItemData GetSelectedMarketItem()
        {
            if (marketItems == null || marketItems.Count == 0)
            {
                return null;
            }

            selectedMarketIndex = Mathf.Clamp(selectedMarketIndex, 0, marketItems.Count - 1);
            return marketItems[selectedMarketIndex];
        }

        private void SelectPreviousItem()
        {
            if (marketItems == null || marketItems.Count == 0) return;
            selectedMarketIndex = (selectedMarketIndex - 1 + marketItems.Count) % marketItems.Count;
            RefreshTrade();
        }

        private void SelectNextItem()
        {
            if (marketItems == null || marketItems.Count == 0) return;
            selectedMarketIndex = (selectedMarketIndex + 1) % marketItems.Count;
            RefreshTrade();
        }

        private void BuySelectedItem()
        {
            ItemData currentItem = GetSelectedMarketItem();
            if (currentItem != null && economyManager != null && inventoryManager != null)
            {
                economyManager.BuyItemToInventory(currentItem, 1, inventoryManager);
                RefreshAll();
            }
        }

        private void SellSelectedItem()
        {
            ItemData currentItem = GetSelectedMarketItem();
            if (currentItem == null || economyManager == null || inventoryManager == null)
                return;

            int finalRevenue = Mathf.RoundToInt(currentItem.basePrice * GetSaleMultiplier(currentItem));
            economyManager.SellItemWholesale(currentItem, 1, inventoryManager, finalRevenue);
            RefreshAll();
        }

        private void RepairBoat()
        {
            if (economyManager == null || durabilityManager == null)
                return;

            ServiceData repairService = ScriptableObject.CreateInstance<ServiceData>();
            repairService.serviceName = "Sua Ghe";
            repairService.costMoney = GetRepairCost();
            repairService.durabilityRestoreAmount = 18f;
            economyManager.BuyService(repairService);
            Destroy(repairService);
            RefreshAll();
        }

        private void RestoreStamina()
        {
            if (economyManager == null)
                return;

            ServiceData mealService = ScriptableObject.CreateInstance<ServiceData>();
            mealService.serviceName = "Bun Nuoc Leo";
            mealService.costMoney = 8000;
            mealService.staminaRestoreAmount = 18f;
            economyManager.BuyService(mealService);
            Destroy(mealService);
            RefreshAll();
        }

        private int GetRepairCost()
        {
            if (durabilityManager == null)
                return 12000;

            float missingDurability = durabilityManager.MaxDurability - durabilityManager.CurrentDurability;
            return 6000 + Mathf.RoundToInt(missingDurability * 220f);
        }

        private float GetSaleMultiplier(ItemData item)
        {
            float multiplier = 1.05f + Mathf.Min(0.18f, (timeManager != null ? (timeManager.CurrentDay - 1) : 0) * 0.04f);
            string rumor = marketNewsController != null && marketNewsController.CurrentNews != null
                ? marketNewsController.CurrentNews.marketRumor.ToLowerInvariant()
                : string.Empty;

            string itemName = item != null && !string.IsNullOrEmpty(item.itemName)
                ? item.itemName.ToLowerInvariant()
                : string.Empty;

            if (!string.IsNullOrEmpty(itemName) && rumor.Contains(itemName))
            {
                multiplier += 0.12f;
            }

            return multiplier;
        }

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;

            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
            eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return panel;
        }

        private Text CreateText(string name, Transform parent, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private Image CreateImage(string name, Transform parent)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.color = new Color(1f, 1f, 1f, 0.15f);
            return image;
        }

        private void CreateActionButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Stretch(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
            buttonObject.GetComponent<Image>().color = new Color(0.88f, 0.71f, 0.34f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            Text text = CreateText("Label", buttonObject.transform, 20, TextAnchor.MiddleCenter);
            text.text = label;
            text.color = new Color(0.10f, 0.08f, 0.05f, 1f);
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
        }

        private void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        public void OpenUpgradePanel()
        {
            CloseAllPanels();
            SetPanelVisible(upgradePanel, true);
        }

        public void OpenNewsPanel()
        {
            CloseAllPanels();
            SetPanelVisible(newsPanel, true);
        }

        public void CloseAllPanels()
        {
            SetPanelVisible(upgradePanel, false);
            SetPanelVisible(tradePanel, false);
            SetPanelVisible(newsPanel, false);
            isNpcTradeOpen = false;
        }

        private void ToggleUpgradePanel()
        {
            bool wasActive = upgradePanel != null && upgradePanel.activeSelf;
            CloseAllPanels();
            if (!wasActive) SetPanelVisible(upgradePanel, true);
        }

        private void ToggleTradePanel()
        {
            bool wasActive = tradePanel != null && tradePanel.activeSelf;
            CloseAllPanels();
            if (!wasActive) SetPanelVisible(tradePanel, true);
        }

        private void ToggleNewsPanel()
        {
            bool wasActive = newsPanel != null && newsPanel.activeSelf;
            CloseAllPanels();
            if (!wasActive) SetPanelVisible(newsPanel, true);
        }

        public void OpenNpcTrade(string npcName)
        {
            currentTradeNpcName = string.IsNullOrWhiteSpace(npcName) ? "Thuong Lai" : npcName;
            isNpcTradeOpen = true;
            if (tradeTitleText != null)
                tradeTitleText.text = $"GIAO DICH - {currentTradeNpcName.ToUpperInvariant()}";
            SetPanelVisible(tradePanel, true);
            RefreshTrade();
        }

        public void CloseNpcTrade()
        {
            isNpcTradeOpen = false;
            if (tradeTitleText != null)
                tradeTitleText.text = "CHO NOI TRUNG TAM";
            SetPanelVisible(tradePanel, false);
        }

        private void SetPanelVisible(GameObject panel, bool isVisible)
        {
            if (panel != null)
                panel.SetActive(isVisible);
        }
    }
}
