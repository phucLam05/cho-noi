/**
 * BoatController: MonoBehaviour điều khiển vật lý ghe chính.
 * [Chức năng]: Nhận đầu vào từ IBoatInput, áp dụng 4 lực vật lý lên Rigidbody
 *              trong FixedUpdate: lực đẩy, cản nước, cản ngang, mô-men lái.
 *              Lấy tỷ lệ tải trọng từ IWeightProvider và áp dụng Performance
 *              Multiplier: ghe càng nặng → tăng tốc chậm, bẻ lái lỳ, cản nước tăng.
 *              Phase 3: phát hiện mắc cạn (chạm đáy sông theo riverbedLayer) → giảm
 *              mạnh lực đẩy/lái và thêm ma sát, mô phỏng ghe bị kẹt khi nước hạ.
 *              Phase 4: khóa trần vận tốc theo độ bền (IDurabilityProvider) và cấu hình
 *              Rigidbody (Continuous + freeze rotation X/Z) chống xuyên tường, chống lật.
 * [Dependencies]: IBoatInput, IWeightProvider, IDurabilityProvider (Domain),
 *                 BoatStats (Infrastructure), Rigidbody.
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

        [Header("Mắc cạn (Grounding) — Phase 3")]
        // Layer của đáy sông / Terrain. Khi ghe chạm collider thuộc layer này -> mắc cạn.
        [SerializeField] private LayerMask riverbedLayer;
        // Hệ số lực đẩy/lái còn lại khi mắc cạn (0.05 = chỉ còn 5%, ghe gần như đứng yên).
        [SerializeField, Range(0f, 1f)] private float groundedThrustPenalty = 0.05f;
        // Lực cản phụ rất lớn khi mắc cạn để mô phỏng ma sát với đáy sông.
        [SerializeField] private float groundedExtraDrag = 8f;

        private Rigidbody rb;
        private IBoatInput boatInput;
        // Nguồn cấp tỷ lệ tải trọng (tùy chọn). Null → ghe coi như trống, hiệu suất 100%.
        private IWeightProvider weightProvider;
        private IDurabilityProvider durabilityProvider;
        // True khi ghe đang chạm đáy sông (mắc cạn).
        private bool isGrounded;

        public bool IsGrounded => isGrounded;
        public float EngineThrustMultiplier { get; set; } = 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            boatInput = GetComponent<IBoatInput>();
            // Decouple: chỉ cần một component bất kỳ trên ghe implement các interface này.
            weightProvider = GetComponent<IWeightProvider>();
            durabilityProvider = GetComponent<IDurabilityProvider>();

            // Chống xuyên tường (tunneling) khi ghe chạy nhanh — bắt buộc theo physics-tuning-rules.
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            // Chỉ cho xoay quanh trục Y → va chạm không làm ghe lật úp / văng lên trời.
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            // Bước 0: Tính hệ số suy giảm hiệu suất theo tải trọng (1 lần / FixedUpdate).
            // WeightRatio ∈ [0,1]; Performance = 1 - ratio*MaxPenaltyFactor (đầy tải → tối thiểu).
            // dragMul: chở nặng làm lực cản nước tăng nhẹ (tối đa +50% khi đầy tải).
            float weightRatio = weightProvider != null
                ? Mathf.Clamp01(weightProvider.GetCurrentWeightRatio())
                : 0f;
            float performance = 1f - weightRatio * boatStats.MaxPenaltyFactor;
            float dragMultiplier = 1f + weightRatio * 0.5f;

            // Mắc cạn: nhân thêm groundedThrustPenalty (chỉ khi grounded, không thì = 1).
            if (isGrounded)
                performance *= groundedThrustPenalty;

            ApplyThrust(boatInput.Throttle, performance);
            ApplyWaterDrag(dragMultiplier);
            ApplySidewaysResistance();
            ApplySteering(boatInput.Steering, performance);

            // Ma sát đáy sông: lực cản phụ ngược chiều vận tốc khi mắc cạn.
            if (isGrounded)
                rb.AddForce(-rb.linearVelocity * groundedExtraDrag, ForceMode.Acceleration);

            // Bước 5 (Phase 4) - Khóa trần vận tốc theo độ bền (SAU mọi lực).
            ClampVelocityByDurability();
        }

        /// <summary>
        /// Giới hạn vận tốc tối đa theo độ bền. Độ bền KHÔNG giảm thrustForce (ghe hỏng
        /// vẫn nhích đi được) mà chỉ khóa trần tốc độ, giữ tối thiểu 30% để ghe không
        /// kẹt chết giữa sông khi độ bền = 0.
        /// </summary>
        private void ClampVelocityByDurability()
        {
            float durabilityRatio = durabilityProvider != null ? durabilityProvider.GetDurabilityRatio() : 1f;
            // currentMaxSpeed = baseMaxSpeed * clamp(ratio, 0.3, 1.0)
            float currentMaxSpeed = boatStats.BaseMaxSpeed * Mathf.Clamp(durabilityRatio, 0.3f, 1f);

            if (rb.linearVelocity.magnitude > currentMaxSpeed)
                rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
        }

        // Phát hiện mắc cạn: chạm liên tục với collider thuộc riverbedLayer.
        private void OnCollisionStay(Collision collision)
        {
            if (IsRiverbed(collision.gameObject.layer))
                isGrounded = true;
        }

        // Rời khỏi đáy sông (nước dâng lại) -> hết mắc cạn.
        private void OnCollisionExit(Collision collision)
        {
            if (IsRiverbed(collision.gameObject.layer))
                isGrounded = false;
        }

        // Kiểm tra một layer có nằm trong riverbedLayer mask không.
        private bool IsRiverbed(int layer) => (riverbedLayer.value & (1 << layer)) != 0;

        /// <summary>
        /// Bước 1 - Lực đẩy: Áp dụng lực tiến/lùi theo hướng mũi ghe (transform.forward).
        /// ForceMode.Acceleration bỏ qua khối lượng để cảm giác điều khiển đồng nhất.
        /// </summary>
        /// <param name="throttle">Giá trị từ -1 (lùi) đến 1 (tiến).</param>
        /// <param name="performance">Hệ số hiệu suất [0,1] theo tải trọng (1 = đầy lực).</param>
        private void ApplyThrust(float throttle, float performance)
        {
            // F_thrust = forward * throttle * thrustForce * performance * engineThrustMultiplier
            rb.AddForce(transform.forward * throttle * boatStats.ThrustForce * performance * EngineThrustMultiplier, ForceMode.Acceleration);
        }

        /// <summary>
        /// Bước 2 - Lực cản nước: Tạo lực ngược chiều vận tốc tổng để ghe không trôi mãi.
        /// Mô phỏng lực ma sát của nước; tăng nhẹ theo dragMultiplier khi ghe chở nặng.
        /// </summary>
        /// <param name="dragMultiplier">Hệ số nhân lực cản theo tải trọng (≥ 1).</param>
        private void ApplyWaterDrag(float dragMultiplier)
        {
            // F_resistance = -velocity * waterDrag * dragMultiplier
            Vector3 resistance = -rb.linearVelocity * boatStats.WaterDrag * dragMultiplier;
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
        /// Nhân performance để ghe chở nặng bẻ lái lỳ (trễ) hơn.
        /// </summary>
        /// <param name="steering">Giá trị từ -1 (trái) đến 1 (phải).</param>
        /// <param name="performance">Hệ số hiệu suất [0,1] theo tải trọng (1 = đầy lực).</param>
        private void ApplySteering(float steering, float performance)
        {
            // T_steer = up * steering * turnTorque * |velocity| * performance
            rb.AddTorque(
                transform.up * steering * boatStats.TurnTorque * rb.linearVelocity.magnitude * performance,
                ForceMode.Acceleration
            );
        }
    }
}
