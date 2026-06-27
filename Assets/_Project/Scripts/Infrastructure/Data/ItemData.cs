using UnityEngine;

namespace ChoNoiMienTay.Infrastructure
{
    [CreateAssetMenu(fileName = "NewItemData", menuName = "ChoNoi/Data/Item Data", order = 1)]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Info")]
        public string itemID;
        public string itemName;
        
        [Header("Economy & Physics")]
        [Tooltip("Giá gốc của vật phẩm để tính toán mua bán")]
        public int basePrice;
        
        [Tooltip("Khối lượng của vật phẩm, ảnh hưởng đến độ nặng của ghe")]
        public float weight;

        [Header("Visuals (Future Integration)")]
        [Tooltip("Prefab hoặc 3D Model sẽ được load và gán lên ghe/cây bẹo")]
        public GameObject modelPrefab;
        
        [Tooltip("Icon 2D hiển thị trong UI (nếu có)")]
        public Sprite icon;
    }
}
