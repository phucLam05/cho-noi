/**
 * ResponsiveLayoutBuilder: Script hỗ trợ thiết lập tự động hệ thống UI/UX co giãn (Responsive) trong Unity.
 * [Chức năng]: Cung cấp các phương thức và ContextMenu (click chuột phải trong Inspector) 
 *              để tự động cấu hình CanvasScaler và RectTransform của 2 màn hình Kho đồ (Inventory) 
 *              và Cửa hàng (Shop) chuẩn theo tỷ lệ màn hình PC landscape.
 * [Dependencies]: UnityEngine, UnityEngine.UI.
 */

using UnityEngine;
using UnityEngine.UI;

namespace ChoNoiMienTay.Presentation
{
    [ExecuteAlways]
    [RequireComponent(typeof(Canvas))]
    public class ResponsiveLayoutBuilder : MonoBehaviour
    {
        [Header("Cấu hình Canvas Scaler")]
        [SerializeField] private float referenceWidth = 1920f;
        [SerializeField] private float referenceHeight = 1080f;
        [SerializeField] [Range(0f, 1f)] private float matchWidthOrHeight = 0.5f;

        [Header("Màn hình Kho đồ (Inventory UI)")]
        [SerializeField] private RectTransform inventoryPanel;
        [SerializeField] private RectTransform inventoryHeader;
        [SerializeField] private RectTransform inventoryCloseButton;
        [SerializeField] private RectTransform inventoryGridParent;
        [SerializeField] private RectTransform inventoryDetailsPanel;
        [SerializeField] private RectTransform inventoryEquipButton;

        [Header("Màn hình Cửa hàng (Shop UI)")]
        [SerializeField] private RectTransform shopPanel;
        [SerializeField] private RectTransform shopWalletWidget;
        [SerializeField] private RectTransform shopCategoryMenu;
        [SerializeField] private RectTransform shopProductViewport;
        [SerializeField] private RectTransform shopSummaryPanel;

        /// <summary>
        /// Tự động cấu hình Canvas Scaler chuẩn cho hiển thị PC ngang (landscape)
        /// </summary>
        [ContextMenu("1. Thiet Lap Canvas Scaler")]
        public void ApplyCanvasScalerSetup()
        {
            CanvasScaler scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(referenceWidth, referenceHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = matchWidthOrHeight;

            Debug.Log($"[ResponsiveLayoutBuilder] Đã cấu hình CanvasScaler: {referenceWidth}x{referenceHeight}, Match = {matchWidthOrHeight}");
        }

        /// <summary>
        /// Áp dụng neo (Anchors) và tâm (Pivot) cho màn hình Kho đồ theo bảng thiết kế
        /// </summary>
        [ContextMenu("2. Thiet Lap Layout Kho Do (Inventory)")]
        public void ApplyInventoryLayout()
        {
            if (inventoryPanel == null)
            {
                Debug.LogWarning("[ResponsiveLayoutBuilder] Thiếu Inventory_Panel reference!");
                return;
            }

            // 1. Panel chính của Kho đồ: Co giãn phủ kín Canvas
            SetAnchorsAndPivot(inventoryPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResetOffsets(inventoryPanel);

            // 2. Header Area: Nằm ở trên cùng, chiếm 10% chiều cao (từ 90% đến 100%)
            if (inventoryHeader != null)
            {
                SetAnchorsAndPivot(inventoryHeader, new Vector2(0.0f, 0.9f), new Vector2(1.0f, 1.0f), new Vector2(0.5f, 1.0f));
                ResetOffsets(inventoryHeader);
            }

            // 3. Nút Đóng (Close Button): Neo ở góc trên cùng bên phải của Header
            if (inventoryCloseButton != null)
            {
                SetAnchorsAndPivot(inventoryCloseButton, Vector2.one, Vector2.one, Vector2.one);
                // Giữ nguyên size của nút, chỉ reset offset để bám vào góc
                inventoryCloseButton.anchoredPosition = new Vector2(-15f, -15f); // Cách lề 15px
            }

            // 4. Vùng hiển thị ô chứa (Grid Area): Chiếm 70% bên trái (từ 0% đến 70% Width, 0% đến 90% Height)
            if (inventoryGridParent != null)
            {
                SetAnchorsAndPivot(inventoryGridParent, Vector2.zero, new Vector2(0.7f, 0.9f), new Vector2(0.0f, 0.5f));
                ResetOffsets(inventoryGridParent);
                ConfigureGridLayoutGroup(inventoryGridParent);
            }

            // 5. Panel chi tiết (Details Panel): Chiếm 30% bên phải (từ 70% đến 100% Width, 0% đến 90% Height)
            if (inventoryDetailsPanel != null)
            {
                SetAnchorsAndPivot(inventoryDetailsPanel, new Vector2(0.7f, 0.0f), new Vector2(1.0f, 0.9f), new Vector2(1.0f, 0.5f));
                ResetOffsets(inventoryDetailsPanel);
            }

            // 6. Nút Sử dụng/Trang bị (Equip Button): Nằm phía dưới Details Panel
            if (inventoryEquipButton != null)
            {
                SetAnchorsAndPivot(inventoryEquipButton, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.15f), new Vector2(0.5f, 0.0f));
                ResetOffsets(inventoryEquipButton);
            }

            Debug.Log("[ResponsiveLayoutBuilder] Đã hoàn thành cấu hình tự động cho màn hình Kho đồ.");
        }

        /// <summary>
        /// Áp dụng neo (Anchors) và tâm (Pivot) cho màn hình Cửa hàng theo bảng thiết kế
        /// </summary>
        [ContextMenu("3. Thiet Lap Layout Cua Hang (Shop)")]
        public void ApplyShopLayout()
        {
            if (shopPanel == null)
            {
                Debug.LogWarning("[ResponsiveLayoutBuilder] Thiếu Shop_Panel reference!");
                return;
            }

            // 1. Panel chính của Cửa hàng: Co giãn phủ kín Canvas
            SetAnchorsAndPivot(shopPanel, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f));
            ResetOffsets(shopPanel);

            // 2. Wallet Widget: Neo góc trên bên phải để theo dõi tiền
            if (shopWalletWidget != null)
            {
                SetAnchorsAndPivot(shopWalletWidget, new Vector2(0.7f, 0.9f), new Vector2(1.0f, 1.0f), new Vector2(1.0f, 1.0f));
                ResetOffsets(shopWalletWidget);
            }

            // 3. Danh mục Menu (Category): Chiếm 20% bên trái (từ 0% đến 20% Width, 0% đến 90% Height)
            if (shopCategoryMenu != null)
            {
                SetAnchorsAndPivot(shopCategoryMenu, Vector2.zero, new Vector2(0.2f, 0.9f), new Vector2(0.0f, 0.5f));
                ResetOffsets(shopCategoryMenu);
                ConfigureVerticalLayoutGroup(shopCategoryMenu);
            }

            // 4. Viewport hiển thị sản phẩm (Products): Chiếm 55% ở giữa (từ 20% đến 75% Width, 0% đến 90% Height)
            if (shopProductViewport != null)
            {
                SetAnchorsAndPivot(shopProductViewport, new Vector2(0.2f, 0.0f), new Vector2(0.75f, 0.9f), new Vector2(0.5f, 0.5f));
                ResetOffsets(shopProductViewport);
            }

            // 5. Panel tóm tắt (Summary Panel): Chiếm 25% bên phải (từ 75% đến 100% Width, 0% đến 90% Height)
            if (shopSummaryPanel != null)
            {
                SetAnchorsAndPivot(shopSummaryPanel, new Vector2(0.75f, 0.0f), new Vector2(1.0f, 0.9f), new Vector2(1.0f, 0.5f));
                ResetOffsets(shopSummaryPanel);
            }

            Debug.Log("[ResponsiveLayoutBuilder] Đã hoàn thành cấu hình tự động cho màn hình Cửa hàng.");
        }

        /// <summary>
        /// Thiết lập neo và tâm cho một RectTransform cụ thể
        /// </summary>
        public static void SetAnchorsAndPivot(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
        {
            if (rect == null) return;
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
        }

        /// <summary>
        /// Khôi phục các Margin về 0 để phần tử UI khớp khít hoàn toàn vào hệ neo Anchor
        /// </summary>
        public static void ResetOffsets(RectTransform rect)
        {
            if (rect == null) return;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Tự động cấu hình GridLayoutGroup cho các ô vật phẩm (Slots)
        /// </summary>
        private void ConfigureGridLayoutGroup(RectTransform target)
        {
            GridLayoutGroup grid = target.GetComponent<GridLayoutGroup>();
            if (grid == null) grid = target.gameObject.AddComponent<GridLayoutGroup>();

            grid.cellSize = new Vector2(128f, 128f);
            grid.spacing = new Vector2(10f, 10f);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.UpperLeft;
            grid.constraint = GridLayoutGroup.Constraint.Flexible;

            ContentSizeFitter fitter = target.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = target.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>
        /// Tự động cấu hình VerticalLayoutGroup cho danh mục hàng hóa
        /// </summary>
        private void ConfigureVerticalLayoutGroup(RectTransform target)
        {
            VerticalLayoutGroup layout = target.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = target.gameObject.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
        }
    }
}
