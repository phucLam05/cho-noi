using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ChoNoi.Application;
using ChoNoi.Infrastructure;
using ChoNoiMienTay.Infrastructure;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.UI;

namespace ChoNoi.UI
{
    public class FullSimulatorUI : MonoBehaviour
    {
        [Header("Managers")]
        public BambooPoleManager bambooPoleManager;
        public InventoryManager inventoryManager;
        public RiverMarketHUD riverMarketHUD;

        private GameObject canvasObject;
        private GameObject tutorialPanel;
        private GameObject marketingPanel;
        private GameObject dialoguePanel;
        private GameObject settingsPanel;

        private Text marketingText;
        private Text dialogueText;
        private Text npcNameText;
        private Image npcAvatar;

        private DialogueNode currentNode;

        private void Start()
        {
            BuildExtraUI();
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                ToggleMarketing();
            }
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

            // Top right buttons for new features
            CreateActionButton(canvasObject.transform, "Huong Dan", new Vector2(0.66f, 0.90f), new Vector2(0.73f, 0.96f), ToggleTutorial);
            CreateActionButton(canvasObject.transform, "Cai Dat", new Vector2(0.58f, 0.90f), new Vector2(0.65f, 0.96f), ToggleSettings);

            // Tutorial Panel
            tutorialPanel = CreatePanel("TutorialPanel", canvasObject.transform, new Color(0.1f, 0.1f, 0.1f, 0.9f));
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
            settingsPanel = CreatePanel("SettingsPanel", canvasObject.transform, new Color(0.1f, 0.1f, 0.2f, 0.9f));
            Stretch(settingsPanel.GetComponent<RectTransform>(), new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f));
            CreateText("Title", settingsPanel.transform, 32, TextAnchor.MiddleCenter).text = "CAI DAT";
            Stretch(settingsPanel.transform.Find("Title").GetComponent<RectTransform>(), new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.95f));
            CreateActionButton(settingsPanel.transform, "Am Thanh: ON", new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.65f), () => {});
            CreateActionButton(settingsPanel.transform, "Do Hoa: CAO", new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.45f), () => {});
            CreateActionButton(settingsPanel.transform, "Dong", new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.2f), ToggleSettings);
            settingsPanel.SetActive(false);

            // Marketing Panel (Cay Beo)
            marketingPanel = CreatePanel("MarketingPanel", canvasObject.transform, new Color(0.15f, 0.25f, 0.15f, 0.9f));
            Stretch(marketingPanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.08f), new Vector2(0.3f, 0.44f));
            CreateText("Title", marketingPanel.transform, 24, TextAnchor.MiddleCenter).text = "QUAN LY CAY BEO";
            Stretch(marketingPanel.transform.Find("Title").GetComponent<RectTransform>(), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f));
            marketingText = CreateText("Items", marketingPanel.transform, 20, TextAnchor.UpperLeft);
            Stretch(marketingText.rectTransform, new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.8f));
            CreateActionButton(marketingPanel.transform, "Treo Khom", new Vector2(0.05f, 0.2f), new Vector2(0.45f, 0.3f), () => HangItemByName("Khom"));
            CreateActionButton(marketingPanel.transform, "Treo Bi Dao", new Vector2(0.55f, 0.2f), new Vector2(0.95f, 0.3f), () => HangItemByName("Bi Dao"));
            CreateActionButton(marketingPanel.transform, "Go Tat Ca", new Vector2(0.05f, 0.05f), new Vector2(0.45f, 0.15f), ClearPole);
            marketingPanel.SetActive(false);

            // Dialogue Panel
            dialoguePanel = CreatePanel("DialoguePanel", canvasObject.transform, new Color(0.1f, 0.1f, 0.1f, 0.95f));
            Stretch(dialoguePanel.GetComponent<RectTransform>(), new Vector2(0.2f, 0.05f), new Vector2(0.8f, 0.35f));
            npcAvatar = CreateImage("Avatar", dialoguePanel.transform, Color.white);
            Stretch(npcAvatar.rectTransform, new Vector2(0.02f, 0.1f), new Vector2(0.18f, 0.9f));
            npcNameText = CreateText("Name", dialoguePanel.transform, 24, TextAnchor.MiddleLeft);
            Stretch(npcNameText.rectTransform, new Vector2(0.2f, 0.75f), new Vector2(0.95f, 0.95f));
            dialogueText = CreateText("Dialogue", dialoguePanel.transform, 22, TextAnchor.UpperLeft);
            Stretch(dialogueText.rectTransform, new Vector2(0.2f, 0.3f), new Vector2(0.95f, 0.7f));

            CreateActionButton(dialoguePanel.transform, "Noi Ngot (-10 TL)", new Vector2(0.2f, 0.05f), new Vector2(0.4f, 0.25f), () => HandleDialogueAction(DialogueAction.Haggle));
            CreateActionButton(dialoguePanel.transform, "Tang Qua", new Vector2(0.45f, 0.05f), new Vector2(0.65f, 0.25f), () => HandleDialogueAction(DialogueAction.GiveGift));
            CreateActionButton(dialoguePanel.transform, "Thoat", new Vector2(0.7f, 0.05f), new Vector2(0.9f, 0.25f), CloseDialogue);
            dialoguePanel.SetActive(false);
        }

        private void ToggleTutorial() => tutorialPanel.SetActive(!tutorialPanel.activeSelf);
        private void ToggleSettings() => settingsPanel.SetActive(!settingsPanel.activeSelf);
        
        public void ToggleMarketing()
        {
            if (marketingPanel == null) return;
            marketingPanel.SetActive(!marketingPanel.activeSelf);
            if (marketingPanel.activeSelf) RefreshMarketing();
        }

        public void OpenMarketingPanel()
        {
            if (marketingPanel != null && !marketingPanel.activeSelf)
            {
                marketingPanel.SetActive(true);
                RefreshMarketing();
            }
        }

        public void CloseMarketingPanel()
        {
            if (marketingPanel != null)
                marketingPanel.SetActive(false);
        }

        private void RefreshMarketing()
        {
            if (bambooPoleManager == null)
            {
                marketingText.text = "Khong tim thay BambooPoleManager.";
                return;
            }

            marketingText.text = $"Dang treo: {bambooPoleManager.DisplayedItems.Count} / {bambooPoleManager.MaxDisplayedItems}\n\n";
            foreach (var item in bambooPoleManager.DisplayedItems)
            {
                marketingText.text += $"- {item.itemName}\n";
            }
        }

        private void HangItemByName(string name)
        {
            if (inventoryManager == null || bambooPoleManager == null) return;
            foreach (var kvp in inventoryManager.Inventory)
            {
                if (kvp.Key.itemName.Contains(name))
                {
                    bambooPoleManager.HangItem(kvp.Key);
                    RefreshMarketing();
                    return;
                }
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

        public void OpenDialogue(DialogueData data)
        {
            if (data == null || data.nodes.Count == 0) return;
            dialoguePanel.SetActive(true);
            npcAvatar.sprite = data.npcAvatar;
            PlayNode(data.GetNode(data.initialNodeId) ?? data.nodes[0]);
        }

        private void PlayNode(DialogueNode node)
        {
            if (node == null)
            {
                CloseDialogue();
                return;
            }
            currentNode = node;
            npcNameText.text = node.speakerName;
            dialogueText.text = node.dialogueText;
        }

        private void HandleDialogueAction(DialogueAction action)
        {
            // Simple mock for Dialogue interaction
            if (action == DialogueAction.Haggle)
            {
                dialogueText.text = "Ban da noi ngot. NPC rat vui va giam gia nhe!";
            }
            else if (action == DialogueAction.GiveGift)
            {
                dialogueText.text = "Ban da tang qua. Thien cam tang vut!";
            }
        }

        private void CloseDialogue()
        {
            dialoguePanel.SetActive(false);
        }

        // Helpers
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
    }
}
