#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ChoNoiMienTay.Presentation;
using ChoNoiMienTay.Infrastructure;

namespace ChoNoiMienTay.Editor
{
    public class DraftUICreator
    {
        [MenuItem("ChoNoi/UI/Create Draft Inventory UI")]
        public static void CreateDraftUI()
        {
            // 1. Tạo Canvas
            GameObject canvasGO = new GameObject("InventoryCanvas");
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGO.AddComponent<GraphicRaycaster>();

            // 2. Tạo EventSystem
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            // 3. Tạo Managers
            GameObject managerGO = new GameObject("GameManagers");
            InventoryManager inventoryManager = managerGO.AddComponent<InventoryManager>();
            PlayerStats playerStats = managerGO.AddComponent<PlayerStats>();
            EconomyManager economyManager = managerGO.AddComponent<EconomyManager>();
            HagglingSystem hagglingSystem = managerGO.AddComponent<HagglingSystem>();

            // 4. Tạo Panel nền
            GameObject panelGO = new GameObject("BackgroundPanel");
            panelGO.transform.SetParent(canvasGO.transform, false);
            Image panelImage = panelGO.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.75f);
            RectTransform panelRect = panelGO.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.95f);
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            // 5. Tạo Texts
            Text CreateText(string name, int size, TextAnchor align, Vector2 min, Vector2 max)
            {
                GameObject txtGO = new GameObject(name);
                txtGO.transform.SetParent(panelGO.transform, false);
                Text txt = txtGO.AddComponent<Text>();
                txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                txt.color = Color.yellow;
                txt.fontSize = size;
                txt.alignment = align;
                RectTransform r = txtGO.GetComponent<RectTransform>();
                r.anchorMin = min; r.anchorMax = max;
                r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
                return txt;
            }

            Text capacityText = CreateText("CapacityText", 20, TextAnchor.UpperLeft, new Vector2(0.02f, 0.9f), new Vector2(0.4f, 0.98f));
            Text statsText = CreateText("StatsText", 18, TextAnchor.UpperRight, new Vector2(0.45f, 0.9f), new Vector2(0.98f, 0.98f));
            Text invText = CreateText("InventoryText", 18, TextAnchor.UpperLeft, new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.88f));

            // 6. Helper Tạo Button
            Button CreateBtn(string name, string text, Vector2 min, Vector2 max)
            {
                GameObject btnGO = DefaultControls.CreateButton(new DefaultControls.Resources());
                btnGO.name = name;
                btnGO.transform.SetParent(panelGO.transform, false);
                RectTransform rect = btnGO.GetComponent<RectTransform>();
                rect.anchorMin = min; rect.anchorMax = max;
                rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
                Text btnText = btnGO.GetComponentInChildren<Text>();
                btnText.text = text;
                btnText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return btnGO.GetComponent<Button>();
            }

            // Hàng Dịch Vụ / Trả Giá (Top của khu vực Button)
            Button btnBunRieu = CreateBtn("BtnBunRieu", "Mua Bún Riêu (Hồi TL)", new Vector2(0.05f, 0.40f), new Vector2(0.30f, 0.48f));
            Button btnSweetTalk = CreateBtn("BtnSweetTalk", "Nói Ngọt (-10 TL, +TC)", new Vector2(0.35f, 0.40f), new Vector2(0.65f, 0.48f));
            Button btnGift = CreateBtn("BtnGift", "Tặng Khóm (+TC)", new Vector2(0.70f, 0.40f), new Vector2(0.95f, 0.48f));

            // Hàng Mua / Bán
            Button buyQuanAo = CreateBtn("BuyQuanAo", "Mua Quần Áo", new Vector2(0.05f, 0.28f), new Vector2(0.45f, 0.36f));
            Button sellQuanAo = CreateBtn("SellQuanAo", "Chốt Sỉ Quần Áo", new Vector2(0.55f, 0.28f), new Vector2(0.95f, 0.36f));

            Button buyKhom = CreateBtn("BuyKhom", "Mua Khóm", new Vector2(0.05f, 0.16f), new Vector2(0.45f, 0.24f));
            Button sellKhom = CreateBtn("SellKhom", "Chốt Sỉ Khóm", new Vector2(0.55f, 0.16f), new Vector2(0.95f, 0.24f));

            Button buyBiDao = CreateBtn("BuyBiDao", "Mua Bí Đao", new Vector2(0.05f, 0.04f), new Vector2(0.45f, 0.12f));
            Button sellBiDao = CreateBtn("SellBiDao", "Chốt Sỉ Bí Đao", new Vector2(0.55f, 0.04f), new Vector2(0.95f, 0.12f));

            // 7. Gắn script InventoryUI và liên kết reference
            InventoryUI ui = canvasGO.AddComponent<InventoryUI>();
            
            // Link Text
            ui.capacityDisplayText = capacityText;
            ui.inventoryDisplayText = invText;
            ui.statsDisplayText = statsText;

            Selection.activeGameObject = canvasGO;
            Debug.Log("[DraftUICreator] Đã tạo Canvas UI Tích Hợp Kinh Tế thành công!");
        }
    }
}
#endif
