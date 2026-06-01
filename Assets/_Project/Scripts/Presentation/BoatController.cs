/**
 * BoatController: MonoBehaviour điều khiển vật lý ghe chính.
 * [Chức năng]: Nhận đầu vào từ IBoatInput, áp dụng 4 lực vật lý lên Rigidbody
 *              trong FixedUpdate: lực đẩy, cản nước, cản ngang, mô-men lái.
 * [Dependencies]: IBoatInput (Domain), BoatStats (Infrastructure), Rigidbody.
 */

using UnityEngine;
using ChoNoi.Domain;
using ChoNoi.Infrastructure;

namespace ChoNoi.Presentation
{
    [RequireComponent(typeof(Rigidbody))]
    public class BoatController : MonoBehaviour
    {
        [SerializeField] private BoatStats boatStats;

        public BoatStats Stats
        {
            get => boatStats;
            set => boatStats = value;
        }

        private Rigidbody rb;
        private IBoatInput boatInput;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            boatInput = GetComponent<IBoatInput>();
        }

        private void FixedUpdate()
        {
            ApplyThrust(boatInput.Throttle);
            ApplyWaterDrag();
            ApplySidewaysResistance();
            ApplySteering(boatInput.Steering);
        }

        /// <summary>
        /// Bước 1 - Lực đẩy: Áp dụng lực tiến/lùi theo hướng mũi ghe (transform.forward).
        /// ForceMode.Acceleration bỏ qua khối lượng để cảm giác điều khiển đồng nhất.
        /// </summary>
        /// <param name="throttle">Giá trị từ -1 (lùi) đến 1 (tiến).</param>
        private void ApplyThrust(float throttle)
        {
            // F_thrust = forward * throttle * thrustForce
            rb.AddForce(transform.forward * throttle * boatStats.ThrustForce, ForceMode.Acceleration);
        }

        /// <summary>
        /// Bước 2 - Lực cản nước: Tạo lực ngược chiều vận tốc tổng để ghe không trôi mãi.
        /// Mô phỏng lực ma sát của nước tác động lên thân ghe.
        /// </summary>
        private void ApplyWaterDrag()
        {
            // F_resistance = -velocity * waterDrag
            Vector3 resistance = -rb.linearVelocity * boatStats.WaterDrag;
            rb.AddForce(resistance, ForceMode.Acceleration);
        }

        /// <summary>
        /// Bước 3 - Lực cản ngang: Triệt tiêu vận tốc trượt sang hai bên (transform.right).
        /// Quan trọng nhất để ghe chạy đúng hướng mũi, không bị drift.
        /// </summary>
        private void ApplySidewaysResistance()
        {
            // Chiếu vận tốc lên trục ngang của ghe để tách phần trượt ngang
            // V_sideways = right * dot(velocity, right)
            Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);

            // Áp lực ngược để triệt tiêu phần trượt ngang đó
            rb.AddForce(-sidewaysVelocity * boatStats.SidewaysDrag, ForceMode.Acceleration);
        }

        /// <summary>
        /// Bước 4 - Mô-men lái: Xoay ghe quanh trục Y.
        /// Nhân thêm vận tốc hiện tại để ghe chỉ bẻ lái khi đang chạy — thực tế hơn.
        /// </summary>
        /// <param name="steering">Giá trị từ -1 (trái) đến 1 (phải).</param>
        private void ApplySteering(float steering)
        {
            // T_steer = up * steering * turnTorque * |velocity|
            rb.AddTorque(
                transform.up * steering * boatStats.TurnTorque * rb.linearVelocity.magnitude,
                ForceMode.Acceleration
            );
        }
    }
}
