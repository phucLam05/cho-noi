# Chợ Nổi Miền Tây

3D Simulation / Management game lấy bối cảnh chợ nổi miền Tây Nam Bộ Việt Nam.  
Dự án được xây dựng bằng **Unity 6 (6000.4.7f1)** theo mô hình **Clean Architecture**, phân tách rành mạch logic lõi, giao diện UI và tương tác vật lý.

---

## Mục lục

- [Tổng quan game & Game Loop](#tổng-quan-game--game-loop)
- [Tech Stack](#tech-stack)
- [Kiến trúc dự án (Clean Architecture)](#kiến-trúc-dự-án-clean-architecture)
- [Cấu trúc thư mục chi tiết](#cấu-trúc-thư-mục-chi-tiết)
- [Hệ thống & Tính năng chính](#hệ-thống--tính-năng-chính)
- [Cấu hình chức năng NPC (Dành cho nhà phát triển)](#cấu-hình-chức-năng-npc-dành-cho-nhà-phát-triển)
- [Chạy Tests & Build Cảnh](#chạy-tests--build-cảnh)

---

## Tổng quan game & Game Loop

Trò chơi mô phỏng chu kỳ làm việc và đời sống thương hồ qua 3 Phase thời gian:

| Phase | Thời gian | Hoạt động chính |
|---|---|---|
| **Phase 1: Bình Minh** | 3AM – 10AM | Lái ghe tranh bến, quảng bá hàng bằng **Cây Bẹo**, mặc cả bán sỉ với **Thương Lái**. |
| **Phase 2: Chiều Tà** | 1PM – 6PM | Đi rạch nhỏ thu mua nông sản lẻ giá gốc, về **Trại Ghe** để sửa chữa và nâng cấp. |
| **Phase 3: Chạng Vạng** | 6PM – 8PM | Sắp xếp kho hàng trên ghe, chuẩn bị Cây Bẹo, đi ngủ (Save game) chuyển ngày mới. |

---

## Tech Stack

- **Engine**: Unity 6 (6000.4.7f1)
- **Ngôn ngữ**: C# (.NET Standard 2.1)
- **Input System**: Unity New Input System (đối thoại, lái ghe, kéo thả UI bẹo)
- **Physics**: Rigidbody + FixedUpdate (lực đẩy tiến/lùi, cản nước trượt ngang)
- **UI System**: Unity UI (Canvas + RectTransform) xây dựng lập trình động (Procedural UI)
- **Dữ liệu**: ScriptableObjects (Data-Driven, dễ dàng cân bằng game mà không cần sửa code)

---

## Kiến trúc dự án (Clean Architecture)

Dự án tuân thủ nghiêm ngặt 4 lớp kiến trúc nhằm phân tách các thành phần độc lập:

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation                         │ MonoBehaviour, Input, Vật lý, UI
│   BoatController | ShorePlayerController | UI Scripts   │
└────────────┬─────────────────────────────┬──────────────┘
             │ implement                   │ use
             ▼                             ▼
┌─────────────────────────────────────────────────────────┐
│                       Domain                            │ Logic nghiệp vụ thuần C#, không MonoBehaviour
│     IBoatInput | ITimeSystem | GamePhase.cs             │
└─────────────────────────────────────────────────────────┘
             ▲
             │ data
┌─────────────────────────────────────────────────────────┐
│                    Infrastructure                       │ ScriptableObjects dữ liệu, Quản lý Save/Load
│    BoatStats | SaveLoadManager | BargainingEconomyConfig│
└─────────────────────────────────────────────────────────┘
```

---

## Cấu trúc thư mục chi tiết

```
Assets/
├── InputSystem_Actions.inputactions   # Định nghĩa Input Action Map
├── Scenes/                            # Chứa cảnh build
└── _Project/
    ├── Scripts/
    │   ├── Domain/                    # Lớp lõi: Interface di chuyển, hệ thống thời gian, enum Phase
    │   │   ├── IBoatInput.cs
    │   │   ├── ITimeSystem.cs
    │   │   └── GamePhase.cs
    │   ├── Application/               # Quản lý chu kỳ game
    │   │   └── TimeManager.cs         # Đồng hồ in-game, trigger chuyển Phase
    │   ├── Infrastructure/            # Cấu hình dữ liệu (ScriptableObjects), Save/Load
    │   │   ├── BoatStats.cs           # Chỉ số vật lý của ghe
    │   │   ├── ItemData.cs            # Định nghĩa vật phẩm (khóm, bí đao, củ sắn...)
    │   │   ├── InventoryManager.cs    # Quản lý tải trọng, khoang chứa ghe
    │   │   ├── BoatUpgradeCatalogSO.cs# Catalog nâng cấp ghe
    │   │   ├── EnvironmentProfileSO.cs# Cấu hình thời tiết/sương mù/ánh sáng
    │   │   ├── BargainingEconomyConfig.cs # Cấu hình giá cả mặc cả sỉ
    │   │   └── SaveLoadManager.cs     # Tự động lưu game khi ngủ
    │   ├── Presentation/              # MonoBehaviours xử lý vật lý, điều khiển và thực thể
    │   │   ├── BoatController.cs      # Chuyển động ghe
    │   │   ├── PCBoatInput.cs         # Đọc bàn phím/tay cầm lái ghe
    │   │   ├── BoatFollowCamera.cs    # Camera theo dõi ghe
    │   │   ├── Player/                # Nhân vật (di chuyển trên bộ, tiền túi, thể lực, tương tác)
    │   │   │   ├── ShorePlayerController.cs
    │   │   │   ├── BoatBoardingController.cs (Lên/xuống ghe an toàn lên cầu tàu)
    │   │   │   ├── PlayerNpcTradeInteractor.cs (Dò tìm tương tác NPC gần nhất)
    │   │   │   └── PlayerStats.cs
    │   │   ├── NPC/                   # Trí tuệ NPC
    │   │   │   ├── NpcTradeTarget.cs  # Phân loại tương tác NPC (Bargain, Trade, Upgrade)
    │   │   │   └── SimpleNpcWander.cs # Di chuyển tuần tra của NPC
    │   │   ├── Environment/           # Nội suy ánh sáng, sương mù theo thời gian
    │   │   │   ├── EnvironmentController.cs
    │   │   │   └── AmbientBob.cs
    │   │   └── Managers/              # Hệ thống quản lý kinh tế và tin tức
    │   │       ├── EconomyManager.cs  # Trung tâm thanh toán mua bán sỉ/lẻ
    │   │       └── MarketNewsController.cs
    │   ├── UI/                        # Giao diện HUD và Đối thoại
    │   │   ├── RiverMarketHUD.cs      # HUD trên cùng (Tiền, Thể lực, Độ bền), Menu nâng cấp
    │   │   └── FullSimulatorUI.cs     # Đối thoại Witcher 3, kéo thả Cây Bẹo, chọn số lượng sỉ
    │   ├── Editor/                    # Các script tạo cảnh tự động và chạy test
    │   │   ├── RiverMarketSceneBuilder.cs  # Tạo địa hình, sông ngòi, cầu tàu, spawn thực thể
    │   │   ├── FullSimulatorSceneBuilder.cs# Khởi tạo các hệ thống quản lý, phân loại NPC
    │   │   └── BoatTestRunner.cs      # Trình chạy unit test
    │   └── Tests/                     # Các test xác thực thủ công và log thời gian
    │       ├── BoatControllerValidator.cs
    │       └── TimeLogger.cs
    └── Prefabs/                       # Prefab mô hình 3D
```

---

## Hệ thống & Tính năng chính

### 1. Di chuyển và Vật lý Ghe (`BoatController.cs`)
- Sử dụng công thức vật lý giả lập dòng nước: Lực đẩy tiến/lùi, cản nước tịnh tiến và đặc biệt là **hệ số cản trượt ngang (Sideways Drag)** giúp giữ hướng mũi ghe khi rẽ.
- **Steering nhân vận tốc**: Lực lái tỉ lệ thuận với tốc độ di chuyển hiện tại, phản ánh chân thực việc bánh lái chỉ có tác dụng khi có dòng nước chảy qua.
- **Tải trọng ảnh hưởng tốc độ**: Tổng khối lượng hàng hóa trong khoang chứa ảnh hưởng trực tiếp đến gia tốc và quán tính của ghe.

### 2. Giao diện đối thoại & Mặc cả sỉ kiểu Witcher 3
- Hộp đối thoại phụ đề nằm ở **dưới cùng chính giữa** với nền đen mờ sang trọng. Tên NPC có màu vàng nổi bật.
- Danh sách lựa chọn của người chơi nằm ở **phía rìa phải, bên trên phụ đề**, được đánh số thứ tự rõ ràng (`1. `, `2. `) để dễ điều khiển.
- **Giá đề xuất mặc định thông minh**: Khi mở bảng ra giá sỉ, thanh trượt mặc định trỏ về giá mở cửa của NPC (`NpcOpeningPrice`) ở lượt đầu tiên và bắt đầu từ mức giá mà người chơi đã đề xuất trước đó (`PlayerProposedPrice`) ở các lượt đôi co tiếp theo.
- **Cơ chế xác suất mặc cả suy giảm mũ**: NPC đồng ý ngay lập tức nếu đơn giá bán $\le$ giá trần chấp nhận ẩn của họ. Nếu cao hơn, xác suất chấp nhận $P_{accept} = e^{-5 \times Ratio}$ và xác suất bỏ đi $P_{walk\_away} = 1.0 - e^{-3 \times Ratio \times (Turn + 1)}$. Chi tiết công thức toán học xem tại **[bargaining_mechanics.md](file:///e:/university/pru/cho-noi-mien-tay/knowledge-base/task-phase-04-tv1-physics-tuning/bargaining_mechanics.md)**.
- **Giới hạn 1 giao dịch/ngày**: Mỗi NPC chỉ thực hiện tối đa 1 phiên giao dịch bán sỉ mỗi ngày (thành công hoặc hủy bỏ). Hệ thống khóa tương tác mua bán lại cho đến khi người chơi đi ngủ chuyển sang ngày mới.

### 3. Kéo thả Cây Bẹo và Đa Tab (Tabbed Cargo UI)
- Phím `B` mở giao diện quản lý đa năng gồm 2 Tab:
  - **Tab Khoang Thuyền**: Danh sách thống kê toàn bộ hàng hóa đang có trên ghe, khối lượng và giá trị tương đương.
  - **Tab Cây Bẹo**: Giao diện kéo thả nông sản tiếp thị lên sào tre. Có nút [Gỡ] nhanh và hỗ trợ kéo đè vật phẩm mới để thay thế vật phẩm cũ.
- **Lưới ô hàng cuộn dọc & Căn chỉnh tối ưu**: Các lưới ô chứa hàng hóa tại Tab Khoang Thuyền và Tab Cây Bẹo (kho hàng trên ghe) được bọc bằng `ScrollRect` và `RectMask2D` động để cuộn trơn tru khi nâng cấp tải trọng ghe. Kho hàng trên ghe của Tab Cây Bẹo hiển thị theo lưới 4 cột căn giữa giúp phân bố không gian cân đối, không bị che khuất.
- **Chặn phím tương tác chồng đè**: Phím `B` tự động bị vô hiệu hóa khi người chơi đang đối thoại hoặc mặc cả với NPC để ngăn chặn lỗi giao diện chồng chéo.

---

## Cấu hình chức năng NPC (Dành cho nhà phát triển)

Nếu bạn muốn thay đổi hoặc tinh chỉnh chức năng của các NPC trong game, bạn có thể thực hiện tại các khu vực sau:

### 1. Thay đổi Kiểu tương tác của NPC (Bargain, Trade, Upgrade)
Mỗi NPC có một component `NpcTradeTarget` quy định bán kính tương tác và loại tương tác. Việc phân loại này được cấu hình tự động tại **[FullSimulatorSceneBuilder.cs](file:///d:/Ky7/pru/assignment/new/cho-noi-mien-tay/Assets/_Project/Scripts/Editor/FullSimulatorSceneBuilder.cs)** bên trong hàm `BuildFullScene()`:
```csharp
ConfigureTradeTarget("MerchantLargeBoat", InteractionTargetType.Bargain);   // Thương lái mặc cả sỉ
ConfigureTradeTarget("FoodVendorSmallBoat", InteractionTargetType.Trade);   // Ghe bán dạo ăn uống, mua lẻ
ConfigureTradeTarget("ShoreVillagerNpc", InteractionTargetType.Trade);      // Dân làng mua bán lẻ, giao lưu
ConfigureTradeTarget("WoodPost", InteractionTargetType.Upgrade);            // Cọc nâng cấp bảo trì trại ghe
```
- **Cách chỉnh**: Bạn chỉ cần thay đổi tham số `InteractionTargetType` (ví dụ: đổi từ `Trade` thành `Bargain` nếu muốn cho phép dân làng trả giá sỉ). Sau khi đổi, hãy chạy menu **ChoNoi > Scenes > Build Full UI Simulator Scene** trong Unity Editor để áp dụng.

### 2. Cân bằng kinh tế & Mức độ trả giá của NPC
Toàn bộ thông số kinh tế, giá cơ bản của nông sản, và hồ sơ tính cách của từng thương lái được quản lý thông qua file Asset ScriptableObject:
- **Đường dẫn**: `Assets/_Project/ScriptableObjects/Bargaining/BargainingEconomyConfig.asset`
- **Cách chỉnh (không cần code)**: Click chọn file này trong Inspector của Unity Editor để điều chỉnh:
  - `Stamina Cost Per Negotiation`: Số thể lực tiêu tốn cho mỗi lượt nâng/hạ giá.
  - `Offer Step`: Bước nhảy giá tiền mỗi lần bấm nút tăng/giảm giá.
  - `Agricultural Items`: Danh sách nông sản, giá gốc và khoảng biến động giá thị trường.
  - `Npc Profiles`: Danh sách thương lái, gồm Avatar, `openingPriceMultiplier` (hệ số mở giá đầu ngày) và `maxAcceptPriceMultiplier` (ngưỡng chấp nhận giá tối đa của NPC).

### 3. Thay đổi Lời thoại NPC (Dialogue text)
Nội dung lời thoại của NPC và người chơi tương ứng với từng trạng thái đối thoại được quy định trong **[FullSimulatorUI.cs](file:///d:/Ky7/pru/assignment/new/cho-noi-mien-tay/Assets/_Project/Scripts/UI/FullSimulatorUI.cs)** tại hàm `UpdateDialogueUI()`:
- **Cách chỉnh**: Sửa nội dung các chuỗi text gán cho `dialogueText.text` nằm trong khối `switch (dialogueState)` của từng trường hợp (VD: `DialogueState.MerchantGreeting`, `DialogueState.VendorGreeting`).

---

## Chạy Tests & Build Cảnh

### Chạy Unit Tests từ Terminal
Tất cả các bài kiểm tra toán học và công thức vật lý ghe có thể được chạy tự động qua terminal:
```bash
./run-tests.sh
```

### Build cảnh tự động trong Unity
Để build lại toàn bộ scene từ mã nguồn sạch:
1. Mở Unity Editor.
2. Click chọn menu **ChoNoi > Scenes > Build Full UI Simulator Scene**.
3. Cảnh `RiverMarketScene.unity` mới nhất sẽ tự động được sinh ra trong thư mục `Assets/_Project/Scenes/Core/` với đầy đủ liên kết thực thể.
