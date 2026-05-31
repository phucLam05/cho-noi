using UnityEngine;

namespace ChoNoiMienTay.Gameplay
{
    [CreateAssetMenu(fileName = "NewServiceData", menuName = "Cho Noi/Service Data", order = 2)]
    public class ServiceData : ScriptableObject
    {
        [Header("Service Info")]
        public string serviceID;
        public string serviceName;
        
        [Header("Cost")]
        public int costMoney; // Giá tiền để mua dịch vụ này

        [Header("Benefits")]
        [Tooltip("Lượng thể lực hồi phục (ví dụ: Bún riêu hồi 50)")]
        public float staminaRestoreAmount;
        
        [Tooltip("Lượng độ bền hồi phục (ví dụ: Sửa ghe hồi 100)")]
        public float durabilityRestoreAmount;
    }
}
