# Phase 2 Test Plan: Đồng hồ & Vật lý tải trọng

Yêu cầu Agent viết thêm các đoạn code Debug/Log và Script Test để xác nhận 2 nghiệm thu (Acceptance Criteria) sau:

## Test 1: Time & Phase Trigger
- **Phương pháp:** Viết một script `TimeLogger` lắng nghe event `OnPhaseChanged` từ `TimeManager`.
- **Expected Outcome:** Console phải tự động log ra thông báo ĐÚNG VỚI KHUNG GIỜ. 
  - "Bắt đầu Phase 1: Bình Minh Giao Thương (03:00 AM)"
  - "Bắt đầu Phase 2: Chiều Tà Thu Mua (13:00 PM)"
- Bố trí biến `timeScale` cao (VD: x60) để test nhanh trong vài chục giây thực tế.

## Test 2: Weight Impact on Physics
- **Phương pháp:** Bổ sung tính năng mô phỏng khối lượng vào `BoatControllerTest` hoặc tạo GUI nhỏ trên màn hình (dùng `OnGUI`) cho phép kéo Slider thay đổi giá trị `CurrentWeight`.
- **Expected Outcome:** - Khi Slider Weight = 0: Console log "Tốc độ: 100%, Lực bẻ lái: 100%". Ghe chạy nhanh nhạy.
  - Khi Slider Weight = Max: Console log "Tốc độ giảm còn 60%, Bẻ lái nặng nề". Cảm giác điều khiển (gia tốc, rẽ hướng) bị delay rõ rệt.