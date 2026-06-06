/**
 * EnvironmentController: Đồng bộ ánh sáng, sương mù, mực nước theo thời gian (Observer).
 * [Chức năng]: Nghe event OnTimeChanged của TimeManager, nội suy mượt (Coroutine) màu/cường độ
 *              Light, RenderSettings.fogDensity và Y của mặt nước theo EnvironmentProfileSO.
 *              Có slider [Range(0,24)] + OnValidate để preview ngày/đêm tức thì trong Editor.
 *              TUYỆT ĐỐI không dùng Update() — Coroutine tự tắt khi transition xong (tiết kiệm CPU).
 * [Dependencies]: TimeManager (Application); EnvironmentProfileSO (Infrastructure).
 */

using System.Collections;
using UnityEngine;
using ChoNoi.Application;
using ChoNoi.Infrastructure;

namespace ChoNoi.Presentation.Environment
{
    public class EnvironmentController : MonoBehaviour
    {
        [Header("Tham chiếu")]
        [SerializeField] private TimeManager timeManager;
        [SerializeField] private EnvironmentProfileSO profile;
        [SerializeField] private Light directionalLight;
        [SerializeField] private Transform waterTransform;   // GameObject Nước (Mesh + BoxCollider)

        [Header("Chuyển tiếp")]
        [SerializeField] private bool rotateSun = true;
        [SerializeField] private float sunYaw = 30f;
        // Tốc độ "đuổi" hình ảnh theo thời gian mục tiêu (giờ-ảo / giây-thực).
        [SerializeField] private float transitionSpeed = 6f;

        [Header("Editor Preview (Test 1) — kéo để xem ngày/đêm tức thì")]
        [SerializeField, Range(0f, 24f)] private float editorPreviewHour = 12f;
        [SerializeField] private bool applyInEditor = true;

        private float displayedHour = 12f;
        private float targetHour = 12f;
        private Coroutine transition;

        private void OnEnable()
        {
            if (timeManager != null)
                timeManager.OnTimeChanged += HandleTimeChanged;
        }

        private void OnDisable()
        {
            if (timeManager != null)
                timeManager.OnTimeChanged -= HandleTimeChanged;
            if (transition != null) { StopCoroutine(transition); transition = null; }
        }

        /// <summary>
        /// Nhận thời gian thô (giờ, phút) từ TimeManager -> quy về giờ thực 0..24
        /// và khởi động transition nếu chưa chạy.
        /// </summary>
        private void HandleTimeChanged(int hour, int minute)
        {
            targetHour = hour + minute / 60f;
            if (transition == null)
                transition = StartCoroutine(TransitionRoutine());
        }

        /// <summary>
        /// Coroutine nội suy displayedHour -> targetHour rồi TỰ TẮT khi tới nơi.
        /// Tránh tính toán mỗi frame trong Update() (theo environment-time-rules.md).
        /// </summary>
        private IEnumerator TransitionRoutine()
        {
            while (true)
            {
                // Đường ngắn nhất trên vòng 24h (xử lý wrap 23h -> 0h) qua phép quy về độ.
                float deltaDeg = Mathf.DeltaAngle(displayedHour / 24f * 360f, targetHour / 24f * 360f);
                if (Mathf.Abs(deltaDeg) < 0.05f) break;

                float maxStep = transitionSpeed * Time.deltaTime;                  // giờ/giây
                float stepHours = Mathf.Min(maxStep, Mathf.Abs(deltaDeg) / 360f * 24f);
                displayedHour = Mathf.Repeat(displayedHour + Mathf.Sign(deltaDeg) * stepHours, 24f);
                ApplyEnvironment(displayedHour);
                yield return null;
            }

            displayedHour = targetHour;
            ApplyEnvironment(displayedHour);
            transition = null;   // tắt coroutine — không còn tốn CPU
        }

        /// <summary>
        /// Áp toàn bộ thông số môi trường tại 1 mốc giờ (instant) — dùng cho cả runtime & editor.
        /// </summary>
        /// <param name="hour">Giờ trong ngày 0..24.</param>
        private void ApplyEnvironment(float hour)
        {
            if (profile == null) return;
            float t = Mathf.Repeat(hour, 24f) / 24f;

            if (directionalLight != null)
            {
                directionalLight.color = profile.EvaluateLightColor(t);
                directionalLight.intensity = profile.EvaluateLightIntensity(t);
                if (rotateSun)
                    directionalLight.transform.rotation = Quaternion.Euler(profile.EvaluateSunPitch(t), sunYaw, 0f);
            }

            RenderSettings.fog = true;
            RenderSettings.fogDensity = profile.EvaluateFogDensity(t);

            if (waterTransform != null)
            {
                Vector3 p = waterTransform.position;
                p.y = profile.EvaluateWaterHeight(t);
                waterTransform.position = p;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Test 1: kéo slider editorPreviewHour -> thấy ngày/đêm đổi tức thì (không cần Play).
        /// Chỉ chạy trong Edit mode để không can thiệp đồng hồ game lúc Play.
        /// </summary>
        private void OnValidate()
        {
            if (!applyInEditor || UnityEngine.Application.isPlaying) return;
            displayedHour = targetHour = editorPreviewHour;
            ApplyEnvironment(editorPreviewHour);
        }
#endif
    }
}
