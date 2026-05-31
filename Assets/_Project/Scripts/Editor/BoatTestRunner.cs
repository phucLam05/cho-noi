/**
 * BoatTestRunner: Static Editor class chạy toàn bộ unit test từ terminal.
 * [Chức năng]: Chạy qua lệnh "Unity -batchmode -executeMethod ChoNoi.Editor.BoatTestRunner.Run"
 *              Kiểm tra BoatStats, công thức vật lý, IBoatInput contract — không cần Play mode.
 *              Kết quả in ra log, exit code 0 = tất cả pass, exit code 1 = có fail.
 * [Dependencies]: BoatStats (Infrastructure), UnityEditor.
 */

using UnityEngine;
using UnityEditor;
using ChoNoi.Infrastructure;

namespace ChoNoi.Editor
{
    public static class BoatTestRunner
    {
        private static int _passed;
        private static int _failed;

        // Có thể gọi từ menu Unity Editor: ChoNoi > Run Tests
        [MenuItem("ChoNoi/Run All Tests")]
        public static void RunFromMenu() => Run();

        /// <summary>
        /// Entry point cho batch mode:
        /// Unity -batchmode -quit -executeMethod ChoNoi.Editor.BoatTestRunner.Run
        /// </summary>
        public static void Run()
        {
            _passed = 0;
            _failed = 0;

            PrintHeader("BOAT PHYSICS TEST SUITE");

            // --- Nhóm 1: BoatStats validation ---
            PrintGroup("1. KIEM TRA BOAT STATS (Unit Test)");
            RunStatsTests();

            // --- Nhóm 2: Công thức vật lý ---
            PrintGroup("2. KIEM TRA CONG THUC VAT LY");
            RunPhysicsFormulaTests();

            // --- Nhóm 3: IBoatInput contract ---
            PrintGroup("3. KIEM TRA IBOATINPUT CONTRACT");
            RunInputContractTests();

            // --- Tổng kết ---
            PrintSummary();

            // Exit code 1 nếu có bài fail (cho CI/CD hoặc script bên ngoài)
            if (Application.isBatchMode)
                EditorApplication.Exit(_failed > 0 ? 1 : 0);
        }

        // ──────────────────────────────────────────────
        // NHÓM 1: BOAT STATS
        // ──────────────────────────────────────────────

        private static void RunStatsTests()
        {
            // Tạo instance với giá trị mặc định đã khai báo trong BoatStats.cs
            var stats = ScriptableObject.CreateInstance<BoatStats>();

            Assert("BoatStats tao duoc instance (khong null)",
                stats != null);

            Assert("ThrustForce phai > 0 (ghe moi co the di chuyen)",
                stats.ThrustForce > 0f);

            Assert("WaterDrag phai > 0 (ghe moi co the giam toc)",
                stats.WaterDrag > 0f);

            Assert("SidewaysDrag phai > 0 (ghe moi chay thang huong mui)",
                stats.SidewaysDrag > 0f);

            Assert("TurnTorque phai > 0 (ghe moi co the be lai)",
                stats.TurnTorque > 0f);

            Assert("SidewaysDrag nen >= WaterDrag (chong truot ngang tot hon can doc)",
                stats.SidewaysDrag >= stats.WaterDrag);

            Object.DestroyImmediate(stats);
        }

        // ──────────────────────────────────────────────
        // NHÓM 2: CÔNG THỨC VẬT LÝ
        // ──────────────────────────────────────────────

        private static void RunPhysicsFormulaTests()
        {
            var stats = ScriptableObject.CreateInstance<BoatStats>();

            // --- Thrust ---
            // F_thrust = forward * throttle * thrustForce
            Vector3 forward      = Vector3.forward;           // (0,0,1)
            float   throttleFull = 1f;
            float   throttleHalf = 0.5f;
            float   throttleBack = -1f;

            Vector3 thrustFull = forward * throttleFull * stats.ThrustForce;
            Vector3 thrustHalf = forward * throttleHalf * stats.ThrustForce;
            Vector3 thrustBack = forward * throttleBack * stats.ThrustForce;

            Assert("Thrust (throttle=1): luc dam tien dung chieu forward",
                thrustFull.z > 0f && Approx(thrustFull.x, 0f) && Approx(thrustFull.y, 0f));

            Assert("Thrust (throttle=0.5): luc bang 50% so voi full throttle",
                Approx(thrustHalf.magnitude, thrustFull.magnitude * 0.5f));

            Assert("Thrust (throttle=-1): luc dam nguoc chieu (lui)",
                thrustBack.z < 0f);

            Assert("Thrust (throttle=0): khong co luc day",
                Approx((forward * 0f * stats.ThrustForce).magnitude, 0f));

            // --- Water Drag ---
            // F_drag = -velocity * waterDrag
            Vector3 velocity = new Vector3(0f, 0f, 5f);  // dang chay tien 5 m/s
            Vector3 drag     = -velocity * stats.WaterDrag;

            Assert("WaterDrag: luc can nguoc chieu voi van toc",
                drag.z < 0f);

            Assert("WaterDrag: do lon ti le voi van toc (van toc x2 -> can x2)",
                Approx((-velocity * 2f * stats.WaterDrag).magnitude,
                       drag.magnitude * 2f));

            Assert("WaterDrag: khi dung im (v=0) khong co luc can",
                Approx((-Vector3.zero * stats.WaterDrag).magnitude, 0f));

            // --- Sideways Resistance ---
            // V_side = right * dot(v, right) ; F = -V_side * sidewaysDrag
            Vector3 right = Vector3.right;  // (1,0,0)

            // Ghe di thang (v theo forward) → khong co thanh phan ngang
            Vector3 vForward        = new Vector3(0f, 0f, 3f);
            Vector3 sidewaysForward = right * Vector3.Dot(vForward, right);
            Assert("SidewaysResistance: ghe di thang (khong truot ngang) → V_side = 0",
                Approx(sidewaysForward.magnitude, 0f));

            // Ghe truot ngang thuần → thanh phan ngang = toàn bộ vận tốc
            Vector3 vSideways        = new Vector3(4f, 0f, 0f);
            Vector3 sidewaysSideways = right * Vector3.Dot(vSideways, right);
            Assert("SidewaysResistance: ghe truot ngang thuan → V_side = v",
                Approx(sidewaysSideways.magnitude, vSideways.magnitude));

            // --- Steering (mô-men lái) ---
            // T = up * steering * turnTorque * |velocity|
            Vector3 up      = Vector3.up;
            float   steer   = 1f;

            // Khi đứng im: |velocity| = 0 → torque = 0 (không lái được khi đứng im)
            float torqueStill = (up * steer * stats.TurnTorque * 0f).magnitude;
            Assert("Steering (dung im): mo-men xoan = 0 (ghe khong quay khi dung im)",
                Approx(torqueStill, 0f));

            // Khi đang chạy: torque > 0
            float speed = 5f;
            float torqueMoving = (up * steer * stats.TurnTorque * speed).magnitude;
            Assert("Steering (dang chay): mo-men xoan > 0 (ghe quay khi co van toc)",
                torqueMoving > 0f);

            // Mô-men tỉ lệ thuận với vận tốc: speed x2 → torque x2
            float torqueDouble = (up * steer * stats.TurnTorque * (speed * 2f)).magnitude;
            Assert("Steering: mo-men ti le thuan voi van toc (v x2 → T x2)",
                Approx(torqueDouble, torqueMoving * 2f));

            Object.DestroyImmediate(stats);
        }

        // ──────────────────────────────────────────────
        // NHÓM 3: IBOATINPUT CONTRACT
        // ──────────────────────────────────────────────

        private static void RunInputContractTests()
        {
            // Dùng MockBoatInput để kiểm tra contract của IBoatInput
            var mock = new MockInput();

            mock.Set(0f, 0f);
            Assert("IBoatInput: throttle=0, steering=0 → ca hai = 0",
                Approx(mock.Throttle, 0f) && Approx(mock.Steering, 0f));

            mock.Set(1f, 1f);
            Assert("IBoatInput: throttle=1 (tien) va steering=1 (phai) hop le",
                mock.Throttle <= 1f && mock.Steering <= 1f);

            mock.Set(-1f, -1f);
            Assert("IBoatInput: throttle=-1 (lui) va steering=-1 (trai) hop le",
                mock.Throttle >= -1f && mock.Steering >= -1f);

            mock.Set(0.5f, -0.75f);
            Assert("IBoatInput: gia tri trung gian (0.5, -0.75) nam trong [-1, 1]",
                mock.Throttle >= -1f && mock.Throttle <= 1f &&
                mock.Steering >= -1f && mock.Steering <= 1f);
        }

        // ──────────────────────────────────────────────
        // MOCK + UTILITIES
        // ──────────────────────────────────────────────

        // Mock đơn giản implement IBoatInput để test contract
        private class MockInput : ChoNoi.Domain.IBoatInput
        {
            public float Throttle { get; private set; }
            public float Steering { get; private set; }
            public void Set(float throttle, float steering)
            {
                Throttle = throttle;
                Steering = steering;
            }
        }

        // So sánh float với ngưỡng sai số
        private static bool Approx(float a, float b, float epsilon = 0.001f)
            => Mathf.Abs(a - b) < epsilon;

        // ──────────────────────────────────────────────
        // PRINT HELPERS
        // ──────────────────────────────────────────────

        private static void Assert(string description, bool condition)
        {
            if (condition)
            {
                _passed++;
                Debug.Log($"  [PASS] {description}");
            }
            else
            {
                _failed++;
                Debug.LogError($"  [FAIL] {description}");
            }
        }

        private static void PrintHeader(string title)
            => Debug.Log($"\n{'='.ToString().PadRight(1)}{'='.ToString().PadLeft(1)}" +
                         $" {title} " +
                         $"{'='.ToString().PadRight(1)}{'='.ToString().PadLeft(1)}\n");

        private static void PrintGroup(string name)
            => Debug.Log($"\n--- {name} ---");

        private static void PrintSummary()
        {
            int total = _passed + _failed;
            string result = _failed == 0 ? "ALL PASS" : $"{_failed} FAIL";
            Debug.Log(
                $"\n==========================================\n" +
                $"  KET QUA: {_passed}/{total} test passed  |  {result}\n" +
                $"==========================================\n"
            );
        }
    }
}
