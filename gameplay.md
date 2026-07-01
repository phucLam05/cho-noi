# Tài Liệu Thiết Kế Tính Năng & Core Gameplay MVP — Chợ Nổi Miền Tây

Bản thiết kế này mô tả chi tiết các tính năng chính, vòng lặp gameplay (Core Loop) và các cơ chế tương tác trong phiên bản thử nghiệm **MVP Chợ Nổi Miền Tây**.

---

## 1. VÒNG LẶP GAMEPLAY CỐT LÕI (CORE LOOP)

Gameplay của trò chơi xoay quanh chu kỳ ngày/đêm thực tế của một thương hồ miền Tây, phân chia rõ ràng thành các giai đoạn giao thương buổi sáng và chuẩn bị/bảo trì buổi chiều tối.

```mermaid
flowchart TD
    START["🌅 Bình Minh (03:00 AM)"] --> PREP["Chuẩn bị hàng & treo Cây Bẹo (Phím B)"]
    PREP --> MARKET["🌞 Sáng Giao Thương (05:00 - 10:00 AM)"]
    
    MARKET --> SPAWN["Spawners ngoài bản đồ xuất hiện thuyền NPC"]
    SPAWN --> TRAFFIC["Luồng thuyền chạy qua lại giao thương và tham quan"]
    TRAFFIC --> INTERACT{"Người chơi áp sát & nhấn E?"}
    
    INTERACT -->|Ghe Khách hàng| BARGAIN["Thương lượng sỉ (Bargain UI)"]
    INTERACT -->|Ghe Hủ tiếu / Cà phê| FOOD["Ăn uống dạo (+Thể lực -Tiền)"]
    INTERACT -->|Ghe Du lịch| TALK["Giao lưu nhận xét tin tức"]
    
    BARGAIN --> DEAL{"Chốt đơn thành công?"}
    DEAL -->|Có| REVENUE["Bán hàng sỉ thu tiền lớn (+Tiền -Hàng)"]
    DEAL -->|Không| LEAVE["Khách bỏ đi, tổn thất thể lực nói ngọt"]
    
    FOOD --> NEXT{"Còn trong khung giờ chợ?"}
    TALK --> NEXT
    REVENUE --> NEXT
    LEAVE --> NEXT
    
    NEXT -->|Có & Người chơi ở vùng chợ Z: 55-95| SPAWN
    NEXT -->|Hết giờ (10:00 AM)| TANCHO["🌤️ Chiều Tà Trùng Tu (10:00 AM - 18:00 PM)"]
    
    TANCHO --> BUY["Vào rạch thu mua nông sản hoặc đến Trại Ghe"]
    BUY --> UPGRADE["Ghé WoodPost nâng cấp / sửa chữa ghe"]
    UPGRADE --> NIGHT["🌙 Đêm Buông (18:00 PM - 03:00 AM)"]
    
    NIGHT --> SLEEP_CHECK{"Về bến nhà Z < 45f?"}
    SLEEP_CHECK -->|Có| SLEEP["😴 Đi Ngủ (Phím E)"]
    SLEEP_CHECK -->|Không| PENALTY["Dầm đêm hao thể lực -> Ngất xỉu (Phạt tiền)"]
    
    PENALTY --> RESPAWN["Được đưa về nhà an toàn"]
    RESPAWN --> SLEEP
    
    SLEEP --> SUMMARY["Bảng Tổng Kết Ngày & Auto-Save"]
    SUMMARY --> START
```

---

## 2. CHI TIẾT CÁC TÍNH NĂNG CHÍNH CỦA MVP

### 🌅 A. Hệ Thống Thời Gian & Đẩy Nhanh Tốc Độ Thử Nghiệm
* **TimeManager & DayFlowController**:
  * Trò chơi được chia làm 4 phase chính: **Dawn** (03:00 - 10:00), **Day** (10:00 - 13:00), **Dusk** (13:00 - 18:00), và **Night** (18:00 - 03:00 hôm sau).
  * Lắng nghe đổi phase để hiển thị thông báo hướng dẫn (Ví dụ: *"Hừng đông lên rồi! Treo nông sản lên Cây Bẹo để gọi khách"*).
* **Tốc độ thử nghiệm (8 phút/ngày)**:
  * Tốc độ thời gian được tăng tốc với hệ số `timeScale = 3f` (1 giây thực tế = 3 phút trong game). 
  * Người chơi hoàn thành chu kỳ một ngày trọn vẹn trong **8 phút thực tế**, tối ưu hóa thời gian thử nghiệm vòng lặp game.

---

### ⛵ B. Phân Chia 3 Khu Vực Địa Lý Trên Bản Đồ (Tọa Độ Z)
Không gian sông nước được phân định rõ ràng bằng tọa độ để người chơi buộc phải di chuyển ghe thực hiện nhiệm vụ:
1. **Nhà Trên Bờ / Bến Xuất Phát (Z < 45f)**: 
   - Người chơi bắt đầu ngày mới trên chân cầu gỗ, đi bộ xuống bến để lên ghe. 
   - Đây là nơi duy nhất cho phép nhấn phím `E` để đi ngủ khi trời tối.
2. **Khu Chợ Nổi Buổi Sáng (Z từ 55f đến 95f)**: 
   - Trọng tâm giao thương. Người chơi phải chèo ghe ra đây thì hệ thống mới kích hoạt spawn khách hàng. Nếu đậu ghe ở bến nhà hoặc trại ghe, chợ sẽ không hoạt động.
3. **Trại Ghe Xóm Nước (Z từ 105f đến 140f)**: 
   - Nằm ở ngã ba sông phía trên. Nơi neo đậu của **Ghe Buôn Lớn (MerchantLargeBoat)** để bán lượng lớn hàng tồn kho và cọc gỗ **WoodPost** để nâng cấp/sửa chữa ghe.

---

### 👥 C. Hệ Thống Spawn & Phân Loại Thuyền NPC
Hệ thống spawn thuyền liên tục mỗi 2 - 6 giây, giới hạn tối đa 12 thuyền hoạt động đồng thời trên sông. Để chống hiện tượng xuất hiện đột ngột giữa sông, thuyền được spawn ở rìa bản đồ (ngoài tầm nhìn camera) và chèo vào.

Trò chơi phân chia thành 3 nhóm thuyền NPC:
1. **Thuyền Khách Mua Hàng (Buyer - 70%)**:
   - Chỉ xuất hiện khi người chơi treo nông sản lên Cây Bẹo.
   - NPC tự tìm đến áp sát ghe người chơi, dừng lại và chờ tương tác.
   - Khi giao dịch kết thúc hoặc người chơi từ chối, họ sẽ chèo ngược về rìa sông và tự huỷ.
2. **Thuyền Khách Du Lịch (Tourist - 15%)**:
   - Chèo qua lại dọc ngã ba sông theo lộ trình định sẵn để tạo không khí sầm uất.
   - Người chơi có thể lái ghe áp sát, nhấn `E` để trò chuyện, hỏi thăm thời tiết hoặc tin tức. Tự huỷ ở cuối lộ trình tuần tra.
3. **Thuyền Dịch Vụ Ăn Uống (Food Vendor - 15%)**:
   - Di chuyển tuần tra trên sông.
   - Người chơi có thể áp sát để tương tác mua đồ ăn (Bún nước lèo, bún riêu) để **hồi phục thể lực** trực tiếp dưới nước, tránh bị kiệt sức.

---

### 🎋 D. Chiến Lược Cây Bẹo (Marketing)
* **Cơ chế treo hàng**: Nhấn phím `B` mở giao diện Cây Bẹo bên mũi ghe. Người chơi kéo thả nông sản (Bí Đao, Khóm, Cam, Dưa Hấu) từ khoang ghe lên sào tre để tiếp thị.
* **Mặt hàng mong muốn**: NPC khách hàng spawn ra sẽ chỉ muốn mua loại quả đang được treo trên Cây Bẹo của người chơi (`DesiredItem`).
* **Sức hút Cây Bẹo**: Treo càng nhiều loại nông sản thì tần suất spawn khách mua hàng càng nhanh (attract multiplier).

---

### 💰 E. Hệ Thống Kinh Tế & Thương Lượng Sỉ (Haggling)
* **Minigame mặc cả**: Sử dụng hệ thống mặc cả thông qua thanh thiện cảm NPC. 
  - Người chơi tiêu hao thể lực (Stamina) để đưa ra đề xuất giá bán mong muốn. 
  - NPC có thể đồng ý, từ chối hoặc đưa ra giá đề nghị ngược lại (counter offer).
* **Mua sỉ/lẻ**: Mua nông sản giá gốc từ các nhà vườn nhỏ (mua lẻ) và bán sỉ số lượng lớn cho khách chợ nổi hoặc Ghe Buôn Lớn để ăn chênh lệch giá.

---

### 🔧 F. Trại Ghe & Bảo Trì Nâng Cấp Ghe
Tương tác với cọc gỗ `WoodPost` để mở UI Trại Ghe:
* **Sửa chữa**: Sử dụng tiền VND trực tiếp phục hồi độ bền ghe (`Durability`). Ghe bị va quẹt nhiều sẽ hỏng hóc, làm chậm tốc độ di chuyển.
* **Nâng cấp**:
  - *Sức chứa khoang*: Chở được nhiều nông sản hơn để bán sỉ.
  - *Mái che*: Bảo vệ nông sản khỏi hỏng hóc do thời tiết.
  - *Động cơ*: Gia tốc và tốc độ di chuyển nhanh hơn.
  - *Cây Bẹo*: Cho phép treo nhiều loại quả tiếp thị hơn cùng lúc.

---

### 😴 G. Đêm Muộn & Cơ Chế Kết Thúc Ngày (Ngủ)
* **Phạt đêm muộn (Faint)**:
  - Phase đêm (sau 18:00 PM), chèo ghe ngoài sông lớn sẽ bị trừ thể lực liên tục do lạnh và sương mù.
  - Nếu thể lực về 0, người chơi ngất xỉu và được dân làng đưa về bến nhà, bị phạt hỏng ghe nhẹ và trừ 5.000 VNĐ viện phí.
* **Nghỉ ngơi tại Bến Nhà**:
  - Chèo ghe về bến (`Z < 45f`), nhấn phím `E` để kích hoạt ngủ.
  - Màn hình chuyển sang bảng **Tổng Kết Ngày**: Hiển thị số ngày đã qua, số tiền hiện có, độ bền ghe và thể lực.
  - Hệ thống thực hiện tự động lưu game (`SaveLoadManager`) để người chơi có thể tiếp tục chơi từ điểm này khi reload.
  - Hồi phục hoàn toàn thể lực cho ngày mới và reset toàn bộ trạng thái NPC thương hội.
