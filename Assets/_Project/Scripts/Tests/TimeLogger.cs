/**
 * TimeLogger: Script kiểm thử Test 1 (Time & Phase Trigger) trong test-plan.
 * [Chức năng]: Lắng nghe event OnPhaseChanged / OnTimeChanged từ TimeManager và in
 *              Console thông báo ĐÚNG KHUNG GIỜ mỗi khi đổi Phase. Dùng để nghiệm thu
 *              hệ thống thời gian mà không cần UI. Đặt timeScale cao (vd x60) trên
 *              TimeManager để test nhanh trong vài chục giây thực.
 * [Dependencies]: TimeManager (Application), GamePhase (Domain).
 * [Setup]: Kéo GameObject chứa TimeManager vào field Time Manager trong Inspector.
 */

using UnityEngine;
using ChoNoi.Domain;
using ChoNoi.Application;

namespace ChoNoi.Tests
{
    public class TimeLogger : MonoBehaviour
    {
        [Header("Tham chiếu (kéo thả vào Inspector)")]
        [SerializeField] private TimeManager timeManager;

        // Bật để log từng phút (rất nhiều log) — mặc định chỉ log khi đổi Phase.
        [SerializeField] private bool logEveryMinute = false;

        private void OnEnable()
        {
            if (timeManager == null)
            {
                Debug.LogError("[TimeLogger] Chua gan TimeManager vao Inspector!");
                return;
            }

            timeManager.OnPhaseChanged += HandlePhaseChanged;
            timeManager.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            if (timeManager == null) return;

            timeManager.OnPhaseChanged -= HandlePhaseChanged;
            timeManager.OnTimeChanged -= HandleTimeChanged;
        }

        /// <summary>
        /// In ra thông báo bắt đầu Phase mới với tên tiếng Việt + mốc giờ chuẩn.
        /// </summary>
        private void HandlePhaseChanged(GamePhase phase)
        {
            Debug.Log($"<color=cyan>[TimeLogger] {GetPhaseMessage(phase)}</color>");
        }

        /// <summary>
        /// (Tùy chọn) In giờ:phút mỗi khi phút thay đổi nếu bật logEveryMinute.
        /// </summary>
        private void HandleTimeChanged(int hour, int minute)
        {
            if (logEveryMinute)
                Debug.Log($"[TimeLogger] Thoi gian: {hour:00}:{minute:00}");
        }

        // Map GamePhase -> thông báo tiếng Việt kèm khung giờ (theo test-plan).
        private string GetPhaseMessage(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Dawn:  return "Bắt đầu Phase: Bình Minh Giao Thương (03:00 AM)";
                case GamePhase.Day:   return "Bắt đầu Phase: Ban Ngày Thu Mua (10:00 AM)";
                case GamePhase.Dusk:  return "Bắt đầu Phase: Chiều Tà Thu Mua (13:00 PM)";
                case GamePhase.Night: return "Bắt đầu Phase: Tối Bảo Trì Tại Bến (18:00 PM)";
                default:              return $"Bắt đầu Phase: {phase}";
            }
        }
    }
}
