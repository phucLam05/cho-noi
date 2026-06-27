# Phase 1 Execution Plan: Core System & Boat Controller

## Mục tiêu
Thiết lập nền móng kỹ thuật vững chắc và hệ thống điều khiển ghe dựa trên vật lý (Rigidbody).

## Quy trình thực hiện (Step-by-Step)
1. **Setup Project Environment:** Kiểm tra cấu trúc thư mục, .gitignore và khởi tạo các folder Domain/Infrastructure/Presentation.
2. **Data-Driven Configuration:** Tạo `BoatStats.cs` (ScriptableObject) để quản lý thông số vật lý của ghe.
3. **Input Decoupling:** Tạo interface `IBoatInput` và implement lớp `PCBoatInput` để tách biệt logic điều khiển.
4. **Physics Implementation:** Xây dựng `BoatController.cs` sử dụng `FixedUpdate` để áp dụng lực vật lý.
5. **Validation:** Chạy thử với các khối Cube (Greyboxing) để tinh chỉnh cảm giác lái.