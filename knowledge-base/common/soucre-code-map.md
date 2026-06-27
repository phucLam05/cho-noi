📁 .vscode
📁 Assets
 ├── 📁 Scenes                               <-- (Giữ nguyên của Unity)
 ├── 📁 Settings                             <-- (Giữ nguyên của Unity)
 ├── 📁 TutorialInfo                         <-- (Giữ nguyên của Unity)
 ├── 📄 InputSystem_Actions.inputactions     <-- (Giữ nguyên - Dùng cho New Input System)
 ├── 📄 Readme.asset                         <-- (Giữ nguyên)
 │
 └── 📁 _Project                             <-- [KHU VỰC LÀM VIỆC] Toàn bộ code dự án
     ├── 📁 Scripts
     │   ├── 📁 Domain                       <-- Logic thuần C#, không MonoBehaviour
     │   │   └── 📄 IBoatInput.cs            <-- Interface đầu vào ghe
     │   │
     │   ├── 📁 Application                  <-- (Trống - chờ Service trung gian)
     │   │
     │   ├── 📁 Infrastructure               <-- Data, ScriptableObjects
     │   │   └── 📄 BoatStats.cs             <-- ScriptableObject chỉ số vật lý ghe
     │   │
     │   ├── 📁 Presentation                 <-- MonoBehaviour, Input, Vật lý
     │   │   ├── 📄 BoatController.cs        <-- Điều khiển vật lý ghe (Rigidbody)
     │   │   └── 📄 PCBoatInput.cs           <-- Đọc input bàn phím/gamepad
     │   │
     │   └── 📁 Tests                        <-- Script debug & kiểm thử
     │       └── 📄 BoatControllerValidator.cs <-- Bộ test vật lý ghe (ContextMenu)
     │
     └── 📁 Prefabs                          <-- (Trống - chờ Prefab ghe)
📁 Packages
📁 ProjectSettings
📄 .gitattributes
📄 .gitignore
📄 cho-noi-mien-tay.slnx

---

# Mô tả chi tiết từng Script

## Domain

### `IBoatInput.cs`
- **Namespace:** `ChoNoi.Domain`
- **Loại:** Interface (C# thuần, không MonoBehaviour)
- **Chức năng:** Trừu tượng hóa nguồn đầu vào điều khiển ghe. Bất kỳ thiết bị nào (bàn phím, gamepad, AI) đều implement interface này để `BoatController` không phụ thuộc vào thiết bị cụ thể.
- **Properties:**
  - `float Throttle` — ga/thắng, khoảng [-1, 1] (âm = lùi, dương = tiến)
  - `float Steering` — lái ngang, khoảng [-1, 1] (âm = trái, dương = phải)
- **Được dùng bởi:** `BoatController`, `PCBoatInput`

---

## Infrastructure

### `BoatStats.cs`
- **Namespace:** `ChoNoi.Infrastructure`
- **Loại:** `ScriptableObject`
- **Tạo asset:** Menu `ChoNoi > Boat Stats` trong Unity Editor
- **Chức năng:** Lưu toàn bộ chỉ số vật lý của ghe theo nguyên tắc Data-Driven. Thay đổi cảm giác lái chỉ bằng cách chỉnh số trong Inspector, không cần sửa code.
- **Fields (SerializeField):**

  | Field | Mặc định | Đơn vị | Mô tả |
  |---|---|---|---|
  | `thrustForce` | 10 | m/s² | Lực đẩy tiến/lùi tác động lên Rigidbody |
  | `waterDrag` | 2 | — | Hệ số cản theo chiều vận tốc tổng (ma sát nước) |
  | `sidewaysDrag` | 5 | — | Hệ số cản trượt ngang (giữ ghe đúng hướng mũi) |
  | `turnTorque` | 3 | — | Mô-men xoắn lái, nhân thêm vận tốc để chỉ lái khi chạy |

- **Được dùng bởi:** `BoatController`, `BoatControllerValidator`

---

## Presentation

### `PCBoatInput.cs`
- **Namespace:** `ChoNoi.Presentation`
- **Loại:** `MonoBehaviour`, implement `IBoatInput`
- **Chức năng:** Đọc Action `Move` (Vector2) từ ActionMap `Player` trong file `InputSystem_Actions.inputactions` và cung cấp `Throttle` / `Steering` cho `BoatController`.
- **Setup trong Inspector:** Kéo file `InputSystem_Actions.inputactions` vào field `Input Actions`.
- **Lifecycle quan trọng:**
  - `Awake` — tìm ActionMap `Player` và Action `Move` từ asset
  - `OnEnable / OnDisable` — đăng ký/hủy callback để tránh memory leak
- **Phụ thuộc:** `UnityEngine.InputSystem`, `IBoatInput`

### `BoatController.cs`
- **Namespace:** `ChoNoi.Presentation`
- **Loại:** `MonoBehaviour`, `[RequireComponent(typeof(Rigidbody))]`
- **Chức năng:** Áp dụng 4 lực vật lý lên `Rigidbody` trong `FixedUpdate` dựa trên đầu vào từ `IBoatInput` và chỉ số từ `BoatStats`.
- **Setup trong Inspector:** Gắn `BoatStats.asset` vào field `Boat Stats`. `Rigidbody` và `IBoatInput` được lấy tự động qua `GetComponent`.
- **Các phương thức vật lý (gọi trong FixedUpdate):**

  | Phương thức | Công thức | Mô tả |
  |---|---|---|
  | `ApplyThrust` | `F = forward × throttle × thrustForce` | Lực đẩy tiến/lùi |
  | `ApplyWaterDrag` | `F = -velocity × waterDrag` | Cản nước ngược chiều vận tốc |
  | `ApplySidewaysResistance` | `F = -right × dot(v, right) × sidewaysDrag` | Triệt tiêu trượt ngang |
  | `ApplySteering` | `T = up × steering × turnTorque × |v|` | Mô-men lái (tỉ lệ vận tốc) |

- **Tất cả lực dùng:** `ForceMode.Acceleration` (bỏ qua khối lượng)
- **Phụ thuộc:** `IBoatInput`, `BoatStats`, `Rigidbody`

---

## Tests

### `BoatControllerValidator.cs`
- **Namespace:** `ChoNoi.Tests`
- **Loại:** `MonoBehaviour` (gắn vào cùng GameObject với ghe)
- **Chức năng:** Cung cấp bộ test vật lý qua `[ContextMenu]` (chuột phải Inspector).
- **Setup trong Inspector:** Kéo `BoatStats.asset` và `Rigidbody` của ghe vào các field tương ứng.
- **Các bài test:**

  | ContextMenu | Play mode? | Mô tả | Điều kiện PASS |
  |---|---|---|---|
  | `Test 1 - Kiem Tra BoatStats` | Không cần | Kiểm tra Stats không null, giá trị > 0 | Tất cả field > 0 |
  | `Test 2 - Luc Day Tien` | Bắt buộc | Áp lực 0.5s → đo vận tốc tiến | `forwardSpeed > minExpectedSpeed` |
  | `Test 3 - Mo Men Lai` | Bắt buộc | **3a:** Đứng im không quay; **3b:** Đang chạy phải quay | Δangle < 0.1° (3a), Δangle > 0.5° (3b) |
  | `Test 4 - Quan Tinh Nuoc` | Bắt buộc | Ngắt ga 1.5s → tốc độ phải giảm | `speedAfter < peakSpeed × 0.8` |
  | `>>> Chay Tat Ca Test <<<` | Bắt buộc | Chạy tuần tự Test 1 → 4 | Tất cả PASS |

- **Log:** Xanh = PASS, Đỏ = FAIL kèm gợi ý chỉnh `BoatStats`
