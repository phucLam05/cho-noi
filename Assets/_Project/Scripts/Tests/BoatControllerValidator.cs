/**
 * BoatControllerValidator: Script kiểm thử toàn bộ hệ thống vật lý ghe.
 * [Chức năng]: Cung cấp 4 bài test qua [ContextMenu] (click chuột phải Inspector).
 *   - Test 1 (Unit):       Kiểm tra BoatStats không null và giá trị hợp lệ.
 *   - Test 2 (Physics):    Lực đẩy tiến phải tạo ra vận tốc dương.
 *   - Test 3 (Physics):    Mô-men lái chỉ hoạt động khi ghe đang có vận tốc.
 *   - Test 4 (Physics):    Lực cản nước phải làm ghe giảm tốc sau khi ngắt ga.
 * [Dependencies]: BoatStats (Infrastructure), Rigidbody.
 * [Lưu ý]: Test 2-4 yêu cầu chế độ Play (Application.isPlaying).
 */

using System.Collections;
using UnityEngine;
using ChoNoi.Infrastructure;

namespace ChoNoi.Tests
{
    public class BoatControllerValidator : MonoBehaviour
    {
        [Header("Tham chiếu (kéo thả vào Inspector)")]
        [SerializeField] private BoatStats boatStats;
        [SerializeField] private Rigidbody rb;

        [Header("Thông số kiểm thử")]
        // Số giây áp lực khi test lực đẩy
        [SerializeField] private float thrustTestDuration = 0.5f;
        // Vận tốc tối thiểu phải đạt sau khi áp lực (m/s)
        [SerializeField] private float minExpectedSpeed = 0.05f;
        // Số giây đợi để đo quán tính khi test water drag
        [SerializeField] private float dragTestWaitDuration = 1.5f;

        // ──────────────────────────────────────────────
        // TEST 1 — UNIT TEST (không cần Play mode)
        // ──────────────────────────────────────────────

        /// <summary>
        /// Test 1 - Kiểm tra BoatStats không null và tất cả giá trị lực > 0.
        /// Có thể chạy cả trong Edit mode lẫn Play mode.
        /// </summary>
        [ContextMenu("Test 1 - Kiem Tra BoatStats (Unit Test)")]
        public void ValidateBoatStats()
        {
            LogHeader("TEST 1: KIEM TRA BOAT STATS");
            bool passed = true;

            // Kiểm tra reference
            if (boatStats == null)
            {
                LogFail("BoatStats la NULL! Keo thu file BoatStats.asset vao Inspector.");
                return;
            }

            // Kiểm tra từng chỉ số không âm
            passed &= AssertPositive("ThrustForce",  boatStats.ThrustForce);
            passed &= AssertPositive("WaterDrag",    boatStats.WaterDrag);
            passed &= AssertPositive("SidewaysDrag", boatStats.SidewaysDrag);
            passed &= AssertPositive("TurnTorque",   boatStats.TurnTorque);

            // Cảnh báo nếu ThrustForce quá nhỏ — khó di chuyển trên nước
            if (boatStats.ThrustForce < 1f)
                LogWarn($"ThrustForce = {boatStats.ThrustForce} rat nho, ghe co the khong di chuyen duoc.");

            if (passed) LogPass("Tat ca chi so BoatStats hop le.");
        }

        // ──────────────────────────────────────────────
        // TEST 2 — PHYSICS: LỰC ĐẨY TIẾN
        // ──────────────────────────────────────────────

        /// <summary>
        /// Test 2 - Sau khi áp lực đẩy liên tục trong thrustTestDuration giây,
        /// vận tốc theo hướng mũi ghe phải > minExpectedSpeed.
        /// Chỉ chạy trong Play mode.
        /// </summary>
        [ContextMenu("Test 2 - Luc Day Tien (Physics Test)")]
        public void TestForwardThrust()
        {
            if (!RequirePlayMode("Test 2")) return;
            StartCoroutine(RunThrustTest());
        }

        private IEnumerator RunThrustTest()
        {
            LogHeader("TEST 2: LUC DAY TIEN");

            // Bước 1: Reset trạng thái ghe về đứng yên
            ResetBoat();
            yield return new WaitForFixedUpdate();

            // Bước 2: Áp lực đẩy liên tục trong thrustTestDuration giây
            float elapsed = 0f;
            while (elapsed < thrustTestDuration)
            {
                // Mô phỏng ApplyThrust(throttle=1) và các lực cản
                rb.AddForce(transform.forward * boatStats.ThrustForce, ForceMode.Acceleration);
                ApplyResistanceForces();
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            // Bước 3: Đo vận tốc theo hướng mũi ghe (bỏ qua vận tốc ngang)
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            Debug.Log($"[Validator] Van toc tien sau {thrustTestDuration}s: {forwardSpeed:F3} m/s (can toi thieu: {minExpectedSpeed})");

            if (forwardSpeed > minExpectedSpeed)
                LogPass($"Ghe da di chuyen tien. Van toc = {forwardSpeed:F3} m/s");
            else
                LogFail($"Ghe KHONG di chuyen! Van toc = {forwardSpeed:F3} m/s. Tang ThrustForce trong BoatStats.");

            ResetBoat();
        }

        // ──────────────────────────────────────────────
        // TEST 3 — PHYSICS: LAI GHE
        // ──────────────────────────────────────────────

        /// <summary>
        /// Test 3a: Ghe đứng im → bẻ lái → góc quay KHÔNG được thay đổi (mô-men lái × vận tốc = 0).
        /// Test 3b: Ghe đang chạy → bẻ lái → góc quay PHẢI thay đổi.
        /// </summary>
        [ContextMenu("Test 3 - Mo Men Lai (Physics Test)")]
        public void TestSteering()
        {
            if (!RequirePlayMode("Test 3")) return;
            StartCoroutine(RunSteeringTest());
        }

        private IEnumerator RunSteeringTest()
        {
            LogHeader("TEST 3: MO MEN LAI");

            // ── Test 3a: Đứng im, bẻ lái ──
            ResetBoat();
            yield return new WaitForFixedUpdate();

            float angleBefore = transform.eulerAngles.y;

            // Áp mô-men lái khi vận tốc = 0 → TurnTorque × |velocity| = 0
            for (int i = 0; i < 10; i++)
            {
                rb.AddTorque(transform.up * 1f * boatStats.TurnTorque * rb.linearVelocity.magnitude,
                    ForceMode.Acceleration);
                yield return new WaitForFixedUpdate();
            }

            float angleAfterStill = transform.eulerAngles.y;
            float deltaStill = Mathf.Abs(Mathf.DeltaAngle(angleBefore, angleAfterStill));
            Debug.Log($"[Validator] Test 3a - Goc quay khi dung im: {deltaStill:F3} do");

            if (deltaStill < 0.1f)
                LogPass("Test 3a PASS: Ghe khong quay khi dung im (dung ly thuyet).");
            else
                LogFail($"Test 3a FAIL: Ghe quay {deltaStill:F3} do khi dung im! Kiem tra cong thuc steering.");

            // ── Test 3b: Đang chạy, bẻ lái ──
            ResetBoat();
            yield return new WaitForFixedUpdate();

            // Tạo vận tốc trước
            for (int i = 0; i < 20; i++)
            {
                rb.AddForce(transform.forward * boatStats.ThrustForce, ForceMode.Acceleration);
                yield return new WaitForFixedUpdate();
            }

            float angleBeforeMove = transform.eulerAngles.y;
            float speedBeforeSteer = rb.linearVelocity.magnitude;
            Debug.Log($"[Validator] Toc do truoc khi lai: {speedBeforeSteer:F3} m/s");

            // Bẻ lái phải (steering = 1)
            for (int i = 0; i < 15; i++)
            {
                rb.AddTorque(transform.up * 1f * boatStats.TurnTorque * rb.linearVelocity.magnitude,
                    ForceMode.Acceleration);
                yield return new WaitForFixedUpdate();
            }

            float deltaMove = Mathf.Abs(Mathf.DeltaAngle(angleBeforeMove, transform.eulerAngles.y));
            Debug.Log($"[Validator] Test 3b - Goc quay khi dang chay: {deltaMove:F3} do");

            if (deltaMove > 0.5f)
                LogPass($"Test 3b PASS: Ghe quay {deltaMove:F3} do khi dang chay.");
            else
                LogFail($"Test 3b FAIL: Ghe khong quay khi dang chay! Tang TurnTorque trong BoatStats.");

            ResetBoat();
        }

        // ──────────────────────────────────────────────
        // TEST 4 — PHYSICS: QUAN TINH / WATER DRAG
        // ──────────────────────────────────────────────

        /// <summary>
        /// Test 4 - Sau khi ngắt ga, waterDrag phải làm ghe giảm tốc dần.
        /// Vận tốc sau dragTestWaitDuration giây PHẢI nhỏ hơn vận tốc đỉnh.
        /// </summary>
        [ContextMenu("Test 4 - Quan Tinh Nuoc (Physics Test)")]
        public void TestWaterDrag()
        {
            if (!RequirePlayMode("Test 4")) return;
            StartCoroutine(RunDragTest());
        }

        private IEnumerator RunDragTest()
        {
            LogHeader("TEST 4: QUAN TINH NUOC (WATER DRAG)");

            // Bước 1: Tăng tốc ghe
            ResetBoat();
            for (int i = 0; i < 30; i++)
            {
                rb.AddForce(transform.forward * boatStats.ThrustForce, ForceMode.Acceleration);
                ApplyResistanceForces();
                yield return new WaitForFixedUpdate();
            }

            float peakSpeed = rb.linearVelocity.magnitude;
            Debug.Log($"[Validator] Van toc dinh khi co ga: {peakSpeed:F3} m/s");

            if (peakSpeed < minExpectedSpeed)
            {
                LogFail($"Ghe khong dat van toc dinh ({peakSpeed:F3} m/s). Test khong the tiep tuc.");
                yield break;
            }

            // Bước 2: Ngắt ga — chỉ áp lực cản
            float elapsed = 0f;
            while (elapsed < dragTestWaitDuration)
            {
                ApplyResistanceForces();
                elapsed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            float speedAfterDrag = rb.linearVelocity.magnitude;
            Debug.Log($"[Validator] Van toc sau {dragTestWaitDuration}s ngat ga: {speedAfterDrag:F3} m/s");

            if (speedAfterDrag < peakSpeed * 0.8f)
                LogPass($"Ghe giam toc dung quy luat: {peakSpeed:F3} -> {speedAfterDrag:F3} m/s");
            else
                LogFail($"Ghe KHONG giam toc! Tang WaterDrag trong BoatStats. (Hien tai: {boatStats.WaterDrag})");

            ResetBoat();
        }

        // ──────────────────────────────────────────────
        // CHAY TAT CA TEST
        // ──────────────────────────────────────────────

        /// <summary>
        /// Chạy tuần tự Test 1 → 4. Test 2-4 yêu cầu Play mode.
        /// </summary>
        [ContextMenu(">>> Chay Tat Ca Test <<<")]
        public void RunAllTests()
        {
            LogHeader("CHAY TAT CA TEST");
            ValidateBoatStats();
            if (Application.isPlaying)
                StartCoroutine(RunAllPhysicsTests());
            else
                LogWarn("Test 2-4 can Play mode. Nhan Play roi click lai 'Chay Tat Ca Test'.");
        }

        private IEnumerator RunAllPhysicsTests()
        {
            yield return RunThrustTest();
            yield return RunSteeringTest();
            yield return RunDragTest();
            LogHeader("HOAN TAT TAT CA TEST");
        }

        // ──────────────────────────────────────────────
        // HÀM TIỆN ÍCH NỘI BỘ
        // ──────────────────────────────────────────────

        // Áp lực cản nước và lực cản ngang (giống BoatController)
        private void ApplyResistanceForces()
        {
            Vector3 resistance = -rb.linearVelocity * boatStats.WaterDrag;
            rb.AddForce(resistance, ForceMode.Acceleration);

            Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.linearVelocity, transform.right);
            rb.AddForce(-sidewaysVelocity * boatStats.SidewaysDrag, ForceMode.Acceleration);
        }

        // Đặt ghe về trạng thái đứng yên
        private void ResetBoat()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Kiểm tra đang ở Play mode
        private bool RequirePlayMode(string testName)
        {
            if (Application.isPlaying) return true;
            LogWarn($"{testName} can Play mode. Nhan nut Play roi chay lai.");
            return false;
        }

        // Kiểm tra giá trị > 0
        private bool AssertPositive(string fieldName, float value)
        {
            if (value > 0f)
            {
                Debug.Log($"  [OK] {fieldName} = {value}");
                return true;
            }
            LogFail($"  {fieldName} = {value} (phai > 0!)");
            return false;
        }

        private void LogHeader(string title) =>
            Debug.Log($"\n========== {title} ==========");

        private void LogPass(string msg) =>
            Debug.Log($"<color=green>[PASS] {msg}</color>");

        private void LogFail(string msg) =>
            Debug.LogError($"[FAIL] {msg}");

        private void LogWarn(string msg) =>
            Debug.LogWarning($"[WARN] {msg}");
    }
}
