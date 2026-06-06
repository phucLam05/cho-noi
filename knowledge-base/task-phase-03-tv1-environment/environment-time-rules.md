# Environment & Optimization Rules

Khi lập trình Phase 3, bắt buộc tuân thủ các quy tắc sau để giữ hiệu suất (Performance) cho game:

## 1. Không dùng Update() cho hệ thống môi trường
**TUYỆT ĐỐI KHÔNG** tính toán sự thay đổi ánh sáng mỗi frame trong hàm `Update()` nếu không cần thiết.
- Thay vào đó, khi `TimeManager` phát ra event (ví dụ mỗi phút in-game trôi qua), `EnvironmentController` mới tiến hành cập nhật nội suy (`Lerp`) ánh sáng và mức nước thông qua một `Coroutine`. 
- Khi quá trình chuyển tiếp (transition) hoàn tất, tắt Coroutine để tiết kiệm CPU.

## 2. Quản lý Thủy triều (Tide System)
- Thay vì thay đổi hình dáng Mesh của nước (rất nặng máy), hãy di chuyển (Translate) trục Y của GameObject chứa Water Mesh và Water BoxCollider. 
- *Công thức:* Vị trí Y của mặt nước nội suy tuyến tính giữa `MaxWaterHeight` (Sáng) và `MinWaterHeight` (Chiều).

## 3. Kiến trúc Độc lập (Decoupling)
- Hệ thống Môi trường KHÔNG ĐƯỢC chứa logic của hệ thống Thời gian.
- `EnvironmentController` chỉ đóng vai trò là "Người quan sát" (Observer). Nó nhận dữ liệu thời gian thô (0.0 đến 24.0) từ `ITimeSystem` và tự map với `EnvironmentProfileSO` để render.