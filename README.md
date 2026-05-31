# Chợ Nổi Miền Tây

3D Simulation / Management game lấy bối cảnh chợ nổi miền Tây Nam Bộ Việt Nam.  
Xây dựng bằng **Unity 6 (6000.4.7f1)** theo kiến trúc **Clean Architecture**.

---

## Mục lục

- [Tổng quan game](#tổng-quan-game)
- [Tech Stack](#tech-stack)
- [Kiến trúc dự án](#kiến-trúc-dự-án)
- [Cấu trúc thư mục](#cấu-trúc-thư-mục)
- [Mô tả Source Code](#mô-tả-source-code)
- [Hệ thống vật lý ghe](#hệ-thống-vật-lý-ghe)
- [Chạy Tests](#chạy-tests)
- [Setup trong Unity Editor](#setup-trong-unity-editor)
- [Quy tắc đóng góp code](#quy-tắc-đóng-góp-code)

---

## Tổng quan game

| Thời điểm | Hoạt động |
|---|---|
| 3AM – 10AM (Bình Minh) | Bán hàng tại Cây Bẹo, mặc cả với thương lái |
| 10AM – 4PM (Ban ngày) | Thu mua nông sản tại các kênh rạch |
| Chiều tối | Nâng cấp ghe, bảo trì tại bến |

**Mục tiêu Phase 1:** Hoàn thiện hệ thống di chuyển ghe (Boat Physics & Controller).

---

## Tech Stack

| Thành phần | Chi tiết |
|---|---|
| Engine | Unity 6000.4.7f1 |
| Ngôn ngữ | C# (.NET Standard 2.1) |
| Input System | Unity New Input System (`InputSystem_Actions.inputactions`) |
| Physics | `Rigidbody` + `FixedUpdate` — `ForceMode.Acceleration` |
| Dữ liệu | `ScriptableObject` (Data-Driven, không hard-code) |
| Kiến trúc | Clean Architecture (Domain / Application / Infrastructure / Presentation) |

---

## Kiến trúc dự án

```
┌─────────────────────────────────────┐
│           Presentation              │  MonoBehaviour, Input, Vật lý, UI
│   BoatController  │  PCBoatInput    │
└────────────┬──────────────┬─────────┘
             │ implement    │ use
             ▼              ▼
┌─────────────────────────────────────┐
│              Domain                 │  Logic thuần C#, không MonoBehaviour
│           IBoatInput                │
└─────────────────────────────────────┘
             ▲
             │ data
┌─────────────────────────────────────┐
│           Infrastructure            │  ScriptableObjects, cấu hình
│            BoatStats                │
└─────────────────────────────────────┘
```

**Nguyên tắc cốt lõi:**
- **Dependency Inversion** — `BoatController` phụ thuộc `IBoatInput` (interface), không phụ thuộc `PCBoatInput` (implementation)
- **Single Responsibility** — mỗi class chỉ giải quyết 1 nhiệm vụ
- **Data-Driven** — toàn bộ chỉ số nằm trong `BoatStats.asset`, chỉnh trong Inspector không cần sửa code

---

## Cấu trúc thư mục

```
Assets/
├── InputSystem_Actions.inputactions   # Định nghĩa input (giữ nguyên)
├── Scenes/                            # Scene Unity (giữ nguyên)
├── Settings/                          # Cài đặt project (giữ nguyên)
│
└── _Project/                          # Toàn bộ code dự án
    ├── Scripts/
    │   ├── Domain/
    │   │   └── IBoatInput.cs
    │   ├── Application/               # (chờ Service trung gian — Phase 2)
    │   ├── Infrastructure/
    │   │   └── BoatStats.cs
    │   ├── Presentation/
    │   │   ├── BoatController.cs
    │   │   └── PCBoatInput.cs
    │   ├── Editor/
    │   │   └── BoatTestRunner.cs      # Chạy test từ terminal
    │   └── Tests/
    │       └── BoatControllerValidator.cs
    └── Prefabs/                       # (chờ Prefab ghe — Phase 2)

knowledge-base/                        # Tài liệu nội bộ dự án
run-tests.sh                           # Script chạy test từ terminal
```

---

## Mô tả Source Code

### `Domain/IBoatInput.cs`
**Namespace:** `ChoNoi.Domain`

Interface trừu tượng hóa nguồn đầu vào điều khiển ghe. Mọi thiết bị (bàn phím, gamepad, AI) đều implement interface này để `BoatController` không bị ràng buộc với thiết bị cụ thể.

```csharp
public interface IBoatInput
{
    float Throttle { get; }   // [-1, 1] — âm: lùi, dương: tiến
    float Steering { get; }   // [-1, 1] — âm: trái, dương: phải
}
```

---

### `Infrastructure/BoatStats.cs`
**Namespace:** `ChoNoi.Infrastructure` | **Loại:** `ScriptableObject`  
**Tạo asset:** Menu Unity `ChoNoi > Boat Stats`

Lưu toàn bộ chỉ số vật lý của ghe. Thay đổi cảm giác lái bằng cách chỉnh số trong Inspector.

| Field | Mặc định | Mô tả |
|---|---|---|
| `thrustForce` | `10` | Lực đẩy tiến/lùi (m/s²) |
| `waterDrag` | `2` | Hệ số cản nước theo chiều vận tốc |
| `sidewaysDrag` | `5` | Hệ số cản trượt ngang — giữ ghe đúng hướng mũi |
| `turnTorque` | `3` | Mô-men xoắn lái, nhân vận tốc để chỉ lái khi đang chạy |

---

### `Presentation/PCBoatInput.cs`
**Namespace:** `ChoNoi.Presentation` | **Implement:** `IBoatInput`

Đọc Action `Move` (Vector2) từ ActionMap `Player` trong `InputSystem_Actions.inputactions` và cung cấp `Throttle` / `Steering` cho `BoatController`.

**Setup Inspector:** Kéo file `InputSystem_Actions.inputactions` vào field **Input Actions**.

---

### `Presentation/BoatController.cs`
**Namespace:** `ChoNoi.Presentation` | `[RequireComponent(typeof(Rigidbody))]`

Áp dụng 4 lực vật lý lên `Rigidbody` trong `FixedUpdate`:

| Phương thức | Công thức | Mục đích |
|---|---|---|
| `ApplyThrust` | `F = forward × throttle × thrustForce` | Lực đẩy tiến / lùi |
| `ApplyWaterDrag` | `F = −velocity × waterDrag` | Cản nước — ghe không trôi mãi |
| `ApplySidewaysResistance` | `F = −(right · v) × right × sidewaysDrag` | Chống trượt ngang |
| `ApplySteering` | `T = up × steering × turnTorque × │v│` | Lái — chỉ có tác dụng khi đang chạy |

**Setup Inspector:** Gắn `BoatStats.asset` vào field **Boat Stats**. `Rigidbody` và `IBoatInput` được lấy tự động qua `GetComponent`.

---

### `Tests/BoatControllerValidator.cs`
**Namespace:** `ChoNoi.Tests` | Gắn vào cùng GameObject với ghe

Bộ test vật lý chạy bằng cách chuột phải vào script trong Inspector (`[ContextMenu]`):

| ContextMenu | Cần Play? | Kiểm tra gì |
|---|---|---|
| `Test 1 - Kiem Tra BoatStats` | Không | Stats không null, tất cả giá trị > 0 |
| `Test 2 - Luc Day Tien` | Có | Áp lực 0.5s → vận tốc tiến phải > ngưỡng tối thiểu |
| `Test 3 - Mo Men Lai` | Có | Đứng im không quay; đang chạy phải quay |
| `Test 4 - Quan Tinh Nuoc` | Có | Ngắt ga 1.5s → tốc độ giảm < 80% tốc độ đỉnh |
| `>>> Chay Tat Ca Test <<<` | Có | Chạy tuần tự Test 1 → 4 |

---

### `Editor/BoatTestRunner.cs`
**Namespace:** `ChoNoi.Editor` | Chạy từ terminal hoặc menu `ChoNoi > Run All Tests`

22 unit test kiểm tra toán học công thức vật lý, không cần Play mode:

- **6 test** — BoatStats validation (null check, giá trị dương)
- **12 test** — Xác minh 4 công thức vật lý (thrust, drag, sideways, steering)
- **4 test** — IBoatInput contract (Throttle/Steering ∈ [-1, 1])

---

## Hệ thống vật lý ghe

Toàn bộ lực dùng `ForceMode.Acceleration` (bỏ qua khối lượng Rigidbody):

```
Mỗi FixedUpdate:

  F_thrust    = transform.forward  × throttle × thrustForce
  F_waterDrag = −rb.linearVelocity × waterDrag
  F_sideways  = −(rb.linearVelocity · transform.right) × transform.right × sidewaysDrag
  T_steer     =  transform.up      × steering × turnTorque × |rb.linearVelocity|
```

**Tại sao nhân `|velocity|` cho steering?**  
Ghe thật trên sông không thể bẻ lái khi đứng yên — bánh lái chỉ có tác dụng khi nước chảy qua. Nhân mô-men lái với độ lớn vận tốc mô phỏng đúng hành vi này.

---

## Chạy Tests

Chạy từ terminal (không cần mở Unity Editor):

```bash
./run-tests.sh
```

Kết quả mẫu:
```
==================================================
  CHO NOI MIEN TAY — Boat Physics Test Runner
==================================================
--- 1. KIEM TRA BOAT STATS (Unit Test) ---
  ✓  [PASS] BoatStats tao duoc instance (khong null)
  ✓  [PASS] ThrustForce phai > 0
  ...
--- 2. KIEM TRA CONG THUC VAT LY ---
  ✓  [PASS] Thrust (throttle=1): luc dam tien dung chieu forward
  ...
  RESULT: ALL 22 TESTS PASSED
==================================================
```

Exit code `0` = tất cả pass, `1` = có test fail (dùng được trong CI/CD).

---

## Setup trong Unity Editor

1. Mở project bằng **Unity 6000.4.7f1**
2. Tạo `BoatStats` asset: menu `ChoNoi > Boat Stats`, lưu vào `Assets/_Project/`
3. Tạo GameObject ghe, thêm các component theo thứ tự:
   - `Rigidbody`
   - `PCBoatInput` → kéo `InputSystem_Actions.inputactions` vào field **Input Actions**
   - `BoatController` → kéo `BoatStats.asset` vào field **Boat Stats**
4. (Tuỳ chọn) Thêm `BoatControllerValidator` để test trong Play mode

---

## Quy tắc đóng góp code

- **Namespace:** `ChoNoi.[Layer].[SubFolder]`
- **Naming:** Classes `PascalCase`, variables/methods `camelCase`
- **Mọi class mới** phải có header comment theo template trong `knowledge-base/common/csharp-unity-doc-template.md`
- **Chỉ số vật lý** phải nằm trong `BoatStats` (ScriptableObject), không hard-code
- **Code mới** chỉ được đặt trong `Assets/_Project/` — không sửa `Scenes/`, `Settings/`, `TutorialInfo/`
- **Tham chiếu private** dùng `[SerializeField]` thay vì `public`
