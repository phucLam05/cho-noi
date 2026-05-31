# Chợ Nổi Miền Tây - Unity Project

Dự án phát triển ứng dụng/game Unity dành cho đội ngũ gồm 4 thành viên. 

## Cấu trúc thư mục (Architecture)

Tất cả các file, script, và asset do team tự phát triển sẽ được đặt hoàn toàn trong thư mục `Assets/_Project`. 
Các package hoặc tài nguyên của bên thứ 3 (third-party) sẽ nằm ngoài thư mục này (ví dụ: `Plugins`, `Assets/ImportedAssets`) để tránh nhầm lẫn và dễ quản lý.

```text
Assets/
├── _Project/
│   ├── Animations/       # Chứa các Animation Clips, Animator Controllers
│   ├── Art/              # Chứa 2D Sprites, 3D Models, Materials, Textures
│   ├── Audio/            # Chứa âm thanh (Music, SFX)
│   ├── Prefabs/          # Chứa các GameObject đã được configure sẵn
│   ├── Scenes/           
│   │   ├── Core/         # Các Scene cốt lõi của game (Boot, MainMenu, GameLevel...)
│   │   └── Sandbox/      # Các test scene cá nhân (Mỗi người dùng 1 scene riêng để test)
│   ├── Scripts/          # Mã nguồn C#
│   │   ├── Core/         # Game Manager, Audio Manager...
│   │   ├── Gameplay/     # Logic chính của game, Player Controller, Enemy AI...
│   │   ├── UI/           # Logic xử lý giao diện
│   │   └── Utils/        # Các hàm hỗ trợ dùng chung (Helper, Extension methods)
│   ├── ScriptableObjects/# Dữ liệu thiết lập (Data configuration)
│   └── UI/               # Asset cho giao diện (Icons, Fonts, Panels...)
```

## Quy tắc làm việc nhóm (Workflow)

Để tránh xung đột (Merge Conflict) trong Unity khi nhiều người cùng làm việc, toàn bộ team tuân thủ các quy tắc sau:

### 1. KHÔNG BAO GIỜ làm việc chung trên cùng một Scene
- File Scene (`.unity`) của Unity rất dễ bị lỗi conflict khi merge bằng Git.
- Mỗi thành viên **tự tạo một Scene riêng** trong thư mục `_Project/Scenes/Sandbox/` (ví dụ: `Tuan_TestScene.unity`, `Minh_TestScene.unity`) để làm việc và test các tính năng.
- Khi hoàn thành một tính năng (VD: Nhân vật di chuyển), hãy đóng gói tính năng đó thành một **Prefab** (lưu vào thư mục `_Project/Prefabs/`).
- Người được phân công chịu trách nhiệm "ráp level" sẽ kéo Prefab đó vào Scene chính trong `_Project/Scenes/Core/`.

### 2. Tách nhỏ Prefab
- Tránh tạo một Prefab khổng lồ chứa toàn bộ game.
- Nếu bạn thiết kế UI, hãy tách từng màn hình thành một Prefab riêng lẻ (Ví dụ: `MainMenuPanel.prefab`, `SettingsPanel.prefab`). Điều này cho phép nhiều người có thể chỉnh sửa các phần UI khác nhau cùng lúc.

### 3. Tách Data ra khỏi Logic
- Không được gõ "chết" (hardcode) các thông số như Máu, Sát thương, Tốc độ... thẳng vào code (Scripts).
- Khuyến khích sử dụng **ScriptableObject** để cấu hình dữ liệu. Designer sẽ có thể thay đổi dữ liệu một cách linh hoạt mà không cần lập trình viên can thiệp vào code.

### 4. Quy tắc quản lý phiên bản (Git)
- Đảm bảo đã thiết lập `.gitignore` cho Unity (thư mục `Library`, `Temp`, `Logs`, `obj`, v.v. KHÔNG bao giờ được đưa lên Git).
- Trước khi push code, luôn luôn phải test xem tính năng của mình có gây lỗi (compile error) cho project không.
- Nếu xảy ra xung đột ở các file `.prefab` hoặc `.unity` mà không thể tự giải quyết, hãy thông báo ngay với team để tìm cách xử lý. KHÔNG cố tình ghi đè nếu bạn không chắc chắn.

## Hướng dẫn cài đặt
1. Cài đặt Unity Hub và phiên bản Unity Editor phù hợp với dự án.
2. Clone repository về máy: `git clone <repo_url>`
3. Mở Unity Hub, chọn **Add project from disk** và trỏ tới thư mục dự án vừa clone.
4. Chờ Unity import các tài nguyên lần đầu tiên (sẽ hơi lâu).
5. Mở file `Assets/_Project/Scenes/Sandbox/SampleScene.unity` để kiểm tra.
