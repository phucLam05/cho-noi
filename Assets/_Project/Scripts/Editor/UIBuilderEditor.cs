#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChoNoiMienTay.UI;
using ChoNoiMienTay.Presentation;
using ChoNoi.UI;

namespace ChoNoiMienTay.Editor
{
    public class UIBuilderEditor
    {
        private const string PrefabFolder = "Assets/_Project/Prefabs/UI";
        private const string KenneyRpgFolder = "Assets/_Project/Art/UI/Kenney/rpg/PNG";

        [MenuItem("ChoNoi/UI/Generate Stylized UI Prefabs")]
        public static void GeneratePrefabs()
        {
            Directory.CreateDirectory(PrefabFolder);

            // 1. Configure Sprite Border settings (9-Slicing) for Kenney Sprites
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "panel_brown.png"), new Vector4(20, 20, 20, 20));
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "panel_beige.png"), new Vector4(20, 20, 20, 20));
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "panelInset_beigeLight.png"), new Vector4(12, 12, 12, 12));
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "buttonLong_brown.png"), new Vector4(10, 10, 10, 10));
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "buttonLong_brown_pressed.png"), new Vector4(10, 10, 10, 10));
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "buttonSquare_brown.png"), new Vector4(8, 8, 8, 8));
            ConfigureSprite(Path.Combine(KenneyRpgFolder, "buttonSquare_brown_pressed.png"), new Vector4(8, 8, 8, 8));
            
            AssetDatabase.Refresh();

            // Load Sprites
            Sprite panelBrown = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(KenneyRpgFolder, "panel_brown.png"));
            Sprite panelBeige = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(KenneyRpgFolder, "panel_beige.png"));
            Sprite panelBeigeLight = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(KenneyRpgFolder, "panelInset_beigeLight.png"));
            Sprite buttonBrown = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(KenneyRpgFolder, "buttonLong_brown.png"));
            Sprite buttonBrownPressed = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(KenneyRpgFolder, "buttonLong_brown_pressed.png"));
            Sprite slotSquare = AssetDatabase.LoadAssetAtPath<Sprite>(Path.Combine(KenneyRpgFolder, "buttonSquare_brown.png"));

            // Load standard TMPro font asset (Vietnamese compatible default fallback)
            TMP_FontAsset fontAsset = null;
            try
            {
                fontAsset = TMP_Settings.defaultFontAsset;
            }
            catch (System.Exception) {}

            if (fontAsset == null)
            {
                fontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            }

            // Try loading Be Vietnam Pro SDF if it exists
            TMP_FontAsset beVietnamSDF = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Project/Art/Fonts/BeVietnamPro-Regular SDF.asset");
            if (beVietnamSDF != null)
            {
                fontAsset = beVietnamSDF;
            }

            // 2. Build Reusable UIButton Prefab
            GameObject btnObj = CreateUIElement("UIButton", typeof(Image), typeof(Button));
            btnObj.GetComponent<Image>().sprite = buttonBrown;
            btnObj.GetComponent<Image>().type = Image.Type.Sliced;
            Button btnComponent = btnObj.GetComponent<Button>();
            
            // Configure SpriteState for Hover/Pressed
            btnComponent.transition = Selectable.Transition.SpriteSwap;
            SpriteState state = new SpriteState();
            state.highlightedSprite = buttonBrown;
            state.pressedSprite = buttonBrownPressed;
            state.selectedSprite = buttonBrown;
            btnComponent.spriteState = state;

            GameObject btnTextObj = CreateUIElement("Label", typeof(TextMeshProUGUI), btnObj.transform);
            TextMeshProUGUI btnText = btnTextObj.GetComponent<TextMeshProUGUI>();
            btnText.font = fontAsset;
            btnText.fontSize = 20;
            btnText.text = "BUTTON";
            btnText.color = new Color(0.92f, 0.82f, 0.55f, 1f); // Gold
            btnText.alignment = TextAlignmentOptions.Center;
            Stretch(btnText.rectTransform, Vector2.zero, Vector2.one);
            
            GameObject btnPrefab = SavePrefab(btnObj, "UIButton");

            // 3. Build Reusable UIPanel Prefab (Stylized Wood-Frame Paper Box)
            GameObject panelObj = CreateUIElement("UIPanel", typeof(Image));
            panelObj.GetComponent<Image>().sprite = panelBrown;
            panelObj.GetComponent<Image>().type = Image.Type.Sliced;
            
            GameObject panelContent = CreateUIElement("ContentBg", typeof(Image), panelObj.transform);
            panelContent.GetComponent<Image>().sprite = panelBeigeLight;
            panelContent.GetComponent<Image>().type = Image.Type.Sliced;
            Stretch(panelContent.GetComponent<RectTransform>(), new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f));

            GameObject panelPrefab = SavePrefab(panelObj, "UIPanel");

            // 4. Build Reusable Inventory Slot Prefab
            GameObject slotObj = CreateUIElement("InventorySlot", typeof(Image), typeof(Button));
            slotObj.GetComponent<Image>().sprite = slotSquare;
            slotObj.GetComponent<Image>().type = Image.Type.Sliced;
            slotObj.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 80);

            GameObject slotIcon = CreateUIElement("Icon", typeof(Image), slotObj.transform);
            slotIcon.GetComponent<Image>().color = Color.white;
            slotIcon.SetActive(false);
            Stretch(slotIcon.GetComponent<RectTransform>(), new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));

            GameObject slotCount = CreateUIElement("Count", typeof(TextMeshProUGUI), slotObj.transform);
            TextMeshProUGUI slotCountText = slotCount.GetComponent<TextMeshProUGUI>();
            slotCountText.font = fontAsset;
            slotCountText.fontSize = 16;
            slotCountText.alignment = TextAlignmentOptions.BottomRight;
            slotCountText.text = "1";
            slotCountText.color = Color.white;
            Stretch(slotCountText.rectTransform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));

            GameObject slotPrefab = SavePrefab(slotObj, "InventorySlot");

            // 5. Build Reusable Bamboo Marketing Slot Prefab
            GameObject bSlotObj = CreateUIElement("BambooSlot", typeof(Image), typeof(Button));
            bSlotObj.GetComponent<Image>().sprite = buttonBrown;
            bSlotObj.GetComponent<Image>().type = Image.Type.Sliced;
            bSlotObj.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 60);

            GameObject bIcon = CreateUIElement("Icon", typeof(Image), bSlotObj.transform);
            bIcon.SetActive(false);
            Stretch(bIcon.GetComponent<RectTransform>(), new Vector2(0.05f, 0.1f), new Vector2(0.22f, 0.9f));

            GameObject bLabel = CreateUIElement("Label", typeof(TextMeshProUGUI), bSlotObj.transform);
            bLabel.GetComponent<TextMeshProUGUI>().font = fontAsset;
            bLabel.GetComponent<TextMeshProUGUI>().fontSize = 18;
            bLabel.GetComponent<TextMeshProUGUI>().text = "Trống";
            bLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.MidlineLeft;
            Stretch(bLabel.GetComponent<RectTransform>(), new Vector2(0.25f, 0f), new Vector2(0.75f, 1f));

            GameObject bRemove = CreateUIElement("RemoveButton", typeof(Image), typeof(Button), bSlotObj.transform);
            bRemove.GetComponent<Image>().sprite = buttonBrownPressed;
            bRemove.GetComponent<Image>().type = Image.Type.Sliced;
            bRemove.SetActive(false);
            Stretch(bRemove.GetComponent<RectTransform>(), new Vector2(0.78f, 0.15f), new Vector2(0.95f, 0.85f));
            
            GameObject bRemoveText = CreateUIElement("Text", typeof(TextMeshProUGUI), bRemove.transform);
            bRemoveText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            bRemoveText.GetComponent<TextMeshProUGUI>().fontSize = 14;
            bRemoveText.GetComponent<TextMeshProUGUI>().text = "Gỡ";
            bRemoveText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(bRemoveText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject bambooSlotPrefabObj = SavePrefab(bSlotObj, "BambooSlot");

            // 6. Build Tooltip Prefab
            GameObject ttObj = InstantiatePrefab(panelPrefab);
            ttObj.name = "Tooltip";
            ttObj.AddComponent<CanvasGroup>();
            ttObj.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 150);
            ttObj.GetComponent<RectTransform>().pivot = new Vector2(0, 1);
            
            Transform ttBg = ttObj.transform.Find("ContentBg");
            GameObject ttIcon = CreateUIElement("Icon", typeof(Image), ttBg);
            Stretch(ttIcon.GetComponent<RectTransform>(), new Vector2(0.08f, 0.6f), new Vector2(0.28f, 0.92f));

            GameObject ttName = CreateUIElement("Name", typeof(TextMeshProUGUI), ttBg);
            ttName.GetComponent<TextMeshProUGUI>().font = fontAsset;
            ttName.GetComponent<TextMeshProUGUI>().fontSize = 18;
            ttName.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            ttName.GetComponent<TextMeshProUGUI>().text = "Tên quả";
            Stretch(ttName.GetComponent<RectTransform>(), new Vector2(0.32f, 0.6f), new Vector2(0.92f, 0.92f));

            GameObject ttDesc = CreateUIElement("Description", typeof(TextMeshProUGUI), ttBg);
            ttDesc.GetComponent<TextMeshProUGUI>().font = fontAsset;
            ttDesc.GetComponent<TextMeshProUGUI>().fontSize = 14;
            ttDesc.GetComponent<TextMeshProUGUI>().text = "Mô tả quả...";
            Stretch(ttDesc.GetComponent<RectTransform>(), new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.55f));

            GameObject ttPrice = CreateUIElement("Price", typeof(TextMeshProUGUI), ttBg);
            ttPrice.GetComponent<TextMeshProUGUI>().font = fontAsset;
            ttPrice.GetComponent<TextMeshProUGUI>().fontSize = 14;
            ttPrice.GetComponent<TextMeshProUGUI>().text = "Giá:";
            ttPrice.GetComponent<TextMeshProUGUI>().color = new Color(0.92f, 0.82f, 0.55f, 1f);
            Stretch(ttPrice.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.5f, 0.28f));

            GameObject ttCount = CreateUIElement("Count", typeof(TextMeshProUGUI), ttBg);
            ttCount.GetComponent<TextMeshProUGUI>().font = fontAsset;
            ttCount.GetComponent<TextMeshProUGUI>().fontSize = 14;
            ttCount.GetComponent<TextMeshProUGUI>().text = "Đang có:";
            ttCount.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            Stretch(ttCount.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(0.92f, 0.28f));

            TooltipUI ttUI = ttObj.AddComponent<TooltipUI>();
            ttUI.GetType().GetField("tooltipPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ttUI, ttObj);
            ttUI.GetType().GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ttUI, ttIcon.GetComponent<Image>());
            ttUI.GetType().GetField("itemNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ttUI, ttName.GetComponent<TextMeshProUGUI>());
            ttUI.GetType().GetField("itemDescriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ttUI, ttDesc.GetComponent<TextMeshProUGUI>());
            ttUI.GetType().GetField("sellPriceText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ttUI, ttPrice.GetComponent<TextMeshProUGUI>());
            ttUI.GetType().GetField("countText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(ttUI, ttCount.GetComponent<TextMeshProUGUI>());

            GameObject tooltipPrefab = SavePrefab(ttObj, "Tooltip");

            // 7. Build HUD Prefab
            GameObject hudObj = CreateUIElement("HUD", typeof(RectTransform));
            hudObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);

            // Time & Day Panel (Top Left)
            GameObject timePanel = InstantiatePrefab(panelPrefab);
            timePanel.name = "TimePanel";
            timePanel.transform.SetParent(hudObj.transform, false);
            Stretch(timePanel.GetComponent<RectTransform>(), new Vector2(0.02f, 0.88f), new Vector2(0.24f, 0.98f));
            Transform timeBg = timePanel.transform.Find("ContentBg");
            
            GameObject tDayText = CreateUIElement("DayText", typeof(TextMeshProUGUI), timeBg);
            tDayText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tDayText.GetComponent<TextMeshProUGUI>().fontSize = 22;
            tDayText.GetComponent<TextMeshProUGUI>().text = "Ngày 1";
            Stretch(tDayText.GetComponent<RectTransform>(), new Vector2(0.08f, 0.5f), new Vector2(0.92f, 0.9f));

            GameObject tTimeText = CreateUIElement("TimeText", typeof(TextMeshProUGUI), timeBg);
            tTimeText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tTimeText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            tTimeText.GetComponent<TextMeshProUGUI>().text = "05:00";
            Stretch(tTimeText.GetComponent<RectTransform>(), new Vector2(0.08f, 0.1f), new Vector2(0.5f, 0.45f));

            GameObject tPhaseText = CreateUIElement("PhaseText", typeof(TextMeshProUGUI), timeBg);
            tPhaseText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tPhaseText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            tPhaseText.GetComponent<TextMeshProUGUI>().text = "Bình Minh";
            tPhaseText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            Stretch(tPhaseText.GetComponent<RectTransform>(), new Vector2(0.52f, 0.1f), new Vector2(0.92f, 0.45f));

            // Money Panel (Top Right)
            GameObject moneyPanelObj = InstantiatePrefab(panelPrefab);
            moneyPanelObj.name = "MoneyPanel";
            moneyPanelObj.transform.SetParent(hudObj.transform, false);
            Stretch(moneyPanelObj.GetComponent<RectTransform>(), new Vector2(0.76f, 0.88f), new Vector2(0.98f, 0.98f));
            Transform moneyBg = moneyPanelObj.transform.Find("ContentBg");
            
            GameObject tMoneyText = CreateUIElement("MoneyText", typeof(TextMeshProUGUI), moneyBg);
            tMoneyText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tMoneyText.GetComponent<TextMeshProUGUI>().fontSize = 24;
            tMoneyText.GetComponent<TextMeshProUGUI>().text = "100,000 VNĐ";
            tMoneyText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(tMoneyText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            // Shortcuts & Prompts (Bottom side-by-side keycaps)
            GameObject bPrompt = InstantiatePrefab(panelPrefab);
            bPrompt.name = "InventoryPrompt";
            bPrompt.transform.SetParent(hudObj.transform, false);
            Stretch(bPrompt.GetComponent<RectTransform>(), new Vector2(0.02f, 0.02f), new Vector2(0.18f, 0.08f));
            
            GameObject bPromptTextObj = CreateUIElement("Text", typeof(TextMeshProUGUI), bPrompt.transform.Find("ContentBg"));
            bPromptTextObj.GetComponent<TextMeshProUGUI>().font = fontAsset;
            bPromptTextObj.GetComponent<TextMeshProUGUI>().fontSize = 16;
            bPromptTextObj.GetComponent<TextMeshProUGUI>().text = "[B] Kho Hàng";
            bPromptTextObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(bPromptTextObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject ePrompt = InstantiatePrefab(panelPrefab);
            ePrompt.name = "InteractionPrompt";
            ePrompt.transform.SetParent(hudObj.transform, false);
            ePrompt.SetActive(false);
            Stretch(ePrompt.GetComponent<RectTransform>(), new Vector2(0.82f, 0.02f), new Vector2(0.98f, 0.08f));
            
            GameObject ePromptTextObj = CreateUIElement("Text", typeof(TextMeshProUGUI), ePrompt.transform.Find("ContentBg"));
            ePromptTextObj.GetComponent<TextMeshProUGUI>().font = fontAsset;
            ePromptTextObj.GetComponent<TextMeshProUGUI>().fontSize = 16;
            ePromptTextObj.GetComponent<TextMeshProUGUI>().text = "[E] Tương Tác";
            ePromptTextObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(ePromptTextObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            HUDController hudCtrl = hudObj.AddComponent<HUDController>();
            hudCtrl.GetType().GetField("dayText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, tDayText.GetComponent<TextMeshProUGUI>());
            hudCtrl.GetType().GetField("timeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, tTimeText.GetComponent<TextMeshProUGUI>());
            hudCtrl.GetType().GetField("phaseText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, tPhaseText.GetComponent<TextMeshProUGUI>());
            hudCtrl.GetType().GetField("moneyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, tMoneyText.GetComponent<TextMeshProUGUI>());
            hudCtrl.GetType().GetField("inventoryPrompt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, bPrompt);
            hudCtrl.GetType().GetField("interactionPrompt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, ePrompt);
            hudCtrl.GetType().GetField("interactionDescriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(hudCtrl, ePromptTextObj.GetComponent<TextMeshProUGUI>());

            GameObject hudPrefab = SavePrefab(hudObj, "HUD");

            // 8. Build Inventory Panel Prefab
            GameObject invPanelObj = InstantiatePrefab(panelPrefab);
            invPanelObj.name = "Inventory";
            invPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1000, 700);
            Transform invBg = invPanelObj.transform.Find("ContentBg");

            GameObject invTitle = CreateUIElement("Title", typeof(TextMeshProUGUI), invBg);
            invTitle.GetComponent<TextMeshProUGUI>().font = fontAsset;
            invTitle.GetComponent<TextMeshProUGUI>().fontSize = 28;
            invTitle.GetComponent<TextMeshProUGUI>().text = "KHOANG GHE & CÂY BẸO";
            invTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(invTitle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.90f), new Vector2(0.95f, 0.98f));

            // Grid for inventory (left side)
            GameObject invGridPanel = CreateUIElement("InventoryGridPanel", typeof(ScrollRect), invBg);
            Stretch(invGridPanel.GetComponent<RectTransform>(), new Vector2(0.04f, 0.15f), new Vector2(0.48f, 0.85f));
            
            GameObject viewPort = CreateUIElement("Viewport", typeof(RectMask2D), invGridPanel.transform);
            Stretch(viewPort.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            invGridPanel.GetComponent<ScrollRect>().viewport = viewPort.GetComponent<RectTransform>();

            GameObject gridContent = CreateUIElement("Content", typeof(GridLayoutGroup), viewPort.transform);
            GridLayoutGroup glg = gridContent.GetComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80, 80);
            glg.spacing = new Vector2(8, 8);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = 4;
            Stretch(gridContent.GetComponent<RectTransform>(), Vector2.zero, new Vector2(1, 0));
            gridContent.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
            invGridPanel.GetComponent<ScrollRect>().content = gridContent.GetComponent<RectTransform>();

            // Grid for Bamboo Pole (right side)
            GameObject poleGridPanel = CreateUIElement("BambooPolePanel", typeof(VerticalLayoutGroup), invBg);
            VerticalLayoutGroup vlg = poleGridPanel.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = false;
            Stretch(poleGridPanel.GetComponent<RectTransform>(), new Vector2(0.52f, 0.28f), new Vector2(0.96f, 0.85f));

            GameObject capText = CreateUIElement("CapacityText", typeof(TextMeshProUGUI), invBg);
            capText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            capText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            capText.GetComponent<TextMeshProUGUI>().text = "Tải trọng: 0/100 kg";
            Stretch(capText.GetComponent<RectTransform>(), new Vector2(0.04f, 0.08f), new Vector2(0.48f, 0.14f));

            GameObject instrText = CreateUIElement("InstructionText", typeof(TextMeshProUGUI), invBg);
            instrText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            instrText.GetComponent<TextMeshProUGUI>().fontSize = 16;
            instrText.GetComponent<TextMeshProUGUI>().text = "Bấm vào hàng ở bên trái để treo lên Cây Bẹo.";
            Stretch(instrText.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(0.96f, 0.25f));

            // Close button
            GameObject closeBtn = InstantiatePrefab(btnPrefab);
            closeBtn.name = "CloseButton";
            closeBtn.transform.SetParent(invBg, false);
            closeBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Đóng";
            Stretch(closeBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.02f), new Vector2(0.6f, 0.07f));

            ChoNoiMienTay.UI.InventoryUI invUI = invPanelObj.AddComponent<ChoNoiMienTay.UI.InventoryUI>();
            invUI.GetType().GetField("inventoryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, invPanelObj);
            invUI.GetType().GetField("boatInventoryGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, gridContent.transform);
            invUI.GetType().GetField("bambooPoleGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, poleGridPanel.transform);
            invUI.GetType().GetField("inventorySlotPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, slotPrefab);
            invUI.GetType().GetField("bambooSlotPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, bambooSlotPrefabObj);
            invUI.GetType().GetField("capacityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, capText.GetComponent<TextMeshProUGUI>());
            invUI.GetType().GetField("instructionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, instrText.GetComponent<TextMeshProUGUI>());
            invUI.GetType().GetField("closeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(invUI, closeBtn.GetComponent<Button>());

            GameObject inventoryPrefab = SavePrefab(invPanelObj, "Inventory");

            // 9. Build Dialogue Prefab
            GameObject diaPanelObj = InstantiatePrefab(panelPrefab);
            diaPanelObj.name = "Dialogue";
            diaPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1300, 220);
            Transform diaBg = diaPanelObj.transform.Find("ContentBg");

            GameObject diaNpcPortrait = CreateUIElement("NpcPortrait", typeof(Image), diaBg);
            Stretch(diaNpcPortrait.GetComponent<RectTransform>(), new Vector2(0.02f, 0.08f), new Vector2(0.14f, 0.92f));

            GameObject diaPlayerPortrait = CreateUIElement("PlayerPortrait", typeof(Image), diaBg);
            Stretch(diaPlayerPortrait.GetComponent<RectTransform>(), new Vector2(0.86f, 0.08f), new Vector2(0.98f, 0.92f));

            GameObject diaName = CreateUIElement("NpcName", typeof(TextMeshProUGUI), diaBg);
            diaName.GetComponent<TextMeshProUGUI>().font = fontAsset;
            diaName.GetComponent<TextMeshProUGUI>().fontSize = 20;
            diaName.GetComponent<TextMeshProUGUI>().text = "NPC NAME";
            diaName.GetComponent<TextMeshProUGUI>().color = new Color(0.92f, 0.82f, 0.55f, 1f); // Gold
            Stretch(diaName.GetComponent<RectTransform>(), new Vector2(0.16f, 0.72f), new Vector2(0.84f, 0.92f));

            GameObject diaText = CreateUIElement("DialogueText", typeof(TextMeshProUGUI), diaBg);
            diaText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            diaText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            diaText.GetComponent<TextMeshProUGUI>().text = "Dialogue goes here...";
            Stretch(diaText.GetComponent<RectTransform>(), new Vector2(0.16f, 0.1f), new Vector2(0.84f, 0.65f));

            // Choice Panel (Vertical List)
            GameObject diaChoices = CreateUIElement("ChoiceContainer", typeof(VerticalLayoutGroup), diaBg);
            VerticalLayoutGroup choiceVlg = diaChoices.GetComponent<VerticalLayoutGroup>();
            choiceVlg.spacing = 6;
            choiceVlg.childAlignment = TextAnchor.LowerCenter;
            choiceVlg.childForceExpandHeight = false;
            choiceVlg.childForceExpandWidth = false;
            Stretch(diaChoices.GetComponent<RectTransform>(), new Vector2(0.6f, 0.25f), new Vector2(0.95f, 0.85f));
            diaChoices.SetActive(false);

            DialogueUI diaUI = diaPanelObj.AddComponent<DialogueUI>();
            diaUI.GetType().GetField("dialoguePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, diaPanelObj);
            diaUI.GetType().GetField("npcPortrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, diaNpcPortrait.GetComponent<Image>());
            diaUI.GetType().GetField("playerPortrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, diaPlayerPortrait.GetComponent<Image>());
            diaUI.GetType().GetField("npcNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, diaName.GetComponent<TextMeshProUGUI>());
            diaUI.GetType().GetField("dialogueText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, diaText.GetComponent<TextMeshProUGUI>());
            diaUI.GetType().GetField("choiceContainer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, diaChoices.GetComponent<RectTransform>());
            diaUI.GetType().GetField("choiceButtonPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(diaUI, btnPrefab);

            GameObject dialoguePrefab = SavePrefab(diaPanelObj, "Dialogue");

            // 10. Build Haggling / Trade Prefab
            GameObject tradePanelObj = InstantiatePrefab(panelPrefab);
            tradePanelObj.name = "TradeUI";
            tradePanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 500);
            Transform tradeBg = tradePanelObj.transform.Find("ContentBg");

            GameObject tCustName = CreateUIElement("CustomerName", typeof(TextMeshProUGUI), tradeBg);
            tCustName.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tCustName.GetComponent<TextMeshProUGUI>().fontSize = 24;
            tCustName.GetComponent<TextMeshProUGUI>().text = "Khách Hàng";
            tCustName.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(tCustName.GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));

            GameObject tItemIcon = CreateUIElement("ItemIcon", typeof(Image), tradeBg);
            Stretch(tItemIcon.GetComponent<RectTransform>(), new Vector2(0.08f, 0.58f), new Vector2(0.2f, 0.78f));

            GameObject tItemName = CreateUIElement("ItemName", typeof(TextMeshProUGUI), tradeBg);
            tItemName.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tItemName.GetComponent<TextMeshProUGUI>().fontSize = 20;
            tItemName.GetComponent<TextMeshProUGUI>().text = "Xoài Cát";
            Stretch(tItemName.GetComponent<RectTransform>(), new Vector2(0.24f, 0.68f), new Vector2(0.7f, 0.78f));

            GameObject tItemQty = CreateUIElement("ItemQuantity", typeof(TextMeshProUGUI), tradeBg);
            tItemQty.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tItemQty.GetComponent<TextMeshProUGUI>().fontSize = 18;
            tItemQty.GetComponent<TextMeshProUGUI>().text = "x10 quả";
            Stretch(tItemQty.GetComponent<RectTransform>(), new Vector2(0.24f, 0.58f), new Vector2(0.7f, 0.68f));

            GameObject tBasePrice = CreateUIElement("BasePrice", typeof(TextMeshProUGUI), tradeBg);
            tBasePrice.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tBasePrice.GetComponent<TextMeshProUGUI>().fontSize = 18;
            tBasePrice.GetComponent<TextMeshProUGUI>().text = "Giá gốc: 20,000 VNĐ";
            tBasePrice.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            Stretch(tBasePrice.GetComponent<RectTransform>(), new Vector2(0.5f, 0.58f), new Vector2(0.92f, 0.78f));

            // Negotiation Offer display
            GameObject tOfferText = CreateUIElement("CurrentOffer", typeof(TextMeshProUGUI), tradeBg);
            tOfferText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tOfferText.GetComponent<TextMeshProUGUI>().fontSize = 28;
            tOfferText.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            tOfferText.GetComponent<TextMeshProUGUI>().text = "25,000 VNĐ";
            tOfferText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(tOfferText.GetComponent<RectTransform>(), new Vector2(0.2f, 0.4f), new Vector2(0.8f, 0.52f));

            GameObject tSumText = CreateUIElement("SummaryText", typeof(TextMeshProUGUI), tradeBg);
            tSumText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tSumText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            tSumText.GetComponent<TextMeshProUGUI>().text = "Tổng: 250,000 VNĐ";
            tSumText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            tSumText.GetComponent<TextMeshProUGUI>().color = new Color(0.92f, 0.82f, 0.55f, 1f);
            Stretch(tSumText.GetComponent<RectTransform>(), new Vector2(0.2f, 0.32f), new Vector2(0.8f, 0.4f));

            // Slider for patience
            GameObject tSliderObj = CreateUIElement("PatienceSlider", typeof(Slider));
            Slider tSlider = tSliderObj.GetComponent<Slider>();
            Stretch(tSliderObj.GetComponent<RectTransform>(), new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.28f));
            
            GameObject tBackground = CreateUIElement("Background", typeof(Image), tSliderObj.transform);
            tBackground.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
            Stretch(tBackground.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            GameObject tFillArea = CreateUIElement("Fill Area", typeof(RectTransform), tSliderObj.transform);
            Stretch(tFillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            
            GameObject tFill = CreateUIElement("Fill", typeof(Image), tFillArea.transform);
            tFill.GetComponent<Image>().color = new Color(0.2f, 0.8f, 0.2f);
            tSlider.fillRect = tFill.GetComponent<RectTransform>();

            // Buttons
            GameObject btnInc = InstantiatePrefab(btnPrefab);
            btnInc.name = "IncreaseButton";
            btnInc.transform.SetParent(tradeBg, false);
            btnInc.GetComponentInChildren<TextMeshProUGUI>().text = "Tăng giá +";
            Stretch(btnInc.GetComponent<RectTransform>(), new Vector2(0.52f, 0.12f), new Vector2(0.72f, 0.19f));

            GameObject btnDec = InstantiatePrefab(btnPrefab);
            btnDec.name = "DecreaseButton";
            btnDec.transform.SetParent(tradeBg, false);
            btnDec.GetComponentInChildren<TextMeshProUGUI>().text = "Giảm giá -";
            Stretch(btnDec.GetComponent<RectTransform>(), new Vector2(0.28f, 0.12f), new Vector2(0.48f, 0.19f));

            GameObject btnAcc = InstantiatePrefab(btnPrefab);
            btnAcc.name = "AcceptButton";
            btnAcc.transform.SetParent(tradeBg, false);
            btnAcc.GetComponentInChildren<TextMeshProUGUI>().text = "Chốt kèo (Bán)";
            Stretch(btnAcc.GetComponent<RectTransform>(), new Vector2(0.52f, 0.03f), new Vector2(0.72f, 0.10f));

            GameObject btnWalk = InstantiatePrefab(btnPrefab);
            btnWalk.name = "WalkButton";
            btnWalk.transform.SetParent(tradeBg, false);
            btnWalk.GetComponentInChildren<TextMeshProUGUI>().text = "Để họ đi";
            Stretch(btnWalk.GetComponent<RectTransform>(), new Vector2(0.28f, 0.03f), new Vector2(0.48f, 0.10f));

            TradeUI tUI = tradePanelObj.AddComponent<TradeUI>();
            tUI.GetType().GetField("tradePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tradePanelObj);
            tUI.GetType().GetField("customerNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tCustName.GetComponent<TextMeshProUGUI>());
            tUI.GetType().GetField("itemIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tItemIcon.GetComponent<Image>());
            tUI.GetType().GetField("itemNameText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tItemName.GetComponent<TextMeshProUGUI>());
            tUI.GetType().GetField("itemQuantityText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tItemQty.GetComponent<TextMeshProUGUI>());
            tUI.GetType().GetField("basePriceText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tBasePrice.GetComponent<TextMeshProUGUI>());
            tUI.GetType().GetField("currentOfferText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tOfferText.GetComponent<TextMeshProUGUI>());
            tUI.GetType().GetField("summaryText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tSumText.GetComponent<TextMeshProUGUI>());
            tUI.GetType().GetField("meterSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tSlider);
            tUI.GetType().GetField("meterFillImage", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, tFill.GetComponent<Image>());
            tUI.GetType().GetField("increaseButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, btnInc.GetComponent<Button>());
            tUI.GetType().GetField("decreaseButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, btnDec.GetComponent<Button>());
            tUI.GetType().GetField("acceptButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, btnAcc.GetComponent<Button>());
            tUI.GetType().GetField("walkAwayButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(tUI, btnWalk.GetComponent<Button>());

            GameObject tradePrefab = SavePrefab(tradePanelObj, "TradeUI");

            // 11. Build Day Summary Prefab
            GameObject sumPanelObj = InstantiatePrefab(panelPrefab);
            sumPanelObj.name = "DaySummary";
            sumPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 520);
            Transform sumBg = sumPanelObj.transform.Find("ContentBg");

            GameObject sumTitle = CreateUIElement("Title", typeof(TextMeshProUGUI), sumBg);
            sumTitle.GetComponent<TextMeshProUGUI>().font = fontAsset;
            sumTitle.GetComponent<TextMeshProUGUI>().fontSize = 32;
            sumTitle.GetComponent<TextMeshProUGUI>().text = "TỔNG KẾT NGÀY";
            sumTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(sumTitle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f));

            GameObject sumSub = CreateUIElement("Subtitle", typeof(TextMeshProUGUI), sumBg);
            sumSub.GetComponent<TextMeshProUGUI>().font = fontAsset;
            sumSub.GetComponent<TextMeshProUGUI>().fontSize = 20;
            sumSub.GetComponent<TextMeshProUGUI>().text = "Kết thúc Ngày 1";
            sumSub.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(sumSub.GetComponent<RectTransform>(), new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.88f));

            // Metrics
            GameObject metPanel = CreateUIElement("MetricsPanel", typeof(VerticalLayoutGroup), sumBg);
            metPanel.GetComponent<VerticalLayoutGroup>().spacing = 15;
            metPanel.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Stretch(metPanel.GetComponent<RectTransform>(), new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.78f));

            GameObject incVal = CreateMetricRow(metPanel.transform, fontAsset, "Doanh thu bán sỉ:", "+0 VNĐ", new Color(0.2f, 0.8f, 0.2f));
            GameObject repVal = CreateMetricRow(metPanel.transform, fontAsset, "Phí sửa chữa ghe:", "0 VNĐ", new Color(0.9f, 0.2f, 0.2f));
            GameObject feeVal = CreateMetricRow(metPanel.transform, fontAsset, "Phí ngất xỉu đêm:", "0 VNĐ", new Color(0.9f, 0.2f, 0.2f));
            GameObject profVal = CreateMetricRow(metPanel.transform, fontAsset, "Lợi nhuận ròng:", "0 VNĐ", new Color(0.2f, 0.8f, 0.2f));

            GameObject btnCont = InstantiatePrefab(btnPrefab);
            btnCont.name = "ContinueButton";
            btnCont.transform.SetParent(sumBg, false);
            btnCont.GetComponentInChildren<TextMeshProUGUI>().text = "Tiếp tục ngày mới";
            Stretch(btnCont.GetComponent<RectTransform>(), new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.15f));

            DaySummaryUI dUI = sumPanelObj.AddComponent<DaySummaryUI>();
            dUI.GetType().GetField("summaryPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, sumPanelObj);
            dUI.GetType().GetField("titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, sumTitle.GetComponent<TextMeshProUGUI>());
            dUI.GetType().GetField("daySubtitleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, sumSub.GetComponent<TextMeshProUGUI>());
            dUI.GetType().GetField("incomeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, incVal.GetComponent<TextMeshProUGUI>());
            dUI.GetType().GetField("repairText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, repVal.GetComponent<TextMeshProUGUI>());
            dUI.GetType().GetField("lateFeeText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, feeVal.GetComponent<TextMeshProUGUI>());
            dUI.GetType().GetField("totalProfitText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, profVal.GetComponent<TextMeshProUGUI>());
            dUI.GetType().GetField("continueButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(dUI, btnCont.GetComponent<Button>());

            GameObject summaryPrefab = SavePrefab(sumPanelObj, "DaySummary");

            // 12. Build Notification Prefab
            GameObject notPanelObj = CreateUIElement("NotificationUI", typeof(RectTransform));
            notPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            
            GameObject notPanel = InstantiatePrefab(panelPrefab);
            notPanel.name = "NotificationPanel";
            notPanel.transform.SetParent(notPanelObj.transform, false);
            notPanel.AddComponent<CanvasGroup>();
            notPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 80);
            Stretch(notPanel.GetComponent<RectTransform>(), new Vector2(0.4f, 0.88f), new Vector2(0.6f, 0.95f));
            
            GameObject notText = CreateUIElement("Text", typeof(TextMeshProUGUI), notPanel.transform.Find("ContentBg"));
            notText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            notText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            notText.GetComponent<TextMeshProUGUI>().text = "Notification Message";
            notText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(notText.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            NotificationUI nUI = notPanelObj.AddComponent<NotificationUI>();
            nUI.GetType().GetField("notificationPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(nUI, notPanel);
            nUI.GetType().GetField("notificationText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(nUI, notText.GetComponent<TextMeshProUGUI>());

            GameObject notificationPrefab = SavePrefab(notPanelObj, "NotificationUI");

            // 13. Build Pause/Settings Menu Prefab
            GameObject setPanelObj = CreateUIElement("SettingsUI", typeof(RectTransform));
            setPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);

            // Pause Menu Panel
            GameObject pMenu = InstantiatePrefab(panelPrefab);
            pMenu.name = "PausePanel";
            pMenu.transform.SetParent(setPanelObj.transform, false);
            pMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(450, 500);
            Transform pBg = pMenu.transform.Find("ContentBg");
            
            GameObject pTitle = CreateUIElement("Title", typeof(TextMeshProUGUI), pBg);
            pTitle.GetComponent<TextMeshProUGUI>().font = fontAsset;
            pTitle.GetComponent<TextMeshProUGUI>().fontSize = 28;
            pTitle.GetComponent<TextMeshProUGUI>().text = "TẠM DỪNG";
            pTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(pTitle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.85f), new Vector2(0.95f, 0.95f));

            GameObject pLayout = CreateUIElement("Buttons", typeof(VerticalLayoutGroup), pBg);
            pLayout.GetComponent<VerticalLayoutGroup>().spacing = 15;
            pLayout.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Stretch(pLayout.GetComponent<RectTransform>(), new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.8f));

            GameObject pResume = InstantiatePrefab(btnPrefab); pResume.name = "Resume"; pResume.transform.SetParent(pLayout.transform, false); pResume.GetComponentInChildren<TextMeshProUGUI>().text = "Tiếp tục chơi";
            GameObject pSettings = InstantiatePrefab(btnPrefab); pSettings.name = "Settings"; pSettings.transform.SetParent(pLayout.transform, false); pSettings.GetComponentInChildren<TextMeshProUGUI>().text = "Cài đặt";
            GameObject pQuit = InstantiatePrefab(btnPrefab); pQuit.name = "Quit"; pQuit.transform.SetParent(pLayout.transform, false); pQuit.GetComponentInChildren<TextMeshProUGUI>().text = "Thoát game";

            // Settings Details Panel
            GameObject sMenu = InstantiatePrefab(panelPrefab);
            sMenu.name = "SettingsPanel";
            sMenu.transform.SetParent(setPanelObj.transform, false);
            sMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(550, 550);
            Transform sBg = sMenu.transform.Find("ContentBg");

            GameObject sTitle = CreateUIElement("Title", typeof(TextMeshProUGUI), sBg);
            sTitle.GetComponent<TextMeshProUGUI>().font = fontAsset;
            sTitle.GetComponent<TextMeshProUGUI>().fontSize = 28;
            sTitle.GetComponent<TextMeshProUGUI>().text = "CÀI ĐẶT";
            sTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(sTitle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.96f));

            GameObject sLayout = CreateUIElement("Controls", typeof(VerticalLayoutGroup), sBg);
            sLayout.GetComponent<VerticalLayoutGroup>().spacing = 20;
            sLayout.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleCenter;
            Stretch(sLayout.GetComponent<RectTransform>(), new Vector2(0.08f, 0.15f), new Vector2(0.92f, 0.82f));

            // Volume Row
            GameObject volLabel = CreateUIElement("VolumeLabel", typeof(TextMeshProUGUI), sLayout.transform);
            volLabel.GetComponent<TextMeshProUGUI>().font = fontAsset;
            volLabel.GetComponent<TextMeshProUGUI>().fontSize = 18;
            volLabel.GetComponent<TextMeshProUGUI>().text = "Âm lượng: 100%";
            volLabel.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            GetOrAddLayoutElement(volLabel).preferredHeight = 30;

            GameObject volSliderObj = CreateUIElement("VolumeSlider", typeof(Slider), sLayout.transform);
            GetOrAddLayoutElement(volSliderObj).preferredHeight = 30;
            GetOrAddLayoutElement(volSliderObj).preferredWidth = 350;
            Slider volSlider = volSliderObj.GetComponent<Slider>();
            
            GameObject volBg = CreateUIElement("Background", typeof(Image), volSliderObj.transform); volBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f); Stretch(volBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            GameObject volFillArea = CreateUIElement("Fill Area", typeof(RectTransform), volSliderObj.transform); Stretch(volFillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            GameObject volFill = CreateUIElement("Fill", typeof(Image), volFillArea.transform); volFill.GetComponent<Image>().color = new Color(0.88f, 0.44f, 0.12f); volSlider.fillRect = volFill.GetComponent<RectTransform>();

            // Fullscreen row
            GameObject fsToggleObj = CreateUIElement("FullscreenToggle", typeof(Toggle), sLayout.transform);
            GetOrAddLayoutElement(fsToggleObj).preferredHeight = 40;
            Toggle fsToggle = fsToggleObj.GetComponent<Toggle>();
            
            GameObject fsText = CreateUIElement("Label", typeof(TextMeshProUGUI), fsToggleObj.transform);
            fsText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            fsText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            fsText.GetComponent<TextMeshProUGUI>().text = "Chế độ Toàn màn hình";
            Stretch(fsText.GetComponent<RectTransform>(), new Vector2(0.15f, 0f), new Vector2(1f, 1f));

            // Language Row
            GameObject langBtn = InstantiatePrefab(btnPrefab); langBtn.name = "LanguageButton"; langBtn.transform.SetParent(sLayout.transform, false); langBtn.GetComponentInChildren<TextMeshProUGUI>().text = "Ngôn Ngữ: TIẾNG VIỆT";

            GameObject sClose = InstantiatePrefab(btnPrefab); sClose.name = "CloseSettingsButton"; sClose.transform.SetParent(sBg, false); sClose.GetComponentInChildren<TextMeshProUGUI>().text = "Đóng";
            Stretch(sClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.11f));

            SettingsUI setUI = setPanelObj.AddComponent<SettingsUI>();
            setUI.GetType().GetField("pausePanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, pMenu);
            setUI.GetType().GetField("settingsPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, sMenu);
            setUI.GetType().GetField("volumeSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, volSlider);
            setUI.GetType().GetField("volumeLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, volLabel.GetComponent<TMP_Text>());
            setUI.GetType().GetField("languageButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, langBtn.GetComponent<Button>());
            setUI.GetType().GetField("languageLabel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, langBtn.GetComponentInChildren<TMP_Text>());
            setUI.GetType().GetField("fullscreenToggle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, fsToggle);
            setUI.GetType().GetField("resumeButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, pResume.GetComponent<Button>());
            setUI.GetType().GetField("openSettingsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, pSettings.GetComponent<Button>());
            setUI.GetType().GetField("closeSettingsButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, sClose.GetComponent<Button>());
            setUI.GetType().GetField("quitButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(setUI, pQuit.GetComponent<Button>());

            GameObject settingsPrefab = SavePrefab(setPanelObj, "SettingsUI");

            // 14. Build Confirmation Popup Prefab
            GameObject confPanelObj = InstantiatePrefab(panelPrefab);
            confPanelObj.name = "ConfirmationUI";
            confPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 320);
            Transform confBg = confPanelObj.transform.Find("ContentBg");

            GameObject confTitle = CreateUIElement("Title", typeof(TextMeshProUGUI), confBg);
            confTitle.GetComponent<TextMeshProUGUI>().font = fontAsset;
            confTitle.GetComponent<TextMeshProUGUI>().fontSize = 26;
            confTitle.GetComponent<TextMeshProUGUI>().text = "XÁC NHẬN";
            confTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(confTitle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));

            GameObject confDesc = CreateUIElement("Description", typeof(TextMeshProUGUI), confBg);
            confDesc.GetComponent<TextMeshProUGUI>().font = fontAsset;
            confDesc.GetComponent<TextMeshProUGUI>().fontSize = 18;
            confDesc.GetComponent<TextMeshProUGUI>().text = "Bạn có chắc chắn muốn thực hiện hành động này không?";
            confDesc.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(confDesc.GetComponent<RectTransform>(), new Vector2(0.05f, 0.3f), new Vector2(0.95f, 0.72f));

            GameObject btnYes = InstantiatePrefab(btnPrefab); btnYes.name = "YesButton"; btnYes.transform.SetParent(confBg, false); btnYes.GetComponentInChildren<TextMeshProUGUI>().text = "Có";
            Stretch(btnYes.GetComponent<RectTransform>(), new Vector2(0.55f, 0.08f), new Vector2(0.85f, 0.22f));

            GameObject btnNo = InstantiatePrefab(btnPrefab); btnNo.name = "NoButton"; btnNo.transform.SetParent(confBg, false); btnNo.GetComponentInChildren<TextMeshProUGUI>().text = "Không";
            Stretch(btnNo.GetComponent<RectTransform>(), new Vector2(0.15f, 0.08f), new Vector2(0.45f, 0.22f));

            ConfirmationPopup cPopup = confPanelObj.AddComponent<ConfirmationPopup>();
            cPopup.GetType().GetField("popupPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cPopup, confPanelObj);
            cPopup.GetType().GetField("titleText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cPopup, confTitle.GetComponent<TextMeshProUGUI>());
            cPopup.GetType().GetField("descriptionText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cPopup, confDesc.GetComponent<TextMeshProUGUI>());
            cPopup.GetType().GetField("yesButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cPopup, btnYes.GetComponent<Button>());
            cPopup.GetType().GetField("noButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(cPopup, btnNo.GetComponent<Button>());

            GameObject confirmationPrefab = SavePrefab(confPanelObj, "ConfirmationUI");

            // 15. Build Loading Screen Prefab
            GameObject loadPanelObj = CreateUIElement("LoadingScreenUI", typeof(RectTransform));
            loadPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            loadPanelObj.AddComponent<CanvasGroup>();

            // Fullscreen Paper Background
            GameObject paperBgObj = CreateUIElement("Background", typeof(Image), loadPanelObj.transform);
            paperBgObj.GetComponent<Image>().sprite = panelBeige;
            paperBgObj.GetComponent<Image>().type = Image.Type.Sliced;
            Stretch(paperBgObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);

            // Progress Slider
            GameObject loadSliderObj = CreateUIElement("LoadingSlider", typeof(Slider), loadPanelObj.transform);
            Slider loadSlider = loadSliderObj.GetComponent<Slider>();
            Stretch(loadSliderObj.GetComponent<RectTransform>(), new Vector2(0.25f, 0.2f), new Vector2(0.75f, 0.25f));
            
            GameObject loadBg = CreateUIElement("Background", typeof(Image), loadSliderObj.transform); loadBg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.8f); Stretch(loadBg.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            GameObject loadFillArea = CreateUIElement("Fill Area", typeof(RectTransform), loadSliderObj.transform); Stretch(volFillArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            GameObject loadFill = CreateUIElement("Fill", typeof(Image), loadFillArea.transform); loadFill.GetComponent<Image>().color = new Color(0.88f, 0.44f, 0.12f); loadSlider.fillRect = loadFill.GetComponent<RectTransform>();

            GameObject percentText = CreateUIElement("PercentText", typeof(TextMeshProUGUI), loadPanelObj.transform);
            percentText.GetComponent<TextMeshProUGUI>().font = fontAsset;
            percentText.GetComponent<TextMeshProUGUI>().fontSize = 18;
            percentText.GetComponent<TextMeshProUGUI>().text = "0%";
            percentText.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(percentText.GetComponent<RectTransform>(), new Vector2(0.45f, 0.14f), new Vector2(0.55f, 0.19f));

            GameObject tipsTextObj = CreateUIElement("TipText", typeof(TextMeshProUGUI), loadPanelObj.transform);
            tipsTextObj.GetComponent<TextMeshProUGUI>().font = fontAsset;
            tipsTextObj.GetComponent<TextMeshProUGUI>().fontSize = 20;
            tipsTextObj.GetComponent<TextMeshProUGUI>().text = "Mẹo: Treo khóm lên Cây Bẹo để gọi thêm ghe du khách ghé ghe của bạn.";
            tipsTextObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(tipsTextObj.GetComponent<RectTransform>(), new Vector2(0.15f, 0.28f), new Vector2(0.85f, 0.38f));

            LoadingUI loadUI = loadPanelObj.AddComponent<LoadingUI>();
            loadUI.GetType().GetField("loadingPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(loadUI, loadPanelObj);
            loadUI.GetType().GetField("progressBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(loadUI, loadSlider);
            loadUI.GetType().GetField("progressText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(loadUI, percentText.GetComponent<TextMeshProUGUI>());
            loadUI.GetType().GetField("tipText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(loadUI, tipsTextObj.GetComponent<TextMeshProUGUI>());

            GameObject loadingPrefab = SavePrefab(loadPanelObj, "LoadingUI");

            // 16. Build Transition Prefab
            GameObject transPanelObj = CreateUIElement("TransitionUI", typeof(RectTransform));
            transPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(1920, 1080);
            
            GameObject faderObj = CreateUIElement("Fader", typeof(Image), transPanelObj.transform);
            faderObj.GetComponent<Image>().color = Color.black;
            Stretch(faderObj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one);
            
            TransitionUI transUI = transPanelObj.AddComponent<TransitionUI>();
            transUI.GetType().GetField("transitionPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(transUI, transPanelObj);
            transUI.GetType().GetField("canvasGroup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(transUI, transPanelObj.AddComponent<CanvasGroup>());

            GameObject transitionPrefab = SavePrefab(transPanelObj, "TransitionUI");

            // 17. Build Game Over Prefab
            GameObject goPanelObj = InstantiatePrefab(panelPrefab);
            goPanelObj.name = "GameOverUI";
            goPanelObj.GetComponent<RectTransform>().sizeDelta = new Vector2(700, 420);
            Transform goBg = goPanelObj.transform.Find("ContentBg");

            GameObject goTitle = CreateUIElement("Title", typeof(TextMeshProUGUI), goBg);
            goTitle.GetComponent<TextMeshProUGUI>().font = fontAsset;
            goTitle.GetComponent<TextMeshProUGUI>().fontSize = 32;
            goTitle.GetComponent<TextMeshProUGUI>().text = "BÁN SẠCH GHE MẤT BẾN";
            goTitle.GetComponent<TextMeshProUGUI>().color = new Color(0.9f, 0.2f, 0.2f);
            goTitle.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(goTitle.GetComponent<RectTransform>(), new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.95f));

            GameObject goDesc = CreateUIElement("Description", typeof(TextMeshProUGUI), goBg);
            goDesc.GetComponent<TextMeshProUGUI>().font = fontAsset;
            goDesc.GetComponent<TextMeshProUGUI>().fontSize = 18;
            goDesc.GetComponent<TextMeshProUGUI>().text = "Bạn đã hết khả năng chi trả chi phí sửa chữa và duy trì chiếc ghe buôn. Chuyến giao thương kết thúc tại đây...";
            goDesc.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
            Stretch(goDesc.GetComponent<RectTransform>(), new Vector2(0.08f, 0.35f), new Vector2(0.92f, 0.72f));

            GameObject goRetry = InstantiatePrefab(btnPrefab); goRetry.name = "RetryButton"; goRetry.transform.SetParent(goBg, false); goRetry.GetComponentInChildren<TextMeshProUGUI>().text = "Chơi lại";
            Stretch(goRetry.GetComponent<RectTransform>(), new Vector2(0.55f, 0.12f), new Vector2(0.85f, 0.25f));

            GameObject goMenu = InstantiatePrefab(btnPrefab); goMenu.name = "MenuButton"; goMenu.transform.SetParent(goBg, false); goMenu.GetComponentInChildren<TextMeshProUGUI>().text = "Về trang chủ";
            Stretch(goMenu.GetComponent<RectTransform>(), new Vector2(0.15f, 0.12f), new Vector2(0.45f, 0.25f));

            GameOverUI goUIComp = goPanelObj.AddComponent<GameOverUI>();
            goUIComp.GetType().GetField("gameOverPanel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goUIComp, goBg.gameObject);
            goUIComp.GetType().GetField("messageText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goUIComp, goDesc.GetComponent<TextMeshProUGUI>());
            goUIComp.GetType().GetField("retryButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goUIComp, goRetry.GetComponent<Button>());
            goUIComp.GetType().GetField("mainMenuButton", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(goUIComp, goMenu.GetComponent<Button>());

            GameObject gameOverPrefab = SavePrefab(goPanelObj, "GameOverUI");

            // 18. Build Master Canvas Prefab containing all windows
            GameObject masterCanvasObj = new GameObject("MasterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas c = masterCanvasObj.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler cs = masterCanvasObj.GetComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920, 1080);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0.5f;

            GameObject h = InstantiatePrefab(hudPrefab); h.name = "HUD"; h.transform.SetParent(masterCanvasObj.transform, false);
            GameObject invInst = InstantiatePrefab(inventoryPrefab); invInst.name = "Inventory"; invInst.transform.SetParent(masterCanvasObj.transform, false); invInst.SetActive(false);
            GameObject d = InstantiatePrefab(dialoguePrefab); d.name = "Dialogue"; d.transform.SetParent(masterCanvasObj.transform, false); d.SetActive(false);
            GameObject tr = InstantiatePrefab(tradePrefab); tr.name = "TradeUI"; tr.transform.SetParent(masterCanvasObj.transform, false); tr.SetActive(false);
            GameObject s = InstantiatePrefab(summaryPrefab); s.name = "DaySummary"; s.transform.SetParent(masterCanvasObj.transform, false); s.SetActive(false);
            GameObject n = InstantiatePrefab(notificationPrefab); n.name = "NotificationUI"; n.transform.SetParent(masterCanvasObj.transform, false); n.SetActive(false);
            GameObject st = InstantiatePrefab(settingsPrefab); st.name = "SettingsUI"; st.transform.SetParent(masterCanvasObj.transform, false); st.SetActive(false);
            GameObject cp = InstantiatePrefab(confirmationPrefab); cp.name = "ConfirmationUI"; cp.transform.SetParent(masterCanvasObj.transform, false); cp.SetActive(false);
            GameObject ld = InstantiatePrefab(loadingPrefab); ld.name = "LoadingUI"; ld.transform.SetParent(masterCanvasObj.transform, false); ld.SetActive(false);
            GameObject transInst = InstantiatePrefab(transitionPrefab); transInst.name = "TransitionUI"; transInst.transform.SetParent(masterCanvasObj.transform, false); transInst.SetActive(false);
            GameObject go = InstantiatePrefab(gameOverPrefab); go.name = "GameOverUI"; go.transform.SetParent(masterCanvasObj.transform, false); go.SetActive(false);
            GameObject ttInst = InstantiatePrefab(tooltipPrefab); ttInst.name = "Tooltip"; ttInst.transform.SetParent(masterCanvasObj.transform, false); ttInst.SetActive(false);

            SavePrefab(masterCanvasObj, "MasterCanvas");

            // Clean up scene garbage
            UnityEngine.Object.DestroyImmediate(masterCanvasObj);
            UnityEngine.Object.DestroyImmediate(btnObj);
            UnityEngine.Object.DestroyImmediate(panelObj);
            UnityEngine.Object.DestroyImmediate(slotObj);
            UnityEngine.Object.DestroyImmediate(bSlotObj);
            UnityEngine.Object.DestroyImmediate(ttObj);
            UnityEngine.Object.DestroyImmediate(hudObj);
            UnityEngine.Object.DestroyImmediate(invPanelObj);
            UnityEngine.Object.DestroyImmediate(diaPanelObj);
            UnityEngine.Object.DestroyImmediate(tradePanelObj);
            UnityEngine.Object.DestroyImmediate(sumPanelObj);
            UnityEngine.Object.DestroyImmediate(notPanelObj);
            UnityEngine.Object.DestroyImmediate(setPanelObj);
            UnityEngine.Object.DestroyImmediate(confPanelObj);
            UnityEngine.Object.DestroyImmediate(loadPanelObj);
            UnityEngine.Object.DestroyImmediate(transPanelObj);
            UnityEngine.Object.DestroyImmediate(goPanelObj);

            Debug.Log("Successfully generated all 13 Stylized Prefabs and MasterCanvas inside Assets/_Project/Prefabs/UI!");
        }

        private static void ConfigureSprite(string path, Vector4 borders)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spriteBorder = borders;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        private static GameObject CreateUIElement(string name, System.Type mainType, Transform parent = null)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), mainType);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            return obj;
        }

        private static GameObject CreateUIElement(string name, System.Type t1, System.Type t2, Transform parent = null)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), t1, t2);
            if (parent != null)
            {
                obj.transform.SetParent(parent, false);
            }
            return obj;
        }

        private static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static GameObject InstantiatePrefab(GameObject prefab)
        {
            GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            return obj;
        }

        private static GameObject SavePrefab(GameObject obj, string name)
        {
            string path = Path.Combine(PrefabFolder, $"{name}.prefab");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(obj, path);
            Debug.Log($"Saved prefab: {path}");
            return prefab;
        }

        private static LayoutElement GetOrAddLayoutElement(GameObject go)
        {
            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            return le;
        }

        private static GameObject CreateMetricRow(Transform parent, TMP_FontAsset font, string labelStr, string valueStr, Color valColor)
        {
            GameObject row = new GameObject("MetricRow", typeof(RectTransform), typeof(LayoutElement));
            row.transform.SetParent(parent, false);
            row.GetComponent<LayoutElement>().preferredHeight = 35;
            row.GetComponent<LayoutElement>().preferredWidth = 600;

            GameObject label = CreateUIElement("Label", typeof(TextMeshProUGUI), row.transform);
            label.GetComponent<TextMeshProUGUI>().font = font;
            label.GetComponent<TextMeshProUGUI>().fontSize = 18;
            label.GetComponent<TextMeshProUGUI>().text = labelStr;
            Stretch(label.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.5f, 1f));

            GameObject val = CreateUIElement("Value", typeof(TextMeshProUGUI), row.transform);
            val.GetComponent<TextMeshProUGUI>().font = font;
            val.GetComponent<TextMeshProUGUI>().fontSize = 18;
            val.GetComponent<TextMeshProUGUI>().fontStyle = FontStyles.Bold;
            val.GetComponent<TextMeshProUGUI>().text = valueStr;
            val.GetComponent<TextMeshProUGUI>().color = valColor;
            val.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
            Stretch(val.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(1f, 1f));

            return val;
        }
    }
}
#endif
