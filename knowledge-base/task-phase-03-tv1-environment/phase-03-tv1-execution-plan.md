# Phase 3 (TV1) Execution Plan: Dynamic Environment & Tide System

## Mục tiêu
Đồng bộ hóa hệ thống Thời gian (`TimeManager`) với ánh sáng (Lighting), sương mù (Fog) và mức nước (Tide System) để tạo ra môi trường sống động và tác động trực tiếp đến vật lý của ghe.

## Quy trình thực hiện (Step-by-Step)
1. **Data Driven Configuration (Infrastructure):**
   - Tạo ScriptableObject `EnvironmentProfileSO`. Chứa các dải dữ liệu cho từng GamePhase (Dawn, Day, Dusk, Night): Màu Directional Light, Cường độ sáng, Mật độ Fog (Fog Density), và Chiều cao mặt nước (Water Y-Position).
2. **Event Listening (Presentation/Controllers):**
   - Tạo `EnvironmentController.cs` (kế thừa MonoBehaviour). Script này sẽ Đăng ký (Subscribe) vào event `OnTimeChanged` hoặc `OnPhaseChanged` của `TimeManager` ở Phase 2.
3. **Smooth Visual Transition (Lighting & Fog):**
   - Viết logic `Mathf.Lerp` hoặc dùng `Coroutine` để chuyển đổi mượt mà màu sắc của Light và mật độ Fog giữa các mốc thời gian, tránh việc ánh sáng bị giật cục khi chuyển Phase.
4. **Tide System (Physics/Water):**
   - Đồng bộ vị trí Y của Mesh/Collider mặt nước theo thời gian thực.
   - Khi chiều đến (Dusk), nước hạ xuống mức thấp nhất (Low Tide).
5. **Boat Grounding Logic (Vật lý mắc cạn):**
   - Đảm bảo `BoatController` có xử lý va chạm với đáy sông (Terrain/Riverbed). Khi nước hạ thấp, ghe sẽ tự động chạm collider của đáy và bị áp dụng lực ma sát lớn (cản trở di chuyển).