# Phase 3 Test Plan: Lighting & Tide System

Yêu cầu Agent viết script test hoặc bổ sung GUI cho phép Editor test độc lập.

## Test 1: Time-lapse Visuals
- **Phương pháp:** Tạo một Slider UI (hoặc biến `[Range(0, 24)]` trong Inspector) để Editor có thể kéo trượt thời gian trong ngày từ 0h đến 24h.
- **Expected Outcome:** - Kéo về 5h sáng: Đèn mờ dần, màu hơi xanh/vàng cam, sương mù (Fog) dày đặc.
  - Kéo đến 12h trưa: Đèn cực sáng, màu trắng/vàng gắt, sương mù tan biến hoàn toàn.
  - Sự chuyển tiếp phải diễn ra cực kỳ mượt mà, không bị chớp giật.

## Test 2: Mắc Cạn (Grounding Physics)
- **Phương pháp:** Đặt ghe tại một vị trí "kênh rạch nông" có địa hình (Terrain) nhô cao. Chỉnh thời gian sang 15h chiều.
- **Expected Outcome:** Mặt nước hạ xuống (Y giảm). Trọng lực kéo ghe xuống chạm vào Collider của đáy sông. Khi người chơi cố gắng nhấn ga (Thrust), ghe bị lỳ lại, không thể chạy hoặc chạy cực kỳ chậm do ma sát vật lý với Terrain.