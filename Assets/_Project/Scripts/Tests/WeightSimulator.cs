/**
 * WeightSimulator: Script mô phỏng tải trọng ghe để nghiệm thu Test 2 (Weight Impact).
 * [Chức năng]: Vừa là nguồn cấp tỷ lệ tải (implement IWeightProvider cho BoatController),
 *              vừa cung cấp Slider OnGUI cho phép kéo thay đổi CurrentWeight ngay trong
 *              Play mode để kiểm chứng cảm giác ghe "nặng lên". Hiển thị real-time
 *              Performance (Tốc độ %, Bẻ lái %) đúng công thức Weight Penalty.
 * [Dependencies]: IWeightProvider (Domain), BoatStats (Infrastructure).
 * [Setup]: Gắn CÙNG GameObject với BoatController để được lấy qua GetComponent.
 *          (Tùy chọn) Kéo BoatStats vào field để hiển thị đúng % theo MaxPenaltyFactor.
 */

using UnityEngine;
using ChoNoi.Domain;
using ChoNoi.Infrastructure;

namespace ChoNoi.Tests
{
    public class WeightSimulator : MonoBehaviour, IWeightProvider
    {
        [Header("Mô phỏng tải trọng")]
        [SerializeField] private float maxCapacity = 100f;
        [SerializeField] private float currentWeight = 0f;

        [Header("Tham chiếu (tùy chọn — để hiển thị % chính xác)")]
        [SerializeField] private BoatStats boatStats;

        // Để chỉ log khi % thay đổi đáng kể, tránh spam Console.
        private int lastLoggedPercent = -1;

        /// <summary>
        /// IWeightProvider — trả tỷ lệ tải [0,1] cho BoatController.
        /// </summary>
        public float GetCurrentWeightRatio()
        {
            if (maxCapacity <= 0f) return 0f;
            return Mathf.Clamp01(currentWeight / maxCapacity);
        }

        // Hệ số phạt tối đa: lấy từ BoatStats nếu có, không thì mặc định 0.4 (giống rules).
        private float MaxPenaltyFactor => boatStats != null ? boatStats.MaxPenaltyFactor : 0.4f;

        /// <summary>
        /// GUI debug: Slider kéo CurrentWeight + Label hiển thị hiệu suất theo tải.
        /// </summary>
        private void OnGUI()
        {
            const float w = 360f;
            GUILayout.BeginArea(new Rect(10, 10, w, 160), GUI.skin.box);

            float ratio = GetCurrentWeightRatio();
            float performance = 1f - ratio * MaxPenaltyFactor;
            int speedPercent = Mathf.RoundToInt(performance * 100f);

            GUILayout.Label("<b>MO PHONG TAI TRONG GHE (Weight Simulator)</b>");
            GUILayout.Label($"Khoi luong: {currentWeight:0} / {maxCapacity:0} kg   (ratio = {ratio:0.00})");

            // Slider kéo thay đổi khối lượng hiện tại.
            currentWeight = GUILayout.HorizontalSlider(currentWeight, 0f, maxCapacity);

            GUILayout.Space(6);
            GUILayout.Label($"Hieu suat (Performance): {performance:0.00}");
            GUILayout.Label($"Toc do: {speedPercent}%   |   Be lai: {speedPercent}%");

            if (ratio <= 0.001f)
                GUILayout.Label("=> Ghe trong: chay nhanh, be lai nhay.");
            else if (ratio >= 0.999f)
                GUILayout.Label($"=> Ghe DAY tai: toc do giam con {speedPercent}%, be lai nang ne.");

            GUILayout.EndArea();

            LogIfChanged(speedPercent);
        }

        // In Console khi % hiệu suất đổi (đáp ứng Expected Outcome của Test 2).
        private void LogIfChanged(int speedPercent)
        {
            if (speedPercent == lastLoggedPercent) return;
            lastLoggedPercent = speedPercent;
            Debug.Log($"[WeightSimulator] Toc do: {speedPercent}%, Luc be lai: {speedPercent}%");
        }
    }
}
