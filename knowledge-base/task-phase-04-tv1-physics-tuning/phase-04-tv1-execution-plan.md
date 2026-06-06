# Phase 4 Execution Plan: Physics Tuning & Bug Fixing

## Mục tiêu
1. Tích hợp thanh Độ bền (Durability) vào `BoatController` để giới hạn Vận tốc tối đa (Top Speed) khi ghe bị hỏng.
2. Sửa triệt để lỗi xuyên tường (Clipping) và tối ưu phản hồi va chạm (Collision Response) để ghe không bị văng loạn xạ khi đụng độ.

## Quy trình thực hiện (Step-by-Step)
1. **Decoupling System (Domain):**
   - Tạo interface `IDurabilityProvider` (tương tự như `IWeightProvider` ở Phase 2) chứa hàm trả về tỷ lệ độ bền hiện tại: `float GetDurabilityRatio()`.
2. **Speed Penalty Implementation (Controllers):**
   - Cập nhật `BoatController.cs`. 
   - Lấy `DurabilityRatio` (từ 0.0 đến 1.0). Tính toán giới hạn vận tốc: Ghe càng hỏng, lực đẩy (Thrust) không đổi nhưng vận tốc tối đa (Max Velocity) bị khóa ở mức thấp.
3. **Anti-Clipping Setup (Physics):**
   - Điều chỉnh cấu hình của `Rigidbody` thông qua code (`Awake`) hoặc Inspector. Chuyển `CollisionDetectionMode` từ `Discrete` (mặc định) sang `Continuous` hoặc `ContinuousDynamic`.
   - Nếu cần thiết, thêm logic Raycast quét phía trước mũi ghe để chủ động phanh (brake) trước khi xảy ra va chạm xuyên qua các mỏng (như vách lá, cọc tiêu).
4. **Collision Polish (Physics Materials):**
   - Tạo và gán `PhysicMaterial` cho ghe và môi trường. Đảm bảo `Bounciness` (Độ nảy) ở mức cực thấp (0.0 hoặc 0.1) để ghe đâm vào bờ sẽ trượt dọc theo bờ thay vì nảy ngược ra sau như quả bóng.