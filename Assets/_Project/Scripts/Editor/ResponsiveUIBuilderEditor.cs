/**
 * ResponsiveUIBuilderEditor: Static Editor script tạo nhanh cấu trúc UI mẫu.
 * [Chức năng]: Tạo menu "ChoNoi/Build Responsive UI Canvas" trong Unity Editor.
 *              Khi click, script sẽ tự động sinh toàn bộ GameObject UI của Kho đồ và Cửa hàng
 *              với các thiết lập Anchors, Pivot, Layout Group và ScrollRect chuẩn responsive PC landscape.
 * [Dependencies]: UnityEditor, UnityEngine, UnityEngine.UI.
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using ChoNoiMienTay.Presentation;

namespace ChoNoiMienTay.Editor
{
    public static class ResponsiveUIBuilderEditor
    {
        [MenuItem("ChoNoi/UI/Build Responsive UI Canvas")]
        public static void BuildUI()
        {
            // 1. Tạo Canvas chính
            GameObject canvasObj = new GameObject("Responsive_UICanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<GraphicRaycaster>();

            // Cấu hình Canvas Scaler chuẩn PC landscape (1920x1080, Match height=1 hoặc 0.5)
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // Tạo EventSystem nếu chưa có
            if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<EventSystem>();
                eventSystem.AddComponent<InputSystemUIInputModule>();
            }

            // 2. Tạo Panel Kho đồ (Inventory UI)
            GameObject invPanelObj = CreateUIPanel("Inventory_Panel", canvasObj.transform);
            RectTransform invPanel = invPanelObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(invPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(invPanel);
            invPanelObj.SetActive(false); // Ẩn mặc định

            // 2.1 Header Area
            GameObject invHeaderObj = CreateUIPanel("Header_Area", invPanel);
            RectTransform invHeader = invHeaderObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(invHeader, new Vector2(0.0f, 0.9f), new Vector2(1.0f, 1.0f), new Vector2(0.5f, 1.0f));
            ResponsiveLayoutBuilder.ResetOffsets(invHeader);
            AddTextToPanel("Inventory Title", "KHO ĐỒ GHE THƯƠNG HỒ", invHeader.transform, TextAnchor.MiddleLeft, 32);

            // 2.2 Close Button
            GameObject closeBtnObj = CreateUIButton("BtnClose", "Đóng (X)", invHeader);
            RectTransform closeBtn = closeBtnObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(closeBtn, Vector2.one, Vector2.one, Vector2.one);
            closeBtn.anchoredPosition = new Vector2(-20f, -20f);
            closeBtn.sizeDelta = new Vector2(120f, 50f);

            // 2.3 Grid Area Parent (Scroll View)
            GameObject gridParentObj = CreateUIPanel("Grid_Area_Parent", invPanel);
            RectTransform gridParent = gridParentObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(gridParent, Vector2.zero, new Vector2(0.7f, 0.9f), new Vector2(0.0f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(gridParent);
            gridParentObj.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.6f);

            // Thêm ScrollRect & Viewport cho Grid
            ScrollRect scroll = gridParentObj.AddComponent<ScrollRect>();
            GameObject viewportObj = CreateUIPanel("Viewport", gridParent);
            RectTransform viewport = viewportObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(viewport, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(viewport);
            viewportObj.AddComponent<Image>().color = new Color(0, 0, 0, 0.1f);
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            GameObject contentObj = CreateUIPanel("Grid_Content", viewport);
            RectTransform content = contentObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(content, new Vector2(0.0f, 1.0f), new Vector2(1.0f, 1.0f), new Vector2(0.5f, 1.0f));
            ResponsiveLayoutBuilder.ResetOffsets(content);

            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;

            // GridLayoutGroup cho Slots
            GridLayoutGroup gridLayout = contentObj.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(128f, 128f);
            gridLayout.spacing = new Vector2(12f, 12f);
            gridLayout.padding = new RectOffset(15, 15, 15, 15);
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;

            ContentSizeFitter contentFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // 2.4 Details Panel (Bên phải)
            GameObject detailsObj = CreateUIPanel("Details_Panel", invPanel);
            RectTransform details = detailsObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(details, new Vector2(0.7f, 0.0f), new Vector2(1.0f, 0.9f), new Vector2(1.0f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(details);
            detailsObj.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Text hiển thị chi tiết vật phẩm
            GameObject detailsTextObj = AddTextToPanel("DetailsDisplayText", "Chọn vật phẩm để xem chi tiết...", details, TextAnchor.UpperLeft, 24);
            RectTransform detailsText = detailsTextObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(detailsText, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.95f), new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(detailsText);

            // 2.5 Equip Button
            GameObject equipBtnObj = CreateUIButton("BtnEquip", "Sử Dụng", details);
            RectTransform equipBtn = equipBtnObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(equipBtn, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.15f), new Vector2(0.5f, 0.0f));
            ResponsiveLayoutBuilder.ResetOffsets(equipBtn);

            // 3. Tạo Panel Cửa hàng (Shop UI)
            GameObject shopPanelObj = CreateUIPanel("Shop_Panel", canvasObj.transform);
            RectTransform shopPanelRect = shopPanelObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(shopPanelRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(shopPanelRect);
            shopPanelObj.SetActive(false); // Ẩn mặc định

            // 3.1 Shop Header & Wallet Widget
            GameObject shopHeaderObj = CreateUIPanel("Shop_Header", shopPanelObj.transform);
            RectTransform shopHeader = shopHeaderObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(shopHeader, new Vector2(0.0f, 0.9f), new Vector2(1.0f, 1.0f), new Vector2(0.5f, 1.0f));
            ResponsiveLayoutBuilder.ResetOffsets(shopHeader);
            AddTextToPanel("Shop Title", "CHỢ NỔI NAM BỘ — THU MUA & DỊCH VỤ", shopHeader, TextAnchor.MiddleLeft, 32);

            GameObject walletObj = CreateUIPanel("Wallet_Widget", shopHeader);
            RectTransform wallet = walletObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(wallet, new Vector2(0.7f, 0.0f), new Vector2(0.98f, 1.0f), new Vector2(1.0f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(wallet);
            Text walletText = AddTextToPanel("WalletText", "Số dư: 100,000đ", wallet, TextAnchor.MiddleRight, 28).GetComponent<Text>();

            // 3.2 Category Menu (Bên trái)
            GameObject catMenuObj = CreateUIPanel("Category_Menu", shopPanelObj.transform);
            RectTransform catMenu = catMenuObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(catMenu, Vector2.zero, new Vector2(0.2f, 0.9f), new Vector2(0.0f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(catMenu);
            catMenuObj.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f, 0.85f);

            // Thêm VerticalLayout cho categories
            VerticalLayoutGroup catLayout = catMenuObj.AddComponent<VerticalLayoutGroup>();
            catLayout.spacing = 15f;
            catLayout.padding = new RectOffset(10, 10, 20, 20);
            catLayout.childControlWidth = true;
            catLayout.childControlHeight = false;
            catLayout.childForceExpandWidth = true;
            catLayout.childForceExpandHeight = false;

            CreateUIButton("BtnCatNongSan", "Nông Sản", catMenu);
            CreateUIButton("BtnCatDichVu", "Dịch Vụ Ăn Uống", catMenu);
            CreateUIButton("BtnCatNangCap", "Sửa Chữa / Đồ Ghe", catMenu);

            // 3.3 Product Viewport (Ở giữa)
            GameObject prodViewportObj = CreateUIPanel("Product_Viewport", shopPanelObj.transform);
            RectTransform prodViewport = prodViewportObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(prodViewport, new Vector2(0.2f, 0.0f), new Vector2(0.75f, 0.9f), new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(prodViewport);
            prodViewportObj.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.5f);

            // 3.4 Summary Panel (Bên phải)
            GameObject summaryObj = CreateUIPanel("Summary_Panel", shopPanelObj.transform);
            RectTransform summary = summaryObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(summary, new Vector2(0.75f, 0.0f), new Vector2(1.0f, 0.9f), new Vector2(1.0f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(summary);
            summaryObj.AddComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.9f);
            AddTextToPanel("Summary Title", "TÓM TẮT GIAO DỊCH", summary, TextAnchor.UpperCenter, 24);

            // 4. Tạo HUD phụ (General Stats Display)
            GameObject hudPanelObj = CreateUIPanel("HUD_Stats_Panel", canvasObj.transform);
            RectTransform hudPanel = hudPanelObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(hudPanel, new Vector2(0.02f, 0.02f), new Vector2(0.4f, 0.15f), new Vector2(0.0f, 0.0f));
            ResponsiveLayoutBuilder.ResetOffsets(hudPanel);
            hudPanelObj.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);

            GameObject statsTextObj = AddTextToPanel("StatsDisplayText", "Đang tải chỉ số...", hudPanel, TextAnchor.MiddleCenter, 22);
            RectTransform statsText = statsTextObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(statsText, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(statsText);

            // 5. Thêm InventoryUI vào Canvas để quản lý toàn bộ
            InventoryUI uiController = canvasObj.AddComponent<InventoryUI>();
            uiController.inventoryDisplayText = detailsTextObj.GetComponent<Text>();
            uiController.capacityDisplayText = AddTextToPanel("CapacityDisplayText", "Sức chứa: 0/100 kg", invHeader, TextAnchor.MiddleRight, 24).GetComponent<Text>();
            // Neo text sức chứa ở bên phải Header của Kho đồ
            RectTransform capTextRect = uiController.capacityDisplayText.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(capTextRect, new Vector2(0.5f, 0.0f), new Vector2(0.95f, 1.0f), new Vector2(1.0f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(capTextRect);

            uiController.statsDisplayText = statsTextObj.GetComponent<Text>();

            // Tạo các nút mẫu cho InventoryUI để BindButtons hoạt động
            CreateUIButton("BtnBunRieu", "Ăn Bún Riêu (25k)", canvasObj.transform).SetActive(false);
            CreateUIButton("BtnSweetTalk", "Nói ngọt", canvasObj.transform).SetActive(false);
            CreateUIButton("BtnGift", "Tặng Khóm", canvasObj.transform).SetActive(false);
            CreateUIButton("BuyKhom", "Mua Khóm", canvasObj.transform).SetActive(false);
            CreateUIButton("SellKhom", "Bán Khóm", canvasObj.transform).SetActive(false);

            // Bật sẵn Canvas lên
            invPanelObj.SetActive(true);

            // Đăng ký Undo và chọn GameObject mới tạo trong Editor
            Undo.RegisterCreatedObjectUndo(canvasObj, "Build Responsive UI Canvas");
            Selection.activeGameObject = canvasObj;

            Debug.Log("<color=green>[ResponsiveUIBuilder] Đã tự động tạo dựng xong hệ thống UI Responsive cho Kho đồ và Cửa hàng trong Cảnh hiện tại!</color>");
        }

        // --- CÁC HÀM TRỢ GIÚP TẠO NHANH UI ELEMENT ---

        private static GameObject CreateUIPanel(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private static GameObject CreateUIButton(string name, string label, Transform parent)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            btnObj.GetComponent<Image>().color = new Color(0.8f, 0.8f, 0.8f, 1f);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(btnObj.transform, false);
            
            Text txt = textObj.GetComponent<Text>();
            txt.text = label;
            txt.color = Color.black;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(textRect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(textRect);

            return btnObj;
        }

        private static GameObject AddTextToPanel(string name, string content, Transform parent, TextAnchor alignment, int fontSize)
        {
            GameObject textObj = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObj.transform.SetParent(parent, false);

            Text txt = textObj.GetComponent<Text>();
            txt.text = content;
            txt.color = Color.white;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = fontSize;
            txt.alignment = alignment;

            RectTransform rect = textObj.GetComponent<RectTransform>();
            ResponsiveLayoutBuilder.SetAnchorsAndPivot(rect, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResponsiveLayoutBuilder.ResetOffsets(rect);

            return textObj;
        }
    }
}
