/**
 * EnvironmentProfileSO: ScriptableObject cấu hình môi trường theo thời gian trong ngày.
 * [Chức năng]: Lưu Gradient màu sáng, AnimationCurve cường độ sáng / mật độ fog / mực nước /
 *              góc nắng theo trục 0..24h (chuẩn hóa 0..1). Data-driven, chỉnh trong Inspector,
 *              không hard-code. EnvironmentController gọi các hàm Evaluate để render.
 * [Dependencies]: Không có.
 */

using UnityEngine;

namespace ChoNoi.Infrastructure
{
    [CreateAssetMenu(fileName = "EnvironmentProfile", menuName = "ChoNoi/Environment Profile")]
    public class EnvironmentProfileSO : ScriptableObject
    {
        [Header("Ánh sáng (trục thời gian 0..1 = 0h..24h)")]
        [SerializeField] private Gradient lightColorOverDay;
        [SerializeField] private AnimationCurve lightIntensityOverDay = AnimationCurve.EaseInOut(0, 0, 1, 1);
        // Góc dốc (pitch) của mặt trời theo giờ — mô phỏng nắng lên/lặn (tùy chọn).
        [SerializeField] private AnimationCurve sunPitchOverDay = AnimationCurve.Linear(0, -90, 1, 270);

        [Header("Sương mù (Fog Density)")]
        [SerializeField] private AnimationCurve fogDensityOverDay = AnimationCurve.EaseInOut(0, 0.05f, 1, 0f);

        [Header("Thủy triều (Tide) — Y của mặt nước")]
        [SerializeField] private float maxWaterHeight = 0f;    // Sáng: nước cao
        [SerializeField] private float minWaterHeight = -2f;   // Chiều: nước thấp (Low Tide)
        // Hệ số nội suy 0..1 theo giờ: 1 = maxWaterHeight, 0 = minWaterHeight.
        [SerializeField] private AnimationCurve waterLevelOverDay = AnimationCurve.EaseInOut(0, 1, 1, 1);

        public float MaxWaterHeight => maxWaterHeight;
        public float MinWaterHeight => minWaterHeight;

        /// <summary>Màu Directional Light tại thời điểm t01 (= giờ/24).</summary>
        public Color EvaluateLightColor(float t01)
            => lightColorOverDay != null ? lightColorOverDay.Evaluate(Mathf.Repeat(t01, 1f)) : Color.white;

        /// <summary>Cường độ Directional Light tại t01.</summary>
        public float EvaluateLightIntensity(float t01)
            => lightIntensityOverDay.Evaluate(Mathf.Repeat(t01, 1f));

        /// <summary>Góc dốc (pitch, độ) của mặt trời tại t01.</summary>
        public float EvaluateSunPitch(float t01)
            => sunPitchOverDay.Evaluate(Mathf.Repeat(t01, 1f));

        /// <summary>Mật độ sương mù tại t01 (không âm).</summary>
        public float EvaluateFogDensity(float t01)
            => Mathf.Max(0f, fogDensityOverDay.Evaluate(Mathf.Repeat(t01, 1f)));

        /// <summary>
        /// Mực nước Y nội suy tuyến tính giữa min/max theo waterLevelOverDay (Tide).
        /// </summary>
        /// <param name="t01">Thời gian chuẩn hóa 0..1 (= giờ/24).</param>
        public float EvaluateWaterHeight(float t01)
            => Mathf.Lerp(minWaterHeight, maxWaterHeight, waterLevelOverDay.Evaluate(Mathf.Repeat(t01, 1f)));
    }
}
