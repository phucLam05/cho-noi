using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using ChoNoi.Application;
using ChoNoi.Infrastructure;
using ChoNoi.Presentation;
using ChoNoi.Presentation.NPC;
using ChoNoi.Presentation.Player;
using UnityEngine.InputSystem.UI;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.Systems;
using ChoNoiMienTay.UI;
using ChoNoiMienTay.Data;

namespace ChoNoi.UI
{
    public class FullSimulatorUI : MonoBehaviour
    {
        [Header("Managers")]
        public BambooPoleManager bambooPoleManager;
        public InventoryManager inventoryManager;
        public RiverMarketHUD riverMarketHUD;
        public PlayerStats playerStats;
        public BoatCampManager boatCampManager;
        public BoatBoardingController boatBoardingController;

        private GameObject canvasObject;
        private GameObject tutorialPanel;
        private GameObject marketingPanel;
        private GameObject dialoguePanel;
        private GameObject settingsPanel;

        private Text marketingText;
        private Text dialogueText;
        private Text npcNameText;
        private Image npcAvatar;
        private Image playerAvatar;
        private GameObject choicePanel;
        private List<GameObject> choiceButtons = new List<GameObject>();

        // Side Prompts HUD
        private GameObject leftPromptPanel;
        private GameObject rightPromptPanel;
        private Text leftPromptText;
        private Text rightPromptText;

        // UI grids for drag and drop Cây Bẹo
        private GameObject inventoryGridParent;
        private GameObject poleSlotsParent;

        // Tab and Drag fields
        public GameObject activeDragObject;
        private GameObject cayBeoTabContent;
        private GameObject khoangThuyenTabContent;
        private GameObject khoangThuyenListParent;
        private int activeCargoTab = 0;
        private Button tabButton1;
        private Button tabButton2;

        // Cargo selection
        private int selectedCargoSlotIndex = -1;

        // Dialogue State Machine
        private enum DialogueState
        {
            Closed,
            MerchantGreeting,
            MerchantSelectQuantity,
            MerchantBargaining,
            VendorGreeting,
            VendorTrading,
            VendorNews,
            GardenerGreeting,
            GardenerTrading,
            UpgradeCampGreeting
        }
        private DialogueState dialogueState = DialogueState.Closed;
        private NpcTradeTarget activeNpc;
        private ItemData selectedBargainItem;
        private int bargainQuantity = 1;
        private bool justFinishedTrade = false;
        private bool isSettingUpSlider = false;

        // Trade Quantity Panel
        private GameObject tradeQuantityPanel;
        private Text tradeTitleText;
        private Text tradeItemInfoText;
        private Slider tradeQtySlider;
        private InputField tradeQtyInputField;
        private Text tradeSummaryText;
        private Button tradeConfirmButton;
        private Button tradeCancelButton;
        private Button tradeMaxButton;

        private ItemData currentTradeItem;
        private bool isBuying;
        private bool isWholesale;
        private bool isPriceMode;
        private int maxTradeQty;
        private int selectedTradeQty = 1;
        private int selectedTradePrice = 1000;
        private int minTradePrice = 500;
        private int maxTradePrice = 10000;

        // Upgrade & Maintenance Panel (Trại Ghe)
        private GameObject boatYardPanel;
        private Text yardTitleText;
        private Text yardDurabilityText;
        private Slider yardDurabilitySlider;
        private Button yardRepairButton;
        
        private Text upgradeStorageText;
        private Text upgradeEngineText;
        private Text upgradeRoofText;
        private Text upgradeBambooText;
        private Button upgradeStorageButton;
        private Button upgradeEngineButton;
        private Button upgradeRoofButton;
        private Button upgradeBambooButton;

        // Drag and drop / click-to-place helper state
        private ItemData selectedInventoryItemForPole;
        private readonly List<GameObject> createdUIElements = new List<GameObject>();

        public bool IsDialogueOpen => dialoguePanel != null && dialoguePanel.activeSelf;

        private void Start()
        {
            if (bambooPoleManager == null)
            {
                var boat = GameObject.Find("PlayerBoat");
                if (boat != null)
                    bambooPoleManager = boat.GetComponent<BambooPoleManager>();
                if (bambooPoleManager == null)
                    bambooPoleManager = FindAnyObjectByType<BambooPoleManager>();
            }
            if (inventoryManager == null)
                inventoryManager = FindAnyObjectByType<InventoryManager>();
            if (riverMarketHUD == null)
                riverMarketHUD = FindAnyObjectByType<RiverMarketHUD>();
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();
            if (boatCampManager == null)
                boatCampManager = FindAnyObjectByType<BoatCampManager>();
            if (boatBoardingController == null)
                boatBoardingController = FindAnyObjectByType<BoatBoardingController>();

            var timeManager = FindAnyObjectByType<TimeManager>();
            if (timeManager != null)
            {
                timeManager.OnDayChanged += HandleDayChanged;
            }

            BuildExtraUI();
        }

        private void Update()
        {
            // Detect if player is boarded
            bool isBoarded = boatBoardingController != null && boatBoardingController.IsBoarded;

            // B key to toggle marketing panel, only available when boarded on the boat and dialogue is closed
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                if (isBoarded && !IsDialogueOpen)
                {
                    ToggleMarketing();
                }
            }

            // Escape key closes dialogue or marketing
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (marketingPanel != null && marketingPanel.activeSelf)
                {
                    CloseMarketingPanel();
                }
            }

            UpdateSidePrompts(isBoarded);
        }

        private void BuildExtraUI()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvasObject = new GameObject("FullSimulatorCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }
            else
            {
                canvasObject = canvas.gameObject;
            }

            EnsureEventSystem();

            // Top right buttons for new features
            CreateActionButton(canvasObject.transform, "Huong Dan", new Vector2(0.66f, 0.90f), new Vector2(0.73f, 0.96f), ToggleTutorial);
            CreateActionButton(canvasObject.transform, "Cai Dat", new Vector2(0.58f, 0.90f), new Vector2(0.65f, 0.96f), ToggleSettings);

            // Left & Right Prompts Panel (Middle-sides)
            leftPromptPanel = CreatePanel("LeftPromptPanel", canvasObject.transform, new Color(0.08f, 0.08f, 0.08f, 0.85f));
            Stretch(leftPromptPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.45f), new Vector2(0.18f, 0.52f));
            leftPromptText = CreateText("PromptText", leftPromptPanel.transform, 18, TextAnchor.MiddleCenter);
            Stretch(leftPromptText.rectTransform, Vector2.zero, Vector2.one);
            leftPromptPanel.SetActive(false);

            rightPromptPanel = CreatePanel("RightPromptPanel", canvasObject.transform, new Color(0.08f, 0.08f, 0.08f, 0.85f));
            Stretch(rightPromptPanel.GetComponent<RectTransform>(), new Vector2(0.82f, 0.45f), new Vector2(0.98f, 0.52f));
            rightPromptText = CreateText("PromptText", rightPromptPanel.transform, 18, TextAnchor.MiddleCenter);
            Stretch(rightPromptText.rectTransform, Vector2.zero, Vector2.one);
            rightPromptPanel.SetActive(false);

            // Tutorial Panel
            tutorialPanel = CreatePanel("TutorialPanel", canvasObject.transform, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            Stretch(tutorialPanel.GetComponent<RectTransform>(), new Vector2(0.2f, 0.2f), new Vector2(0.8f, 0.8f));
            CreateText("Title", tutorialPanel.transform, 32, TextAnchor.MiddleCenter).text = "HUONG DAN CHOI";
            Stretch(tutorialPanel.transform.Find("Title").GetComponent<RectTransform>(), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f));
            Text tutText = CreateText("Body", tutorialPanel.transform, 24, TextAnchor.UpperLeft);
            tutText.text = "1. Binh Minh (3AM - 10AM): Lai ghe ra cho, treo hang len Cay Beo de ban.\n\n" +
                           "2. Tra Gia: Su dung the luc de Noi Ngot hoac Ton hang de Tang Qua.\n\n" +
                           "3. Chieu Ta (1PM - 6PM): Vao rach nho thu mua nong san hoac ve Trai Ghe de bao tri.\n\n" +
                           "4. Nang Cap: Mo rong khoang chua, nang cap dong co de ghe chay nhanh hon.";
            Stretch(tutText.rectTransform, new Vector2(0.05f, 0.15f), new Vector2(0.95f, 0.80f));
            CreateActionButton(tutorialPanel.transform, "Dong", new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.12f), ToggleTutorial);
            tutorialPanel.SetActive(false);

            // Settings Panel
            settingsPanel = CreatePanel("SettingsPanel", canvasObject.transform, new Color(0.1f, 0.1f, 0.2f, 0.95f));
            Stretch(settingsPanel.GetComponent<RectTransform>(), new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));
            CreateText("Title", settingsPanel.transform, 32, TextAnchor.MiddleCenter).text = "CAI DAT";
            Stretch(settingsPanel.transform.Find("Title").GetComponent<RectTransform>(), new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f));
            CreateActionButton(settingsPanel.transform, "Am Thanh: ON", new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.65f), () => {});
            CreateActionButton(settingsPanel.transform, "Do Hoa: CAO", new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.45f), () => {});
            CreateActionButton(settingsPanel.transform, "Dong", new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.2f), ToggleSettings);
            settingsPanel.SetActive(false);

            // Marketing Panel (Cargo & Cay Beo) - Stretched side-by-side drag and drop UI with tabs
            marketingPanel = CreatePanel("MarketingPanel", canvasObject.transform, new Color(0.12f, 0.18f, 0.14f, 0.96f));
            Stretch(marketingPanel.GetComponent<RectTransform>(), new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f));
            
            CreateText("Title", marketingPanel.transform, 28, TextAnchor.MiddleCenter).text = "QUAN LY KHOANG THUYEN & CAY BEO";
            Stretch(marketingPanel.transform.Find("Title").GetComponent<RectTransform>(), new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));

            // Create Tab Buttons at the top (below title)
            tabButton1 = CreateActionButton(marketingPanel.transform, "KHOANG THUYEN", new Vector2(0.20f, 0.81f), new Vector2(0.48f, 0.88f), () => SetCargoTab(0));
            tabButton2 = CreateActionButton(marketingPanel.transform, "CAY BEO", new Vector2(0.52f, 0.81f), new Vector2(0.80f, 0.88f), () => SetCargoTab(1));

            // 1. Khoang Thuyen Tab Content
            khoangThuyenTabContent = CreatePanel("KhoangThuyenTabContent", marketingPanel.transform, new Color(0.08f, 0.10f, 0.12f, 0.9f));
            Stretch(khoangThuyenTabContent.GetComponent<RectTransform>(), new Vector2(0.04f, 0.12f), new Vector2(0.96f, 0.80f));

            CreateText("CargoHeader", khoangThuyenTabContent.transform, 22, TextAnchor.MiddleCenter).text = "DANH SACH NONG SAN TRONG KHOANG GHE";
            Stretch(khoangThuyenTabContent.transform.Find("CargoHeader").GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));

            khoangThuyenListParent = new GameObject("CargoListGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            var cargoLayout = khoangThuyenListParent.GetComponent<GridLayoutGroup>();
            cargoLayout.cellSize = new Vector2(110f, 110f);
            cargoLayout.spacing = new Vector2(10f, 10f);
            cargoLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            cargoLayout.constraintCount = 4;
            MakeScrollable(khoangThuyenTabContent, khoangThuyenListParent, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.85f));

            // 2. Cay Beo Tab Content
            cayBeoTabContent = new GameObject("CayBeoTabContent", typeof(RectTransform));
            cayBeoTabContent.transform.SetParent(marketingPanel.transform, false);
            Stretch(cayBeoTabContent.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            // Left panel for Inventory items
            GameObject invPanel = CreatePanel("InventoryPanel", cayBeoTabContent.transform, new Color(0.08f, 0.12f, 0.10f, 0.9f));
            Stretch(invPanel.GetComponent<RectTransform>(), new Vector2(0.04f, 0.12f), new Vector2(0.46f, 0.80f));
            CreateText("InvHeader", invPanel.transform, 22, TextAnchor.MiddleCenter).text = "KHO HANG TREN GHE (Keo tha hoac Click chon)";
            Stretch(invPanel.transform.Find("InvHeader").GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));
            
            inventoryGridParent = new GameObject("InventoryGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            var invLayout = inventoryGridParent.GetComponent<GridLayoutGroup>();
            invLayout.cellSize = new Vector2(100f, 100f);
            invLayout.spacing = new Vector2(8f, 8f);
            invLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            invLayout.constraintCount = 4;
            invLayout.childAlignment = TextAnchor.UpperCenter;
            MakeScrollable(invPanel, inventoryGridParent, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.85f));

            // Right panel for Cây Bẹo Slots
            GameObject polePanel = CreatePanel("PolePanel", cayBeoTabContent.transform, new Color(0.18f, 0.15f, 0.10f, 0.9f));
            Stretch(polePanel.GetComponent<RectTransform>(), new Vector2(0.54f, 0.12f), new Vector2(0.96f, 0.80f));
            CreateText("PoleHeader", polePanel.transform, 22, TextAnchor.MiddleCenter).text = "CAY BEO (CAC MAT HANG DANG TREO)";
            Stretch(polePanel.transform.Find("PoleHeader").GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));

            poleSlotsParent = new GameObject("PoleSlotsGrid", typeof(RectTransform), typeof(VerticalLayoutGroup));
            poleSlotsParent.transform.SetParent(polePanel.transform, false);
            Stretch(poleSlotsParent.GetComponent<RectTransform>(), new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.85f));
            var poleLayout = poleSlotsParent.GetComponent<VerticalLayoutGroup>();
            poleLayout.spacing = 8f;
            poleLayout.childControlWidth = true;
            poleLayout.childControlHeight = false;
            poleLayout.childForceExpandWidth = true;
            poleLayout.childForceExpandHeight = false;

            // Instructions text at bottom of marketing
            marketingText = CreateText("Instructions", marketingPanel.transform, 18, TextAnchor.MiddleCenter);
            marketingText.text = "Keo tha hang tu Kho sang Cay Beo. Click vao o Cay Beo co chu Go de tháo dõ.";
            Stretch(marketingText.rectTransform, new Vector2(0.05f, 0.02f), new Vector2(0.6f, 0.10f));

            CreateActionButton(marketingPanel.transform, "Go Tat Ca", new Vector2(0.65f, 0.03f), new Vector2(0.78f, 0.09f), ClearPole);
            CreateActionButton(marketingPanel.transform, "Dong", new Vector2(0.82f, 0.03f), new Vector2(0.95f, 0.09f), CloseMarketingPanel);
            marketingPanel.SetActive(false);

            // Dialogue Panel (Visual Novel style at the bottom center)
            dialoguePanel = CreatePanel("DialoguePanel", canvasObject.transform, new Color(0f, 0f, 0f, 0.65f));
            Stretch(dialoguePanel.GetComponent<RectTransform>(), new Vector2(0.15f, 0.05f), new Vector2(0.85f, 0.22f));

            // NPC Avatar (left)
            npcAvatar = CreateImage("NpcAvatar", dialoguePanel.transform, Color.white);
            Stretch(npcAvatar.rectTransform, new Vector2(0.02f, 0.12f), new Vector2(0.14f, 0.88f));

            // Player Avatar (right)
            playerAvatar = CreateImage("PlayerAvatar", dialoguePanel.transform, Color.white);
            Stretch(playerAvatar.rectTransform, new Vector2(0.86f, 0.12f), new Vector2(0.98f, 0.88f));

            npcNameText = CreateText("Name", dialoguePanel.transform, 20, TextAnchor.MiddleLeft);
            npcNameText.color = new Color(0.92f, 0.82f, 0.55f, 1f); // Gold color for name
            Stretch(npcNameText.rectTransform, new Vector2(0.16f, 0.65f), new Vector2(0.84f, 0.90f));

            dialogueText = CreateText("Dialogue", dialoguePanel.transform, 18, TextAnchor.UpperLeft);
            Stretch(dialogueText.rectTransform, new Vector2(0.16f, 0.08f), new Vector2(0.84f, 0.60f));

            // Dialogue choices panel (positioned on the right side and above the dialogue box)
            choicePanel = new GameObject("ChoicePanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            choicePanel.transform.SetParent(canvasObject.transform, false);
            Stretch(choicePanel.GetComponent<RectTransform>(), new Vector2(0.60f, 0.24f), new Vector2(0.95f, 0.85f));
            var choiceLayout = choicePanel.GetComponent<VerticalLayoutGroup>();
            choiceLayout.spacing = 10f;
            choiceLayout.childAlignment = TextAnchor.LowerCenter;
            choiceLayout.childControlWidth = true;
            choiceLayout.childControlHeight = false;
            choiceLayout.childForceExpandWidth = true;
            choiceLayout.childForceExpandHeight = false;
            choicePanel.SetActive(false);

            dialoguePanel.SetActive(false);

            // Bổ sung các Panel giao dịch phụ
            BuildTradeQuantityPanel(canvasObject.transform);
            BuildBoatYardPanel(canvasObject.transform);
        }

        private void ToggleTutorial() => tutorialPanel.SetActive(!tutorialPanel.activeSelf);
        private void ToggleSettings() => settingsPanel.SetActive(!settingsPanel.activeSelf);

        private void SetCargoTab(int tab)
        {
            activeCargoTab = tab;
            RefreshMarketing();
        }

        public void ToggleMarketing()
        {
            if (marketingPanel == null) return;
            marketingPanel.SetActive(!marketingPanel.activeSelf);
            
            var boarding = FindAnyObjectByType<BoatBoardingController>();
            if (marketingPanel.activeSelf)
            {
                activeCargoTab = 0;
                RefreshMarketing();
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;

                if (boarding != null && boarding.IsBoarded)
                    boarding.SetBoatControlActive(false);
            }
            else
            {
                if (activeDragObject != null)
                {
                    Destroy(activeDragObject);
                    activeDragObject = null;
                }
                if (boarding != null && boarding.IsBoarded)
                    boarding.SetBoatControlActive(true);
            }
        }

        public void OpenMarketingPanel()
        {
            if (marketingPanel != null && !marketingPanel.activeSelf)
            {
                ToggleMarketing();
            }
        }

        public void CloseMarketingPanel()
        {
            if (marketingPanel != null && marketingPanel.activeSelf)
            {
                if (activeDragObject != null)
                {
                    Destroy(activeDragObject);
                    activeDragObject = null;
                }
                ToggleMarketing();
            }
        }

        private void RefreshMarketing()
        {
            if (bambooPoleManager == null || inventoryManager == null) return;

            if (activeDragObject != null)
            {
                Destroy(activeDragObject);
                activeDragObject = null;
            }

            if (tabButton1 != null && tabButton2 != null)
            {
                tabButton1.GetComponent<Image>().color = activeCargoTab == 0 ? new Color(0.86f, 0.73f, 0.46f, 1f) : new Color(0.2f, 0.4f, 0.6f, 1f);
                tabButton1.transform.Find("Label").GetComponent<Text>().color = activeCargoTab == 0 ? Color.black : Color.white;

                tabButton2.GetComponent<Image>().color = activeCargoTab == 1 ? new Color(0.86f, 0.73f, 0.46f, 1f) : new Color(0.2f, 0.4f, 0.6f, 1f);
                tabButton2.transform.Find("Label").GetComponent<Text>().color = activeCargoTab == 1 ? Color.black : Color.white;
            }

            if (khoangThuyenTabContent != null) khoangThuyenTabContent.SetActive(activeCargoTab == 0);
            if (cayBeoTabContent != null) cayBeoTabContent.SetActive(activeCargoTab == 1);

            if (marketingText != null)
            {
                if (activeCargoTab == 0)
                {
                    marketingText.text = "Click chọn 2 ô để hoán đổi vị trí nông sản trong khoang chứa.";
                }
                else
                {
                    marketingText.text = "Kéo thả hàng từ Kho sang Cây Bẹo để tiếp thị quảng cáo. Click chữ Gỡ để tháo xuống.";
                }
            }

            // Clear previously created visual items
            foreach (var elem in createdUIElements)
            {
                if (elem != null) Destroy(elem);
            }
            createdUIElements.Clear();

            int maxSlots = inventoryManager.MaxSlots;
            var slots = inventoryManager.CargoSlots;

            if (activeCargoTab == 0)
            {
                // Tab 1: Khoang Thuyen - Grid of Slots (4 columns)
                for (int i = 0; i < maxSlots; i++)
                {
                    int index = i;
                    bool hasItem = index < slots.Count && slots[index] != null && slots[index].item != null && slots[index].amount > 0;

                    GameObject slotObj = CreatePanel($"CargoSlot_{index}", khoangThuyenListParent.transform, 
                        hasItem ? new Color(0.18f, 0.25f, 0.22f, 1f) : new Color(0.08f, 0.08f, 0.08f, 0.7f));
                    createdUIElements.Add(slotObj);

                    var slotHandler = slotObj.AddComponent<UICargoGridSlotHandler>();
                    slotHandler.slotIndex = index;
                    slotHandler.parentUI = this;
                    slotHandler.item = hasItem ? slots[index].item : null;

                    if (selectedCargoSlotIndex == index)
                    {
                        slotObj.GetComponent<Image>().color = new Color(0.88f, 0.71f, 0.34f, 1f);
                    }

                    Text text = CreateText("Label", slotObj.transform, 16, TextAnchor.MiddleCenter);
                    if (hasItem)
                    {
                        var slotItem = slots[index].item;
                        text.text = $"<b>{slotItem.itemName}</b>\nx{slots[index].amount}\n({slotItem.weight * slots[index].amount:0.0} kg)";
                        text.color = Color.white;

                        // Hỗ trợ kéo thả
                        var drag = slotObj.AddComponent<UIDragHandler>();
                        drag.item = slotItem;
                        drag.parentUI = this;
                        drag.fromCargoSlotIndex = index;
                    }
                    else
                    {
                        text.text = $"[Ô {index + 1}]\nTrống";
                        text.color = new Color(1f, 1f, 1f, 0.35f);
                    }
                    Stretch(text.rectTransform, Vector2.zero, Vector2.one);
                }
            }
            else
            {
                // Tab 2: Cay Beo
                // 1. Build Inventory Grid (Left) - Now uses 3-column Cargo Grid slots!
                for (int i = 0; i < maxSlots; i++)
                {
                    int index = i;
                    bool hasItem = index < slots.Count && slots[index] != null && slots[index].item != null && slots[index].amount > 0;

                    GameObject slotObj = CreatePanel($"CargoSlot_{index}", inventoryGridParent.transform, 
                        hasItem ? new Color(0.18f, 0.25f, 0.22f, 1f) : new Color(0.08f, 0.08f, 0.08f, 0.7f));
                    createdUIElements.Add(slotObj);

                    var slotHandler = slotObj.AddComponent<UICargoGridSlotHandler>();
                    slotHandler.slotIndex = index;
                    slotHandler.parentUI = this;
                    slotHandler.item = hasItem ? slots[index].item : null;

                    if (selectedCargoSlotIndex == index)
                    {
                        slotObj.GetComponent<Image>().color = new Color(0.88f, 0.71f, 0.34f, 1f);
                    }

                    Text text = CreateText("Label", slotObj.transform, 14, TextAnchor.MiddleCenter);
                    if (hasItem)
                    {
                        var slotItem = slots[index].item;
                        text.text = $"<b>{slotItem.itemName}</b>\nx{slots[index].amount}";
                        text.color = Color.white;

                        // Hỗ trợ kéo thả lên Cây Bẹo
                        var drag = slotObj.AddComponent<UIDragHandler>();
                        drag.item = slotItem;
                        drag.parentUI = this;
                        drag.fromCargoSlotIndex = index;
                    }
                    else
                    {
                        text.text = $"Ô {index + 1}\nTrống";
                        text.color = new Color(1f, 1f, 1f, 0.35f);
                    }
                    Stretch(text.rectTransform, Vector2.zero, Vector2.one);
                }

                // 2. Build Pole Slots (Right)
                int maxPoleSlots = bambooPoleManager.MaxDisplayedItems;
                List<ItemData> displayed = bambooPoleManager.DisplayedItems;

                for (int i = 0; i < maxPoleSlots; i++)
                {
                    int index = i;
                    bool isOccupied = index < displayed.Count;

                    GameObject slot = CreatePanel($"PoleSlot_{index}", poleSlotsParent.transform, 
                        isOccupied ? new Color(0.28f, 0.22f, 0.16f, 1f) : new Color(0.12f, 0.12f, 0.12f, 0.8f));
                    createdUIElements.Add(slot);
                    var le = slot.AddComponent<LayoutElement>();
                    le.preferredHeight = 60f;

                    // Add drop handler for slot
                    var slotHandler = slot.AddComponent<UIPoleSlotHandler>();
                    slotHandler.slotIndex = index;
                    slotHandler.parentUI = this;

                    Text label = CreateText("Label", slot.transform, 20, TextAnchor.MiddleLeft);
                    label.text = isOccupied ? $" Slot {index + 1}: {displayed[index].itemName}" : $" Slot {index + 1}: [Trong] - Keo tha vao day";
                    Stretch(label.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.70f, 1f));

                    if (isOccupied)
                    {
                        ItemData item = displayed[index];
                        // Add a Remove [X] button
                        GameObject removeBtn = new GameObject("RemoveBtn", typeof(RectTransform), typeof(Image), typeof(Button));
                        removeBtn.transform.SetParent(slot.transform, false);
                        Stretch(removeBtn.GetComponent<RectTransform>(), new Vector2(0.75f, 0.15f), new Vector2(0.95f, 0.85f));
                        removeBtn.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 1f);
                        
                        Text btnTxt = CreateText("Label", removeBtn.transform, 18, TextAnchor.MiddleCenter);
                        btnTxt.text = "Go";
                        Stretch(btnTxt.rectTransform, Vector2.zero, Vector2.one);

                        Button btn = removeBtn.GetComponent<Button>();
                        btn.onClick.AddListener(() =>
                        {
                            bambooPoleManager.RemoveItem(item);
                            RefreshMarketing();
                        });
                    }
                }
            }
        }

        public void HandleItemClicked(ItemData item)
        {
            selectedInventoryItemForPole = item;
            RefreshMarketing();
        }

        public void HandleSlotClicked(int slotIndex)
        {
            if (bambooPoleManager == null) return;

            if (selectedInventoryItemForPole != null)
            {
                // Try to replace the item
                List<ItemData> displayed = bambooPoleManager.DisplayedItems;
                if (slotIndex < displayed.Count)
                {
                    bambooPoleManager.RemoveItem(displayed[slotIndex]);
                }
                bambooPoleManager.HangItem(selectedInventoryItemForPole);
                selectedInventoryItemForPole = null;
                RefreshMarketing();
            }
            else
            {
                // If clicked an occupied slot without item selected, remove it
                List<ItemData> displayed = bambooPoleManager.DisplayedItems;
                if (slotIndex < displayed.Count)
                {
                    bambooPoleManager.RemoveItem(displayed[slotIndex]);
                    RefreshMarketing();
                }
            }
        }

        public void HandleItemDropped(ItemData item, Vector2 screenPos)
        {
            // Drop anywhere outside
            // Check if dropped inside pole slots area
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null || poleSlotsParent == null) return;

            RectTransform rect = poleSlotsParent.GetComponent<RectTransform>();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, canvas.worldCamera))
            {
                if (bambooPoleManager != null && !bambooPoleManager.DisplayedItems.Contains(item))
                {
                    bambooPoleManager.HangItem(item);
                    RefreshMarketing();
                }
            }
        }

        public void HandleItemDroppedOnSlot(ItemData item, int slotIndex)
        {
            if (bambooPoleManager != null)
            {
                List<ItemData> displayed = bambooPoleManager.DisplayedItems;
                if (slotIndex < displayed.Count)
                {
                    bambooPoleManager.RemoveItem(displayed[slotIndex]);
                }
                bambooPoleManager.HangItem(item);
                RefreshMarketing();
            }
        }

        private void ClearPole()
        {
            if (bambooPoleManager != null)
            {
                bambooPoleManager.ClearPole();
                RefreshMarketing();
            }
        }

        // ==========================================
        // PROCEDURAL DIALOGUE & INTERACTION SYSTEM
        // ==========================================
        
        public void OpenBargainDialogue(NpcTradeTarget npc)
        {
            activeNpc = npc;
            dialogueState = DialogueState.MerchantGreeting;
            selectedBargainItem = null;
            justFinishedTrade = false;

            dialoguePanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            UpdateDialogueUI();
        }

        public void OpenTradeDialogue(NpcTradeTarget npc)
        {
            activeNpc = npc;
            if (npc.name.Contains("FoodVendor") || npc.NpcDisplayName.Contains("Vendor"))
            {
                dialogueState = DialogueState.VendorGreeting;
            }
            else
            {
                dialogueState = DialogueState.GardenerGreeting;
            }

            dialoguePanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            UpdateDialogueUI();
        }

        public void OpenUpgradeCampDialogue(NpcTradeTarget npc)
        {
            activeNpc = npc;
            dialogueState = DialogueState.UpgradeCampGreeting;

            dialoguePanel.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            UpdateDialogueUI();
        }

        public void CloseAllDialogueAndPanels()
        {
            dialogueState = DialogueState.Closed;
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
            if (choicePanel != null) choicePanel.SetActive(false);
            
            // Clean up choice buttons
            ClearChoicePanel();
        }

        private void ClearChoicePanel()
        {
            foreach (var btn in choiceButtons)
            {
                if (btn != null) Destroy(btn);
            }
            choiceButtons.Clear();
            if (choicePanel != null) choicePanel.SetActive(false);
        }

        private void UpdateDialogueUI()
        {
            if (activeNpc == null)
            {
                CloseAllDialogueAndPanels();
                return;
            }

            ClearChoicePanel();
            choicePanel.SetActive(true);

            Sprite npcAvatarSprite = GetNpcAvatar(activeNpc);
            Sprite playerAvatarSprite = GetPlayerAvatar();

            if (npcAvatarSprite != null)
            {
                npcAvatar.gameObject.SetActive(true);
                npcAvatar.sprite = npcAvatarSprite;
                npcAvatar.color = Color.white;
            }
            else
            {
                npcAvatar.gameObject.SetActive(false);
            }

            if (playerAvatarSprite != null)
            {
                playerAvatar.gameObject.SetActive(true);
                playerAvatar.sprite = playerAvatarSprite;
                playerAvatar.color = Color.white;
            }
            else
            {
                playerAvatar.gameObject.SetActive(false);
            }

            // Fetch bargaining system
            BargainingSystem bargainingSystem = FindAnyObjectByType<BargainingSystem>();

            switch (dialogueState)
            {
                case DialogueState.MerchantGreeting:
                    if (bargainingSystem == null)
                    {
                        CloseAllDialogueAndPanels();
                        break;
                    }

                    if (activeNpc != null && activeNpc.HasTraded)
                    {
                        npcNameText.text = activeNpc.NpcDisplayName;
                        if (justFinishedTrade)
                        {
                            dialogueText.text = bargainingSystem.CurrentMessage;
                        }
                        else
                        {
                            dialogueText.text = "Tôm cá trái cây mua đủ rồi, hẹn chú em bữa khác nghen!";
                        }
                        CreateChoiceButton("1. Tạm biệt", CloseDialogueAndReleasePlayer);
                        break;
                    }

                    if (!bargainingSystem.HasActiveSession)
                    {
                        var items = new List<KeyValuePair<ItemData, int>>();
                        if (inventoryManager != null)
                        {
                            foreach (var kvp in inventoryManager.Inventory)
                            {
                                if (kvp.Value > 0) items.Add(kvp);
                            }
                        }

                        if (items.Count == 0)
                        {
                            npcNameText.text = activeNpc.NpcDisplayName;
                            if (bargainingSystem.SelectedItem == null && bargainingSystem.CurrentAskPrice == 0)
                            {
                                dialogueText.text = "Chào ngày mới chú em! Hôm nay ghe chú em trống trơn hà, có hàng gì đâu mà bán sỉ!";
                            }
                            else
                            {
                                dialogueText.text = bargainingSystem.CurrentMessage;
                            }
                            CreateChoiceButton("1. Tạm biệt", CloseDialogueAndReleasePlayer);
                            break;
                        }
                        else
                        {
                            int randIdx = UnityEngine.Random.Range(0, items.Count);
                            ItemData item = items[randIdx].Key;
                            int availableCount = items[randIdx].Value;

                            int quantity = 1;
                            if (activeNpc.name.Contains("Large") || activeNpc.NpcDisplayName.Contains("Large") || activeNpc.NpcDisplayName.Contains("Merchant"))
                            {
                                quantity = UnityEngine.Random.Range(5, Mathf.Min(20, availableCount) + 1);
                            }
                            else
                            {
                                quantity = UnityEngine.Random.Range(1, Mathf.Min(5, availableCount) + 1);
                            }

                            bargainingSystem.StartBargainingSession(GetMerchantProfile(activeNpc), item, quantity);
                        }
                    }

                    npcNameText.text = activeNpc.NpcDisplayName;
                    dialogueText.text = bargainingSystem.CurrentMessage;

                    npcAvatar.gameObject.SetActive(true);
                    npcAvatar.color = Color.white;

                    if (bargainingSystem.HasActiveSession)
                    {
                        CreateChoiceButton("1. Đưa ra đơn giá bán (Chém giá)", () =>
                        {
                            OpenTradeQuantityPanel(bargainingSystem.SelectedItem, buy: false, wholesale: true, priceMode: true);
                        });
                        CreateChoiceButton("2. Không bán nữa", () =>
                        {
                            bargainingSystem.RejectDeal();
                            if (activeNpc != null) activeNpc.HasTraded = true;
                            justFinishedTrade = true;
                            dialogueState = DialogueState.MerchantGreeting;
                            UpdateDialogueUI();
                        });
                    }
                    else
                    {
                        CreateChoiceButton("1. Tạm biệt", CloseDialogueAndReleasePlayer);
                    }
                    break;

                case DialogueState.MerchantSelectQuantity:
                    dialogueState = DialogueState.MerchantGreeting;
                    UpdateDialogueUI();
                    break;

                case DialogueState.MerchantBargaining:
                    if (bargainingSystem == null || !bargainingSystem.HasActiveSession)
                    {
                        dialogueState = DialogueState.MerchantGreeting;
                        UpdateDialogueUI();
                        return;
                    }

                    npcNameText.text = activeNpc.NpcDisplayName;
                    dialogueText.text = bargainingSystem.CurrentMessage;

                    npcAvatar.gameObject.SetActive(true);
                    npcAvatar.color = Color.white;

                    int bIndex = 1;
                    if (bargainingSystem.NpcWalkedAway)
                    {
                        CreateChoiceButton($"{bIndex++}. Kêu khách quay lại và chấp nhận giá {bargainingSystem.NpcLastOfferedPrice:N0} VNĐ/quả", () =>
                        {
                            bargainingSystem.AcceptNpcLastOffer();
                            if (activeNpc != null) activeNpc.HasTraded = true;
                            justFinishedTrade = true;
                            dialogueState = DialogueState.MerchantGreeting;
                            UpdateDialogueUI();
                        });
                        CreateChoiceButton($"{bIndex++}. Để khách đi", () =>
                        {
                            bargainingSystem.RejectDeal();
                            if (activeNpc != null) activeNpc.HasTraded = true;
                            justFinishedTrade = true;
                            dialogueState = DialogueState.MerchantGreeting;
                            UpdateDialogueUI();
                        });
                    }
                    else
                    {
                        CreateChoiceButton($"{bIndex++}. Chốt kèo bán luôn với giá {bargainingSystem.NpcLastOfferedPrice:N0} VNĐ/quả", () =>
                        {
                            bargainingSystem.AcceptNpcLastOffer();
                            if (activeNpc != null) activeNpc.HasTraded = true;
                            justFinishedTrade = true;
                            dialogueState = DialogueState.MerchantGreeting;
                            UpdateDialogueUI();
                        });
                        CreateChoiceButton($"{bIndex++}. Thương lượng đơn giá mới (Đôi co tiếp)", () =>
                        {
                            OpenTradeQuantityPanel(bargainingSystem.SelectedItem, buy: false, wholesale: true, priceMode: true);
                        });
                        CreateChoiceButton($"{bIndex++}. Không bán nữa", () =>
                        {
                            bargainingSystem.RejectDeal();
                            if (activeNpc != null) activeNpc.HasTraded = true;
                            justFinishedTrade = true;
                            dialogueState = DialogueState.MerchantGreeting;
                            UpdateDialogueUI();
                        });
                    }
                    break;

                case DialogueState.VendorGreeting:
                    npcNameText.text = activeNpc.NpcDisplayName;
                    dialogueText.text = "Tô bún riêu cua đồng thơm phức đây! Dừng chân ăn uống nghỉ ngơi chút không chú em?";

                    npcAvatar.color = Color.white;
                    playerAvatar.color = new Color(1f, 1f, 1f, 0.4f);

                    int vIndex = 1;
                    CreateChoiceButton($"{vIndex++}. Ăn tô bún nước lèo (Hồi thể lực - 8,000 VNĐ)", () =>
                    {
                        RestoreStaminaOnVendor();
                        UpdateDialogueUI();
                    });

                    CreateChoiceButton($"{vIndex++}. Hỏi thăm tin tức thị trường", () =>
                    {
                        dialogueState = DialogueState.VendorNews;
                        UpdateDialogueUI();
                    });

                    CreateChoiceButton($"{vIndex++}. Nghỉ ngơi đến sáng (Ngủ)", () =>
                    {
                        SleepOnVendor();
                    });

                    CreateChoiceButton($"{vIndex++}. Tạm biệt", CloseDialogueAndReleasePlayer);
                    break;

                case DialogueState.VendorTrading:
                    // Đi theo lối GardenerTrading nên không dùng VendorTrading nữa
                    dialogueState = DialogueState.GardenerTrading;
                    UpdateDialogueUI();
                    break;

                case DialogueState.VendorNews:
                    npcNameText.text = activeNpc.NpcDisplayName;
                    
                    string rumor = "Hiện tại chưa có tin đồn thị trường nào mới.";
                    var newsController = FindAnyObjectByType<MarketNewsController>();
                    if (newsController != null && newsController.CurrentNews != null)
                    {
                        rumor = $"[TIN ĐỒN]: {newsController.CurrentNews.headline}\n\n\"{newsController.CurrentNews.marketRumor}\"";
                    }
                    dialogueText.text = rumor;

                    npcAvatar.color = Color.white;
                    playerAvatar.color = new Color(1f, 1f, 1f, 0.4f);

                    CreateChoiceButton("1. Cảm ơn thông tin!", () =>
                    {
                        // Quay lại greeting tương ứng
                        if (activeNpc.name.Contains("FoodVendor") || activeNpc.NpcDisplayName.Contains("Vendor"))
                        {
                            dialogueState = DialogueState.VendorGreeting;
                        }
                        else
                        {
                            dialogueState = DialogueState.GardenerGreeting;
                        }
                        UpdateDialogueUI();
                    });
                    break;

                case DialogueState.GardenerGreeting:
                    npcNameText.text = activeNpc.NpcDisplayName;
                    dialogueText.text = "Nhà vườn tụi tui mới bẻ trái cây ngoài vựa vô tươi rói nè chú em. Mua lẻ giá gốc đi bán sỉ lại nghen!";

                    npcAvatar.color = Color.white;
                    playerAvatar.color = new Color(1f, 1f, 1f, 0.4f);

                    int gIndex = 1;
                    CreateChoiceButton($"{gIndex++}. Thu mua nông sản giá gốc (Mua lẻ)", () =>
                    {
                        dialogueState = DialogueState.GardenerTrading;
                        UpdateDialogueUI();
                    });

                    CreateChoiceButton($"{gIndex++}. Hỏi thăm tin tức mùa vụ", () =>
                    {
                        dialogueState = DialogueState.VendorNews;
                        UpdateDialogueUI();
                    });

                    CreateChoiceButton($"{gIndex++}. Tạm biệt", CloseDialogueAndReleasePlayer);
                    break;

                case DialogueState.GardenerTrading:
                    npcNameText.text = activeNpc.NpcDisplayName;
                    dialogueText.text = "Tôi sẵn lòng chia lại các mặt hàng nông sản giá gốc này cho chú em:";

                    npcAvatar.color = Color.white;
                    playerAvatar.color = new Color(1f, 1f, 1f, 0.4f);

                    int gtIndex = 1;
                    var marketItemsList = GetMarketItemsList();
                    if (marketItemsList != null)
                    {
                        foreach (var item in marketItemsList)
                        {
                            int buyPrice = item.basePrice;
                            int owned = inventoryManager != null && inventoryManager.Inventory.TryGetValue(item, out int count) ? count : 0;

                            CreateChoiceButton($"{gtIndex++}. Thu mua {item.itemName} (Giá gốc: {buyPrice:N0} VNĐ) [Đang có {owned}]", () =>
                            {
                                OpenTradeQuantityPanel(item, buy: true, wholesale: false);
                            });
                        }
                    }

                    CreateChoiceButton($"{gtIndex++}. Quay lại", () =>
                    {
                        dialogueState = DialogueState.GardenerGreeting;
                        UpdateDialogueUI();
                    });
                    break;

                case DialogueState.UpgradeCampGreeting:
                    npcNameText.text = activeNpc.NpcDisplayName;
                    dialogueText.text = "Chào chú em! Trại ghe xóm nước chuyên đóng, sửa chữa vỏ lãi, ghe xuồng, lắp máy đuôi tôm đây. Cần làm gì nào?";

                    npcAvatar.color = Color.white;
                    playerAvatar.color = new Color(1f, 1f, 1f, 0.4f);

                    CreateChoiceButton("1. Vào Trại Ghe (Nâng cấp & Sửa chữa)", () =>
                    {
                        OpenBoatYardPanel();
                    });

                    CreateChoiceButton("2. Tạm biệt", CloseDialogueAndReleasePlayer);
                    break;
            }
        }

        private void CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClick)
        {
            GameObject btnObj = new GameObject(choiceText, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(choicePanel.transform, false);
            
            // Layout element for preferred size
            var le = btnObj.AddComponent<LayoutElement>();
            le.preferredHeight = 50f;

            Image img = btnObj.GetComponent<Image>();
            img.color = new Color(0.08f, 0.08f, 0.08f, 0.85f);

            Button btn = btnObj.GetComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
            colors.pressedColor = new Color(0.3f, 0.3f, 0.3f, 0.95f);
            colors.disabledColor = new Color(0.1f, 0.1f, 0.1f, 0.5f);
            btn.colors = colors;

            btn.onClick.AddListener(onClick);

            Text text = CreateText("Label", btnObj.transform, 18, TextAnchor.MiddleLeft);
            text.text = choiceText;
            text.color = new Color(0.92f, 0.82f, 0.55f, 1f);
            Stretch(text.rectTransform, new Vector2(0.05f, 0f), new Vector2(0.95f, 1f));

            choiceButtons.Add(btnObj);
        }

        private void CloseDialogueAndReleasePlayer()
        {
            justFinishedTrade = false;
            CloseAllDialogueAndPanels();
            
            var interactor = FindAnyObjectByType<PlayerNpcTradeInteractor>();
            if (interactor != null)
            {
                interactor.CloseTrade();
            }
        }

        private Sprite GetNpcAvatar(NpcTradeTarget npc)
        {
            // First check config for matching avatar
            var config = FindAnyObjectByType<BargainingSystem>()?.EconomyConfig;
            if (config != null)
            {
                foreach (var profile in config.NpcProfiles)
                {
                    if (profile.displayName == npc.NpcDisplayName || npc.name.Contains(profile.npcId))
                        return profile.avatar;
                }
            }

            // Fallback to news databases
            var newsController = FindAnyObjectByType<MarketNewsController>();
            if (newsController != null && newsController.CurrentNews != null && newsController.CurrentNews.npcName == npc.NpcDisplayName)
            {
                return newsController.CurrentNews.npcAvatar;
            }

            return null;
        }

        private Sprite GetPlayerAvatar()
        {
            var config = FindAnyObjectByType<BargainingSystem>()?.EconomyConfig;
            if (config != null && config.NpcProfiles.Count > 0)
            {
                foreach (var profile in config.NpcProfiles)
                {
                    if (profile.npcId.ToLower().Contains("villager") || profile.npcId.ToLower().Contains("dan"))
                        return profile.avatar;
                }
                return config.NpcProfiles[0].avatar;
            }
            return null;
        }

        private BargainingNpcProfile GetMerchantProfile(NpcTradeTarget npc)
        {
            var config = FindAnyObjectByType<BargainingSystem>()?.EconomyConfig;
            if (config != null)
            {
                foreach (var profile in config.NpcProfiles)
                {
                    if (profile.displayName == npc.NpcDisplayName || npc.name.Contains(profile.npcId))
                        return profile;
                }
                if (config.NpcProfiles.Count > 0) return config.NpcProfiles[0];
            }
            return null;
        }

        // ==========================================
        // VENDOR OPERATIONS fallbacks
        // ==========================================
        private int GetRepairCostOnVendor()
        {
            var durability = FindAnyObjectByType<DurabilityManager>();
            if (durability == null) return 12000;
            float missing = durability.MaxDurability - durability.CurrentDurability;
            return 6000 + Mathf.RoundToInt(missing * 220f);
        }

        private void RepairBoatOnVendor()
        {
            var economy = FindAnyObjectByType<EconomyManager>();
            var durability = FindAnyObjectByType<DurabilityManager>();
            if (economy == null || durability == null) return;

            int cost = GetRepairCostOnVendor();
            ServiceData repair = ScriptableObject.CreateInstance<ServiceData>();
            repair.serviceName = "Sua Ghe";
            repair.costMoney = cost;
            repair.durabilityRestoreAmount = 18f;
            economy.BuyService(repair);
            Destroy(repair);
        }

        private void RestoreStaminaOnVendor()
        {
            var economy = FindAnyObjectByType<EconomyManager>();
            if (economy == null) return;

            ServiceData meal = ScriptableObject.CreateInstance<ServiceData>();
            meal.serviceName = "Bun Nuoc Leo";
            meal.costMoney = 8000;
            meal.staminaRestoreAmount = 18f;
            economy.BuyService(meal);
            Destroy(meal);
        }

        private void SleepOnVendor()
        {
            var time = FindAnyObjectByType<TimeManager>();
            if (time != null)
            {
                time.Sleep();
            }
            CloseDialogueAndReleasePlayer();
        }

        private List<ItemData> GetMarketItemsList()
        {
            if (riverMarketHUD != null)
            {
                // Access serialised field marketItems via reflection
                var field = typeof(RiverMarketHUD).GetField("marketItems", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    var val = field.GetValue(riverMarketHUD) as List<ItemData>;
                    if (val != null) return val;
                }
            }
            return new List<ItemData>();
        }

        private int GetSalePriceOnVendor(ItemData item)
        {
            if (riverMarketHUD != null)
            {
                var method = typeof(RiverMarketHUD).GetMethod("GetSaleMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    float mult = (float)method.Invoke(riverMarketHUD, new object[] { item });
                    return Mathf.RoundToInt(item.basePrice * mult);
                }
            }
            return Mathf.RoundToInt(item.basePrice * 1.05f);
        }

        private void BuyItemOnVendor(ItemData item)
        {
            var economy = FindAnyObjectByType<EconomyManager>();
            if (economy != null && inventoryManager != null)
            {
                economy.BuyItemToInventory(item, 1, inventoryManager);
            }
        }

        private void SellItemOnVendor(ItemData item)
        {
            var economy = FindAnyObjectByType<EconomyManager>();
            if (economy != null && inventoryManager != null)
            {
                int finalRevenue = GetSalePriceOnVendor(item);
                economy.SellItemWholesale(item, 1, inventoryManager, finalRevenue);
            }
        }

        // ==========================================
        // CONTEXTUAL SIDE PROMPTS
        // ==========================================
        private void UpdateSidePrompts(bool isBoarded)
        {
            // If any major UI is open (Tutorial, Settings, Dialogue, Upgrade HUD, news HUD) hide prompts
            bool isUIActive = (dialoguePanel != null && dialoguePanel.activeSelf) 
                || (marketingPanel != null && marketingPanel.activeSelf)
                || (tutorialPanel != null && tutorialPanel.activeSelf)
                || (settingsPanel != null && settingsPanel.activeSelf)
                || (riverMarketHUD != null && (riverMarketHUD.IsUpgradeOpen || riverMarketHUD.IsNewsOpen || riverMarketHUD.IsNpcTradeOpen));

            if (isUIActive)
            {
                if (leftPromptPanel != null) leftPromptPanel.SetActive(false);
                if (rightPromptPanel != null) rightPromptPanel.SetActive(false);
                return;
            }

            // 1. Left prompt (E interact)
            var interactor = FindAnyObjectByType<PlayerNpcTradeInteractor>();
            var boarding = FindAnyObjectByType<BoatBoardingController>();

            bool showLeft = false;
            string leftText = "";

            if (interactor != null && interactor.CurrentTarget != null)
            {
                showLeft = true;
                leftText = $"[E] Tuong tac:\n{interactor.CurrentTarget.NpcDisplayName}";
            }
            else if (boarding != null)
            {
                if (isBoarded)
                {
                    if (boarding.CanDismountBoat)
                    {
                        showLeft = true;
                        leftText = "[E] Roi ghe\n(Den diem bo)";
                    }
                }
                else
                {
                    if (boarding.CanBoardBoat)
                    {
                        showLeft = true;
                        leftText = "[E] Len ghe";
                    }
                }
            }

            if (leftPromptPanel != null)
            {
                leftPromptPanel.SetActive(showLeft);
                if (showLeft && leftPromptText != null)
                {
                    leftPromptText.text = leftText;
                }
            }

            // 2. Right prompt (B Cây Bẹo - only on boat)
            bool showRight = isBoarded;
            if (rightPromptPanel != null)
            {
                rightPromptPanel.SetActive(showRight);
                if (showRight && rightPromptText != null)
                {
                    rightPromptText.text = "[B] Lieu dinh\nCay Beo"; // wait, the prompt text says "Dieu chinh", let's make sure it's correct
                    rightPromptText.text = "[B] Dieu chinh\nCay Beo";
                }
            }
        }

        // ==========================================
        // HELPERS
        // ==========================================
        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null) return;
            GameObject eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
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

        private Image CreateImage(string name, Transform parent, Color color)
        {
            GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Button CreateActionButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction onClick)
        {
            GameObject buttonObject = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            Stretch(buttonObject.GetComponent<RectTransform>(), anchorMin, anchorMax);
            buttonObject.GetComponent<Image>().color = new Color(0.2f, 0.4f, 0.6f, 1f);
            Button button = buttonObject.GetComponent<Button>();
            button.onClick.AddListener(onClick);

            Text text = CreateText("Label", buttonObject.transform, 20, TextAnchor.MiddleCenter);
            text.text = label;
            Stretch(text.rectTransform, Vector2.zero, Vector2.one);
            return button;
        }

        private void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        // ==========================================
        // QUANTITY & TRADE INPUT PANEL
        // ==========================================
        private void BuildTradeQuantityPanel(Transform parent)
        {
            tradeQuantityPanel = CreatePanel("TradeQuantityPanel", parent, new Color(0.08f, 0.12f, 0.14f, 0.98f));
            Stretch(tradeQuantityPanel.GetComponent<RectTransform>(), new Vector2(0.35f, 0.30f), new Vector2(0.65f, 0.70f));

            tradeTitleText = CreateText("Title", tradeQuantityPanel.transform, 24, TextAnchor.MiddleCenter);
            tradeTitleText.color = new Color(0.92f, 0.82f, 0.55f, 1f);
            Stretch(tradeTitleText.rectTransform, new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f));

            tradeItemInfoText = CreateText("ItemInfo", tradeQuantityPanel.transform, 18, TextAnchor.UpperCenter);
            Stretch(tradeItemInfoText.rectTransform, new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.82f));

            CreateActionButton(tradeQuantityPanel.transform, "-", new Vector2(0.15f, 0.44f), new Vector2(0.27f, 0.54f), () => AdjustTradeQty(-1));
            
            GameObject inputObj = new GameObject("QtyInput", typeof(RectTransform), typeof(Image), typeof(InputField));
            inputObj.transform.SetParent(tradeQuantityPanel.transform, false);
            Stretch(inputObj.GetComponent<RectTransform>(), new Vector2(0.31f, 0.44f), new Vector2(0.53f, 0.54f));
            inputObj.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.05f, 1f);
            tradeQtyInputField = inputObj.GetComponent<InputField>();
            
            GameObject ph = new GameObject("Placeholder", typeof(RectTransform), typeof(Text));
            ph.transform.SetParent(inputObj.transform, false);
            Text phText = ph.GetComponent<Text>();
            phText.text = "Nhập...";
            phText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            phText.fontSize = 18;
            phText.alignment = TextAnchor.MiddleCenter;
            phText.color = Color.gray;
            Stretch(phText.rectTransform, Vector2.zero, Vector2.one);

            GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtObj.transform.SetParent(inputObj.transform, false);
            Text valText = txtObj.GetComponent<Text>();
            valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valText.fontSize = 20;
            valText.alignment = TextAnchor.MiddleCenter;
            valText.color = Color.white;
            Stretch(valText.rectTransform, Vector2.zero, Vector2.one);

            tradeQtyInputField.placeholder = phText;
            tradeQtyInputField.textComponent = valText;
            tradeQtyInputField.onValueChanged.AddListener(OnTradeInputChanged);

            CreateActionButton(tradeQuantityPanel.transform, "+", new Vector2(0.57f, 0.44f), new Vector2(0.69f, 0.54f), () => AdjustTradeQty(1));
            CreateActionButton(tradeQuantityPanel.transform, "MAX", new Vector2(0.73f, 0.44f), new Vector2(0.85f, 0.54f), SetTradeQtyMax);

            GameObject sliderObj = new GameObject("QtySlider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(tradeQuantityPanel.transform, false);
            Stretch(sliderObj.GetComponent<RectTransform>(), new Vector2(0.15f, 0.32f), new Vector2(0.85f, 0.38f));
            tradeQtySlider = sliderObj.GetComponent<Slider>();
            
            GameObject sliderBg = CreatePanel("Background", sliderObj.transform, new Color(0.1f, 0.1f, 0.1f, 1f));
            Stretch(sliderBg.GetComponent<RectTransform>(), new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));
            
            GameObject sliderFillArea = new GameObject("FillArea", typeof(RectTransform));
            sliderFillArea.transform.SetParent(sliderObj.transform, false);
            Stretch(sliderFillArea.GetComponent<RectTransform>(), new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));
            
            GameObject sliderFill = CreatePanel("Fill", sliderFillArea.transform, new Color(0.88f, 0.71f, 0.34f, 1f));
            Stretch(sliderFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            
            tradeQtySlider.fillRect = sliderFill.GetComponent<RectTransform>();
            tradeQtySlider.onValueChanged.AddListener(OnTradeSliderChanged);

            tradeSummaryText = CreateText("Summary", tradeQuantityPanel.transform, 16, TextAnchor.UpperCenter);
            Stretch(tradeSummaryText.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.30f));

            tradeConfirmButton = CreateActionButton(tradeQuantityPanel.transform, "XÁC NHẬN", new Vector2(0.15f, 0.04f), new Vector2(0.48f, 0.12f), ConfirmTrade);
            tradeConfirmButton.GetComponent<Image>().color = new Color(0.18f, 0.38f, 0.22f, 1f);
            
            tradeCancelButton = CreateActionButton(tradeQuantityPanel.transform, "HỦY", new Vector2(0.52f, 0.04f), new Vector2(0.85f, 0.12f), CancelTrade);
            tradeCancelButton.GetComponent<Image>().color = new Color(0.5f, 0.15f, 0.15f, 1f);

            tradeQuantityPanel.SetActive(false);
        }

        public void OpenTradeQuantityPanel(ItemData item, bool buy, bool wholesale, bool priceMode = false)
        {
            isSettingUpSlider = true;
            currentTradeItem = item;
            isBuying = buy;
            isWholesale = wholesale;
            isPriceMode = priceMode;

            if (choicePanel != null) choicePanel.SetActive(false);

            if (isPriceMode)
            {
                var bargainingSystem = FindAnyObjectByType<ChoNoiMienTay.Systems.BargainingSystem>();
                selectedTradeQty = bargainingSystem != null ? bargainingSystem.BargainQuantity : 1;

                minTradePrice = Mathf.RoundToInt(item.basePrice * 0.5f);
                maxTradePrice = Mathf.RoundToInt(item.basePrice * 3.0f);

                if (bargainingSystem != null && bargainingSystem.PlayerProposedPrice > 0)
                {
                    selectedTradePrice = bargainingSystem.PlayerProposedPrice;
                }
                else if (bargainingSystem != null)
                {
                    selectedTradePrice = bargainingSystem.NpcOpeningPrice;
                }
                else
                {
                    selectedTradePrice = item.basePrice;
                }
                selectedTradePrice = Mathf.Clamp(selectedTradePrice, minTradePrice, maxTradePrice);

                if (tradeQuantityPanel != null)
                {
                    tradeQuantityPanel.SetActive(true);
                    tradeTitleText.text = "ĐỀ XUẤT GIÁ BÁN";
                    tradeItemInfoText.text = $"Mặt hàng: <b>{item.itemName}</b>\nSố lượng bán sỉ: {selectedTradeQty} quả | Giá gốc: {item.basePrice:N0} VNĐ";

                    tradeQtySlider.minValue = minTradePrice;
                    tradeQtySlider.maxValue = maxTradePrice;
                    tradeQtySlider.value = selectedTradePrice;

                    tradeQtyInputField.text = selectedTradePrice.ToString();
                    UpdateTradeSummary();
                }
            }
            else
            {
                if (isBuying)
                {
                    int maxByCash = playerStats != null ? Mathf.FloorToInt(playerStats.CurrentMoney / (float)item.basePrice) : 999;
                    float freeWeight = inventoryManager != null ? (inventoryManager.MaxWeightCapacity - inventoryManager.CurrentTotalWeight) : 100f;
                    int maxByWeight = Mathf.FloorToInt(freeWeight / item.weight);
                    maxTradeQty = Mathf.Min(maxByCash, maxByWeight);
                    maxTradeQty = Mathf.Clamp(maxTradeQty, 0, 100);
                }
                else
                {
                    maxTradeQty = inventoryManager != null && inventoryManager.Inventory.TryGetValue(item, out int count) ? count : 0;
                }

                selectedTradeQty = maxTradeQty > 0 ? 1 : 0;

                if (tradeQuantityPanel != null)
                {
                    tradeQuantityPanel.SetActive(true);
                    tradeTitleText.text = isBuying ? "THU MUA NÔNG SẢN" : (isWholesale ? "BÁN SỈ NÔNG SẢN" : "BÁN LẺ NÔNG SẢN");
                    tradeItemInfoText.text = $"Mặt hàng: <b>{item.itemName}</b>\nĐơn giá: {item.basePrice:N0} VNĐ/sp | Cân nặng: {item.weight} kg/sp";

                    tradeQtySlider.minValue = maxTradeQty > 0 ? 1 : 0;
                    tradeQtySlider.maxValue = Mathf.Max(1, maxTradeQty);
                    tradeQtySlider.value = selectedTradeQty;

                    tradeQtyInputField.text = selectedTradeQty.ToString();
                    UpdateTradeSummary();
                }
            }
            isSettingUpSlider = false;
        }

        private void OnTradeSliderChanged(float value)
        {
            if (isSettingUpSlider) return;
            if (isPriceMode)
            {
                int step = 500;
                var bargainingSystem = FindAnyObjectByType<ChoNoiMienTay.Systems.BargainingSystem>();
                if (bargainingSystem != null && bargainingSystem.EconomyConfig != null)
                {
                    step = bargainingSystem.EconomyConfig.OfferStep;
                }
                selectedTradePrice = Mathf.RoundToInt(value / (float)step) * step;
                selectedTradePrice = Mathf.Clamp(selectedTradePrice, minTradePrice, maxTradePrice);
                tradeQtyInputField.text = selectedTradePrice.ToString();
            }
            else
            {
                selectedTradeQty = Mathf.RoundToInt(value);
                tradeQtyInputField.text = selectedTradeQty.ToString();
            }
            UpdateTradeSummary();
        }

        private void OnTradeInputChanged(string text)
        {
            if (isSettingUpSlider) return;
            if (int.TryParse(text, out int val))
            {
                if (isPriceMode)
                {
                    selectedTradePrice = Mathf.Clamp(val, minTradePrice, maxTradePrice);
                    tradeQtySlider.value = selectedTradePrice;
                }
                else
                {
                    selectedTradeQty = Mathf.Clamp(val, maxTradeQty > 0 ? 1 : 0, maxTradeQty);
                    tradeQtySlider.value = selectedTradeQty;
                }
                UpdateTradeSummary();
            }
        }

        private void AdjustTradeQty(int amount)
        {
            if (isPriceMode)
            {
                int step = 500;
                var bargainingSystem = FindAnyObjectByType<ChoNoiMienTay.Systems.BargainingSystem>();
                if (bargainingSystem != null && bargainingSystem.EconomyConfig != null)
                {
                    step = bargainingSystem.EconomyConfig.OfferStep;
                }
                selectedTradePrice = Mathf.Clamp(selectedTradePrice + amount * step, minTradePrice, maxTradePrice);
                tradeQtySlider.value = selectedTradePrice;
                tradeQtyInputField.text = selectedTradePrice.ToString();
            }
            else
            {
                selectedTradeQty = Mathf.Clamp(selectedTradeQty + amount, maxTradeQty > 0 ? 1 : 0, maxTradeQty);
                tradeQtySlider.value = selectedTradeQty;
                tradeQtyInputField.text = selectedTradeQty.ToString();
            }
            UpdateTradeSummary();
        }

        private void SetTradeQtyMax()
        {
            if (isPriceMode)
            {
                selectedTradePrice = maxTradePrice;
                tradeQtySlider.value = selectedTradePrice;
                tradeQtyInputField.text = selectedTradePrice.ToString();
            }
            else
            {
                selectedTradeQty = maxTradeQty;
                tradeQtySlider.value = selectedTradeQty;
                tradeQtyInputField.text = selectedTradeQty.ToString();
            }
            UpdateTradeSummary();
        }

        private void UpdateTradeSummary()
        {
            if (currentTradeItem == null) return;

            if (isPriceMode)
            {
                int priceTotal = selectedTradePrice * selectedTradeQty;
                string playerMoney = playerStats != null ? playerStats.CurrentMoney.ToString("N0") : "0";

                tradeSummaryText.text = $"Tổng số lượng: <b>{selectedTradeQty}</b> quả\n" +
                                        $"Đơn giá đề xuất: <b>{selectedTradePrice:N0} VNĐ/quả</b>\n" +
                                        $"Thu nhập dự kiến: <b>{priceTotal:N0} VNĐ</b> (Tiền túi: {playerMoney} VNĐ)";
            }
            else
            {
                float weightTotal = currentTradeItem.weight * selectedTradeQty;
                int priceTotal = currentTradeItem.basePrice * selectedTradeQty;
                if (!isBuying && !isWholesale)
                {
                    priceTotal = GetSalePriceOnVendor(currentTradeItem) * selectedTradeQty;
                }

                string moneyLabel = isBuying ? "Chi phí" : "Thu nhập dự kiến";
                string playerMoney = playerStats != null ? playerStats.CurrentMoney.ToString("N0") : "0";
                string freeWeight = inventoryManager != null ? $"{inventoryManager.MaxWeightCapacity - inventoryManager.CurrentTotalWeight:0.0} kg" : "0 kg";

                tradeSummaryText.text = $"Tổng số lượng: <b>{selectedTradeQty}</b> / {maxTradeQty}\n" +
                                        $"Tổng cân nặng: {weightTotal:0.0} kg (Khoang trống: {freeWeight})\n" +
                                        $"{moneyLabel}: <b>{priceTotal:N0} VNĐ</b> (Tiền túi: {playerMoney} VNĐ)";
            }
        }

        private void ConfirmTrade()
        {
            if (currentTradeItem == null)
            {
                tradeQuantityPanel.SetActive(false);
                if (dialogueState != DialogueState.Closed && choicePanel != null) choicePanel.SetActive(true);
                return;
            }

            if (isPriceMode)
            {
                var bargainingSystem = FindAnyObjectByType<ChoNoiMienTay.Systems.BargainingSystem>();
                if (bargainingSystem != null)
                {
                    if (bargainingSystem.PlayerProposedPrice > 0)
                    {
                        bargainingSystem.CounterOffer(selectedTradePrice);
                    }
                    else
                    {
                        bargainingSystem.ProposePrice(selectedTradePrice);
                    }

                    if (bargainingSystem.HasActiveSession)
                    {
                        dialogueState = DialogueState.MerchantBargaining;
                    }
                    else
                    {
                        if (activeNpc != null) activeNpc.HasTraded = true;
                        justFinishedTrade = true;
                        dialogueState = DialogueState.MerchantGreeting;
                    }
                }
                tradeQuantityPanel.SetActive(false);
                UpdateDialogueUI();
            }
            else
            {
                if (selectedTradeQty <= 0)
                {
                    tradeQuantityPanel.SetActive(false);
                    if (dialogueState != DialogueState.Closed && choicePanel != null) choicePanel.SetActive(true);
                    return;
                }

                if (isBuying)
                {
                    var economy = FindAnyObjectByType<EconomyManager>();
                    if (economy != null && inventoryManager != null)
                    {
                        if (economy.BuyItemToInventory(currentTradeItem, selectedTradeQty, inventoryManager))
                        {
                            Debug.Log($"[Trade] Đã mua {selectedTradeQty}x {currentTradeItem.itemName}");
                        }
                    }
                    tradeQuantityPanel.SetActive(false);
                    UpdateDialogueUI();
                }
                else
                {
                    var economy = FindAnyObjectByType<EconomyManager>();
                    if (economy != null && inventoryManager != null)
                    {
                        int sellPrice = GetSalePriceOnVendor(currentTradeItem);
                        int finalRevenue = sellPrice * selectedTradeQty;
                        if (economy.SellItemWholesale(currentTradeItem, selectedTradeQty, inventoryManager, finalRevenue))
                        {
                            Debug.Log($"[Trade] Đã bán lẻ {selectedTradeQty}x {currentTradeItem.itemName}");
                        }
                    }
                    tradeQuantityPanel.SetActive(false);
                    UpdateDialogueUI();
                }
            }
        }

        private void CancelTrade()
        {
            tradeQuantityPanel.SetActive(false);
            if (isPriceMode)
            {
                var bargainingSystem = FindAnyObjectByType<ChoNoiMienTay.Systems.BargainingSystem>();
                if (bargainingSystem != null)
                {
                    bargainingSystem.RejectDeal();
                    if (activeNpc != null) activeNpc.HasTraded = true;
                    justFinishedTrade = true;
                }
                dialogueState = DialogueState.MerchantGreeting;
                UpdateDialogueUI();
            }
            else
            {
                if (dialogueState != DialogueState.Closed && choicePanel != null)
                {
                    choicePanel.SetActive(true);
                }
            }
        }

        // ==========================================
        // UPGRADE & MAINTENANCE BOATYARD PANEL (TRẠI GHE)
        // ==========================================
        private void BuildBoatYardPanel(Transform parent)
        {
            boatYardPanel = CreatePanel("BoatYardPanel", parent, new Color(0.06f, 0.1f, 0.12f, 0.98f));
            Stretch(boatYardPanel.GetComponent<RectTransform>(), new Vector2(0.20f, 0.15f), new Vector2(0.80f, 0.85f));

            yardTitleText = CreateText("Title", boatYardPanel.transform, 30, TextAnchor.MiddleCenter);
            yardTitleText.color = new Color(0.92f, 0.82f, 0.55f, 1f);
            Stretch(yardTitleText.rectTransform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));

            GameObject repairPanel = CreatePanel("RepairSection", boatYardPanel.transform, new Color(0.04f, 0.06f, 0.08f, 0.9f));
            Stretch(repairPanel.GetComponent<RectTransform>(), new Vector2(0.05f, 0.65f), new Vector2(0.95f, 0.85f));
            
            yardDurabilityText = CreateText("DurabilityLabel", repairPanel.transform, 20, TextAnchor.MiddleLeft);
            Stretch(yardDurabilityText.rectTransform, new Vector2(0.04f, 0.55f), new Vector2(0.50f, 0.85f));

            GameObject dSliderObj = new GameObject("DurabilityBar", typeof(RectTransform), typeof(Slider));
            dSliderObj.transform.SetParent(repairPanel.transform, false);
            Stretch(dSliderObj.GetComponent<RectTransform>(), new Vector2(0.04f, 0.20f), new Vector2(0.50f, 0.45f));
            yardDurabilitySlider = dSliderObj.GetComponent<Slider>();
            yardDurabilitySlider.interactable = false;
            
            GameObject dSliderBg = CreatePanel("Bg", dSliderObj.transform, new Color(0.15f, 0.15f, 0.15f, 1f));
            Stretch(dSliderBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            
            GameObject dSliderFillArea = new GameObject("FillArea", typeof(RectTransform));
            dSliderFillArea.transform.SetParent(dSliderObj.transform, false);
            Stretch(dSliderFillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            
            GameObject dSliderFill = CreatePanel("Fill", dSliderFillArea.transform, new Color(0.2f, 0.6f, 0.2f, 1f));
            Stretch(dSliderFill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            yardDurabilitySlider.fillRect = dSliderFill.GetComponent<RectTransform>();

            yardRepairButton = CreateActionButton(repairPanel.transform, "SỬA CHỮA GHE", new Vector2(0.60f, 0.20f), new Vector2(0.94f, 0.80f), YardRepairBoat);
            yardRepairButton.GetComponent<Image>().color = new Color(0.18f, 0.38f, 0.22f, 1f);

            GameObject upgradesSection = CreatePanel("UpgradesSection", boatYardPanel.transform, new Color(0.04f, 0.06f, 0.08f, 0.9f));
            Stretch(upgradesSection.GetComponent<RectTransform>(), new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.60f));

            GameObject colStorage = CreatePanel("ColStorage", upgradesSection.transform, new Color(0.08f, 0.10f, 0.12f, 0.9f));
            Stretch(colStorage.GetComponent<RectTransform>(), new Vector2(0.02f, 0.05f), new Vector2(0.24f, 0.95f));
            upgradeStorageText = CreateText("StorageInfo", colStorage.transform, 16, TextAnchor.UpperCenter);
            Stretch(upgradeStorageText.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.95f));
            upgradeStorageButton = CreateActionButton(colStorage.transform, "NÂNG CẤP KHOANG", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f), YardBuyStorage);

            GameObject colEngine = CreatePanel("ColEngine", upgradesSection.transform, new Color(0.08f, 0.10f, 0.12f, 0.9f));
            Stretch(colEngine.GetComponent<RectTransform>(), new Vector2(0.26f, 0.05f), new Vector2(0.48f, 0.95f));
            upgradeEngineText = CreateText("EngineInfo", colEngine.transform, 16, TextAnchor.UpperCenter);
            Stretch(upgradeEngineText.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.95f));
            upgradeEngineButton = CreateActionButton(colEngine.transform, "NÂNG CẤP MÁY", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f), YardBuyEngine);

            GameObject colRoof = CreatePanel("ColRoof", upgradesSection.transform, new Color(0.08f, 0.10f, 0.12f, 0.9f));
            Stretch(colRoof.GetComponent<RectTransform>(), new Vector2(0.50f, 0.05f), new Vector2(0.72f, 0.95f));
            upgradeRoofText = CreateText("RoofInfo", colRoof.transform, 16, TextAnchor.UpperCenter);
            Stretch(upgradeRoofText.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.95f));
            upgradeRoofButton = CreateActionButton(colRoof.transform, "LỢP MÁI", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f), YardBuyRoof);

            GameObject colBamboo = CreatePanel("ColBamboo", upgradesSection.transform, new Color(0.08f, 0.10f, 0.12f, 0.9f));
            Stretch(colBamboo.GetComponent<RectTransform>(), new Vector2(0.74f, 0.05f), new Vector2(0.96f, 0.95f));
            upgradeBambooText = CreateText("BambooInfo", colBamboo.transform, 16, TextAnchor.UpperCenter);
            Stretch(upgradeBambooText.rectTransform, new Vector2(0.05f, 0.30f), new Vector2(0.95f, 0.95f));
            upgradeBambooButton = CreateActionButton(colBamboo.transform, "NÂNG SÀO BẸO", new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.25f), YardBuyBamboo);

            CreateActionButton(boatYardPanel.transform, "ĐÓNG TRẠI GHE", new Vector2(0.40f, 0.03f), new Vector2(0.60f, 0.10f), CloseBoatYardPanel);

            boatYardPanel.SetActive(false);
        }

        public void OpenBoatYardPanel()
        {
            if (boatYardPanel != null)
            {
                boatYardPanel.SetActive(true);
                RefreshBoatYardUI();
            }
        }

        public void CloseBoatYardPanel()
        {
            if (boatYardPanel != null)
            {
                boatYardPanel.SetActive(false);
            }
        }

        private void RefreshBoatYardUI()
        {
            if (boatCampManager == null) return;

            var durability = FindAnyObjectByType<DurabilityManager>();
            if (durability != null)
            {
                yardDurabilityText.text = $"Độ bền ghe: {durability.CurrentDurability:0} / {durability.MaxDurability:0}";
                yardDurabilitySlider.minValue = 0;
                yardDurabilitySlider.maxValue = durability.MaxDurability;
                yardDurabilitySlider.value = durability.CurrentDurability;

                int repairCost = GetRepairCostOnVendor();
                yardRepairButton.GetComponentInChildren<Text>().text = $"SỬA CHỮA GHE\n({repairCost:N0} VNĐ)";
                yardRepairButton.interactable = durability.CurrentDurability < durability.MaxDurability && playerStats.CurrentMoney >= repairCost;
            }

            BoatUpgradeCatalogSO catalog = boatCampManager.UpgradeCatalog;
            if (catalog != null)
            {
                StorageUpgradeTier storageTier = catalog.GetStorageTier(boatCampManager.StorageLevel);
                upgradeStorageText.text = $"KHOANG CHỨA\nCấp hiện tại: {boatCampManager.StorageLevel}\n" +
                                          $"{(storageTier != null ? $"Tiếp theo: +{storageTier.capacityBonus} kg\nGiá: {storageTier.costMoney:N0}đ" : "ĐÃ ĐẠT CẤP TỐI ĐA")}";
                upgradeStorageButton.interactable = storageTier != null && playerStats.CurrentMoney >= storageTier.costMoney;

                EngineUpgradeTier engineTier = catalog.GetEngineTier(boatCampManager.engineLevel);
                upgradeEngineText.text = $"ĐỘNG CƠ GHE\nCấp hiện tại: {boatCampManager.engineLevel}\n" +
                                        $"{(engineTier != null ? $"Tiếp theo: x{engineTier.thrustMultiplier} Lực đẩy\nGiá: {engineTier.costMoney:N0}đ" : "ĐÃ ĐẠT CẤP TỐI ĐA")}";
                upgradeEngineButton.interactable = engineTier != null && playerStats.CurrentMoney >= engineTier.costMoney;

                RoofUpgradeTier roofTier = catalog.GetRoofTier(boatCampManager.hasRoof ? 1 : 0);
                upgradeRoofText.text = $"MÁI CHE GHE\nTrạng thái: {(boatCampManager.hasRoof ? "Đã lợp mái" : "Chưa có mái")}\n" +
                                      $"{(!boatCampManager.hasRoof && roofTier != null ? $"Lợp mái che\nGiá: {roofTier.costMoney:N0}đ" : "ĐÃ ĐẠT CẤP TỐI ĐA")}";
                upgradeRoofButton.interactable = !boatCampManager.hasRoof && roofTier != null && playerStats.CurrentMoney >= roofTier.costMoney;

                BambooPoleUpgradeTier bambooTier = catalog.GetBambooPoleTier(boatCampManager.bambooPoleLevel);
                upgradeBambooText.text = $"SÀO CÂY BẸO\nCấp hiện tại: {boatCampManager.bambooPoleLevel}\n" +
                                        $"{(bambooTier != null ? $"Tiếp theo: +1 Slot treo\nGiá: {bambooTier.costMoney:N0}đ" : "ĐÃ ĐẠT CẤP TỐI ĐA")}";
                upgradeBambooButton.interactable = bambooTier != null && playerStats.CurrentMoney >= bambooTier.costMoney;
            }
        }

        private void YardBuyStorage()
        {
            if (boatCampManager != null && boatCampManager.TryBuyNextStorageUpgrade())
            {
                RefreshBoatYardUI();
                if (riverMarketHUD != null) riverMarketHUD.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void YardBuyEngine()
        {
            if (boatCampManager != null && boatCampManager.TryBuyNextEngineUpgrade())
            {
                RefreshBoatYardUI();
                if (riverMarketHUD != null) riverMarketHUD.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void YardBuyRoof()
        {
            if (boatCampManager != null && boatCampManager.TryBuyRoofUpgrade())
            {
                RefreshBoatYardUI();
                if (riverMarketHUD != null) riverMarketHUD.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void YardBuyBamboo()
        {
            if (boatCampManager != null && boatCampManager.TryBuyNextBambooPoleUpgrade())
            {
                RefreshBoatYardUI();
                if (riverMarketHUD != null) riverMarketHUD.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);
            }
        }

        private void YardRepairBoat()
        {
            if (boatCampManager != null)
            {
                RepairBoatOnVendor();
                RefreshBoatYardUI();
                if (riverMarketHUD != null) riverMarketHUD.SendMessage("RefreshAll", SendMessageOptions.DontRequireReceiver);
            }
        }

        // ==========================================
        // CARGO SLOT CLICK/DRAG ACTIONS
        // ==========================================
        public void HandleCargoSlotClicked(int index)
        {
            if (selectedCargoSlotIndex == -1)
            {
                if (inventoryManager.CargoSlots[index].item != null)
                {
                    selectedCargoSlotIndex = index;
                    RefreshMarketing();
                }
            }
            else
            {
                if (selectedCargoSlotIndex == index)
                {
                    selectedCargoSlotIndex = -1;
                    RefreshMarketing();
                }
                else
                {
                    inventoryManager.SwapSlots(selectedCargoSlotIndex, index);
                    selectedCargoSlotIndex = -1;
                    RefreshMarketing();
                }
            }
        }

        public void HandleCargoSlotDropped(int fromSlotIndex, int toSlotIndex)
        {
            if (fromSlotIndex != -1 && fromSlotIndex != toSlotIndex)
            {
                inventoryManager.SwapSlots(fromSlotIndex, toSlotIndex);
                RefreshMarketing();
            }
        }

        private void MakeScrollable(GameObject parentPanel, GameObject gridParent, Vector2 minAnchor, Vector2 maxAnchor)
        {
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(UnityEngine.UI.RectMask2D));
            viewport.transform.SetParent(parentPanel.transform, false);
            Stretch(viewport.GetComponent<RectTransform>(), minAnchor, maxAnchor);

            gridParent.transform.SetParent(viewport.transform, false);

            RectTransform contentRT = gridParent.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 300f);

            var fitter = gridParent.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = gridParent.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            var scrollRect = viewport.AddComponent<UnityEngine.UI.ScrollRect>();
            scrollRect.content = contentRT;
            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 20f;
        }

        private void HandleDayChanged(int day)
        {
            ResetNpcsTradeState();
        }

        private void ResetNpcsTradeState()
        {
            var npcs = FindObjectsByType<NpcTradeTarget>(FindObjectsSortMode.None);
            foreach (var npc in npcs)
            {
                npc.HasTraded = false;
            }
        }

        private void OnDestroy()
        {
            var timeManager = FindAnyObjectByType<TimeManager>();
            if (timeManager != null)
            {
                timeManager.OnDayChanged -= HandleDayChanged;
            }
        }
    }

    // ==========================================
    // DRAG AND DROP / CLICK HANDLERS HELPERS
    // ==========================================

    public class UIDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        public ItemData item;
        public FullSimulatorUI parentUI;
        public int fromCargoSlotIndex = -1; // Chỉ số ô hàng gốc (-1 nếu từ nguồn khác)

        private GameObject dragObject;
        private Canvas canvas;

        public void OnBeginDrag(PointerEventData eventData)
        {
            canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            dragObject = new GameObject("DragIcon", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            if (parentUI != null)
            {
                if (parentUI.activeDragObject != null) Destroy(parentUI.activeDragObject);
                parentUI.activeDragObject = dragObject;
            }
            dragObject.transform.SetParent(canvas.transform, false);
            dragObject.transform.SetAsLastSibling();
            
            RectTransform rt = dragObject.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(90f, 90f);
            
            Image img = dragObject.GetComponent<Image>();
            img.color = new Color(0.88f, 0.71f, 0.34f, 0.85f);
            
            CanvasGroup group = dragObject.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;

            UpdatePosition(eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            UpdatePosition(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (dragObject != null) Destroy(dragObject);
            if (parentUI != null)
            {
                if (parentUI.activeDragObject == dragObject)
                    parentUI.activeDragObject = null;
                parentUI.HandleItemDropped(item, eventData.position);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (fromCargoSlotIndex != -1 && parentUI != null)
            {
                parentUI.HandleCargoSlotClicked(fromCargoSlotIndex);
            }
            else if (parentUI != null)
            {
                parentUI.HandleItemClicked(item);
            }
        }

        private void UpdatePosition(Vector2 screenPos)
        {
            if (dragObject == null || canvas == null) return;
            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                screenPos,
                canvas.worldCamera,
                out localPos);
            dragObject.GetComponent<RectTransform>().anchoredPosition = localPos;
        }
    }

    public class UIPoleSlotHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public int slotIndex;
        public FullSimulatorUI parentUI;

        public void OnDrop(PointerEventData eventData)
        {
            var drag = eventData.pointerDrag?.GetComponent<UIDragHandler>();
            if (drag != null && parentUI != null)
            {
                parentUI.HandleItemDroppedOnSlot(drag.item, slotIndex);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentUI != null)
            {
                parentUI.HandleSlotClicked(slotIndex);
            }
        }
    }

    public class UICargoGridSlotHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        public int slotIndex;
        public FullSimulatorUI parentUI;
        public ItemData item;

        public void OnDrop(PointerEventData eventData)
        {
            var drag = eventData.pointerDrag?.GetComponent<UIDragHandler>();
            if (drag != null && parentUI != null)
            {
                if (drag.fromCargoSlotIndex != -1)
                {
                    parentUI.HandleCargoSlotDropped(drag.fromCargoSlotIndex, slotIndex);
                }
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (parentUI != null)
            {
                parentUI.HandleCargoSlotClicked(slotIndex);
            }
        }
    }
}
