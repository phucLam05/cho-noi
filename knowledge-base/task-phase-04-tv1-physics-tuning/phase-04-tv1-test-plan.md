# Phase 4 Test Plan: Durability & Collision Stress Test

Yêu cầu Agent viết script tạo môi trường Stress Test khắc nghiệt nhất cho hệ thống vật lý.

## Test 1: Durability Speed Limit
- **Thiết lập:** Cung cấp một thanh Slider `Durability` giả lập (từ 0 đến 100). Bật tính năng log vận tốc hiện tại (`rb.velocity.magnitude`) ra màn hình UI hoặc Console.
- **Tiêu chí nghiệm thu:** 
  - Khi Durability = 100, ghe đạt vận tốc max (ví dụ: 10m/s).
  - Kéo Durability xuống 10 (Gần hỏng), dù có nhấn giữ ga (Thrust) bao lâu, vận tốc max chỉ dừng lại ở mức 3m/s. Gia tốc lúc khởi hành vẫn mạnh nhưng bị chặn trần tốc độ.

## Test 2: High-Speed Wall Ramming (Đâm tường tốc độ cao)
- **Thiết lập:** Đặt một bức tường rất mỏng (BoxCollider có chiều dày 0.1). Kích hoạt ghe chạy với tốc độ cực đại (nhân x10 lực đẩy để test).
- **Tiêu chí nghiệm thu:**
  - Ghe lao thẳng vào tường mỏng và BỊ CHẶN LẠI HOÀN TOÀN. 
  - Ghe không bị lọt xuyên qua tường.
  - Ghe trượt dọc theo tường nếu đâm ở một góc nghiêng, không bị nảy bật lùi lại quá mạnh, không bị văng lật úp mặt đáy lên trời.