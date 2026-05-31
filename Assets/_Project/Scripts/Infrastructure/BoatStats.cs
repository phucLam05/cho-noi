/**
 * BoatStats: ScriptableObject chứa toàn bộ chỉ số vật lý của ghe.
 * [Chức năng]: Lưu trữ tham số cấu hình theo nguyên tắc Data-Driven.
 *              Mọi chỉ số đều chỉnh được trong Inspector, không hard-code.
 *              Gồm 4 nhóm: lực đẩy, lực cản nước, lực cản ngang, mô-men lái.
 * [Dependencies]: Không có.
 */

using UnityEngine;

namespace ChoNoi.Infrastructure
{
    [CreateAssetMenu(fileName = "BoatStats", menuName = "ChoNoi/Boat Stats")]
    public class BoatStats : ScriptableObject
    {
        [Header("Lực đẩy")]
        // Lực đẩy tiến áp lên Rigidbody theo transform.forward (ForceMode.Acceleration)
        [SerializeField] private float thrustForce = 10f;

        [Header("Lực cản nước")]
        // Hệ số cản theo chiều vận tốc tổng — mô phỏng ma sát nước
        [SerializeField] private float waterDrag = 2f;

        [Header("Lực cản ngang")]
        // Hệ số cản trượt ngang — giữ ghe chạy đúng hướng mũi, không trượt sang bên
        [SerializeField] private float sidewaysDrag = 5f;

        [Header("Mô-men lái")]
        // Mô-men xoắn bẻ lái, nhân thêm vận tốc để ghe chỉ quay khi đang chạy
        [SerializeField] private float turnTorque = 3f;

        public float ThrustForce  => thrustForce;
        public float WaterDrag    => waterDrag;
        public float SidewaysDrag => sidewaysDrag;
        public float TurnTorque   => turnTorque;
    }
}
