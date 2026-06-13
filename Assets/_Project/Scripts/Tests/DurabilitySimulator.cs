/**
 * DurabilitySimulator: Script stress-test độ bền & va chạm cho ghe (Phase 4).
 * [Chức năng]: Vừa là nguồn cấp tỷ lệ độ bền (implement IDurabilityProvider cho BoatController),
 *              vừa cung cấp GUI để: kéo Slider Durability 0–100, đọc vận tốc real-time
 *              (rb.linearVelocity.magnitude) và trần tốc độ hiện tại, và nút "LAO TOI"
 *              bắn ghe về trước với lực x10 để test đâm tường tốc độ cao.
 * [Dependencies]: IDurabilityProvider (Domain), BoatStats (Infrastructure), Rigidbody.
 * [Setup]: Gắn CÙNG GameObject với BoatController; kéo Rigidbody + BoatStats vào Inspector.
 */

using UnityEngine;
using ChoNoi.Domain;
using ChoNoi.Infrastructure;

namespace ChoNoi.Tests
{
    public class DurabilitySimulator : MonoBehaviour, IDurabilityProvider
    {
        [Header("Mô phỏng độ bền")]
        [SerializeField, Range(0f, 100f)] private float durabilityPercent = 100f;

        [Header("Tham chiếu (kéo thả vào Inspector)")]
        [SerializeField] private Rigidbody rb;
        [SerializeField] private BoatStats boatStats;

        // Hệ số nhân lực khi bấm "LAO TOI" để test đâm tường tốc độ cao (Test 2).
        [Header("Stress test va chạm")]
        [SerializeField] private float ramForceMultiplier = 10f;

        private int lastLoggedSpeed = -1;

        /// <summary>
        /// IDurabilityProvider — trả tỷ lệ độ bền [0,1] cho BoatController.
        /// </summary>
        public float GetDurabilityRatio() => Mathf.Clamp01(durabilityPercent / 100f);

        // Trần tốc độ hiện tại theo công thức của physics-tuning-rules (giữ tối thiểu 30%).
        private float CurrentMaxSpeed =>
            (boatStats != null ? boatStats.BaseMaxSpeed : 10f) * Mathf.Clamp(GetDurabilityRatio(), 0.3f, 1f);

        private void OnGUI()
        {
            const float w = 380f;
            GUILayout.BeginArea(new Rect(10, 180, w, 200), GUI.skin.box);

            GUILayout.Label("<b>STRESS TEST DO BEN & VA CHAM (Durability)</b>");

            // Slider độ bền 0–100.
            GUILayout.Label($"Do ben: {durabilityPercent:0} / 100   (ratio = {GetDurabilityRatio():0.00})");
            durabilityPercent = GUILayout.HorizontalSlider(durabilityPercent, 0f, 100f);

            GUILayout.Space(6);
            float speed = rb != null ? rb.linearVelocity.magnitude : 0f;
            GUILayout.Label($"Tran toc do (max): {CurrentMaxSpeed:0.0} m/s");
            GUILayout.Label($"Van toc hien tai : {speed:0.00} m/s");

            if (GUILayout.Button("LAO TOI (Ram x" + ramForceMultiplier + ")") && rb != null && boatStats != null)
            {
                // Bắn ghe về trước bằng xung lực lớn để lao vào tường (Test 2).
                rb.AddForce(transform.forward * boatStats.ThrustForce * ramForceMultiplier, ForceMode.VelocityChange);
                Debug.Log("[DurabilitySimulator] LAO TOI! Dam tuong toc do cao.");
            }

            GUILayout.EndArea();

            LogIfChanged(speed);
        }

        // In Console khi vận tốc (làm tròn) đổi — tiện theo dõi trần tốc độ (Test 1).
        private void LogIfChanged(float speed)
        {
            int rounded = Mathf.RoundToInt(speed);
            if (rounded == lastLoggedSpeed) return;
            lastLoggedSpeed = rounded;
            Debug.Log($"[DurabilitySimulator] Van toc: {speed:0.00} m/s | Tran: {CurrentMaxSpeed:0.0} m/s | Do ben: {durabilityPercent:0}");
        }
    }
}
