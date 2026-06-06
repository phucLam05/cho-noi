# Phase 2 (TV1) Execution Plan: Time System & Weight Physics

## Mục tiêu
1. Xây dựng `TimeManager` quản lý đồng hồ in-game và kích hoạt sự kiện chuyển Phase.
2. Cập nhật `BoatController` để nhận dữ liệu khối lượng từ Inventory và làm suy giảm hiệu suất vật lý (tốc độ, bẻ lái).

## Quy trình thực hiện (Step-by-Step)
1. **Time System Architecture (Domain & Application):**
   - Định nghĩa `enum GamePhase { Dawn, Day, Dusk, Night }`.
   - Tạo `ITimeSystem` interface với các event báo hiệu thời gian trôi qua.
   - Lập trình `TimeManager` (kế thừa MonoBehaviour, đặt trong Application Layer). Thiết lập tỷ lệ thời gian (VD: 1 giây thực tế = 1 phút in-game).
2. **Phase Transition Logic:**
   - Trong `TimeManager`, viết logic kiểm tra giờ hiện tại để trigger sự kiện `OnPhaseChanged` (Bình minh: 3h, Ngày: 10h, Chiều: 13h, Tối: 18h). In log Console.
3. **Cross-System Decoupling (Domain):**
   - Để `BoatController` lấy được khối lượng mà không bị phụ thuộc trực tiếp (hard-coupled) vào `InventoryManager`, tạo interface `IWeightProvider` (chứa hàm `float GetCurrentWeightRatio()`).
4. **Physics Integration (Presentation/Controllers):**
   - Cập nhật `BoatController`. Lấy giá trị từ `IWeightProvider`.
   - Áp dụng hệ số suy giảm (Penalty) lên `forwardThrust` và `turnTorque` trong `FixedUpdate`.