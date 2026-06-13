# Phase 02 (TV1 – Core) — Implementation Report

> Báo cáo toàn bộ kế hoạch & hành động đã thực hiện cho nhiệm vụ Phase 2 của TV1 (Core Programmer):
> Time System + Weight Penalty Physics. Ngày thực hiện: 2026-06-06.

---

## 1. Mục tiêu (theo `phase-02-tv1-execution-plan.md`)
1. Xây dựng `TimeManager` quản lý đồng hồ in-game + event `OnPhaseChanged` / `OnTimeChanged`.
2. Cập nhật `BoatController` lấy tải trọng qua `IWeightProvider` và áp dụng *Performance Multiplier*
   (theo `time-physics-rules.md`).
3. Viết script mô phỏng ghe nặng lên (`WeightSimulator`) + script test theo `phase-02-tv1-test-plan.md`.
4. Script `.sh` kiểm tra build/compile + xuất báo cáo này.

## 2. Quyết định kiến trúc đã chốt
- **Namespace** mọi file mới = `ChoNoi.*` (đúng rule `architecture-guidelines.md`, khớp code ghe Phase 1).
- **Decouple weight**: `BoatController` chỉ phụ thuộc interface `IWeightProvider`, không biết
  `InventoryManager`. Nguồn cấp dữ liệu khi test = `WeightSimulator` (script test).
- **KHÔNG sửa** `InventoryManager.cs` của teammate (tránh merge conflict — theo Workflow rule trong README).
  → Tích hợp thật sau này chỉ cần thêm `: IWeightProvider` cho `InventoryManager`
  (xem mục 7).
- Project **không có `.asmdef`** → tất cả nằm chung `Assembly-CSharp`, namespace cross-reference tự do.

---

## 3. Danh sách file TẠO MỚI

| File | Layer / Namespace | Vai trò |
|---|---|---|
| `Assets/_Project/Scripts/Domain/GamePhase.cs` | `ChoNoi.Domain` | enum `{ Dawn, Day, Dusk, Night }` |
| `Assets/_Project/Scripts/Domain/ITimeSystem.cs` | `ChoNoi.Domain` | Interface: `OnTimeChanged`, `OnPhaseChanged`, `CurrentPhase` |
| `Assets/_Project/Scripts/Domain/IWeightProvider.cs` | `ChoNoi.Domain` | Interface: `float GetCurrentWeightRatio()` |
| `Assets/_Project/Scripts/Application/TimeManager.cs` | `ChoNoi.Application` | MonoBehaviour đồng hồ game, implement `ITimeSystem` |
| `Assets/_Project/Scripts/Tests/TimeLogger.cs` | `ChoNoi.Tests` | Test 1: log mỗi khi đổi Phase |
| `Assets/_Project/Scripts/Tests/WeightSimulator.cs` | `ChoNoi.Tests` | Test 2 + mô phỏng tải: OnGUI slider, implement `IWeightProvider` |
| `build-check.sh` (project root) | — | Kiểm tra compile + chạy unit test qua Unity batchmode |

## 4. Danh sách file CẬP NHẬT

| File | Thay đổi |
|---|---|
| `Infrastructure/BoatStats.cs` | Thêm field `maxPenaltyFactor` (Range 0–1, mặc định `0.4`) + property `MaxPenaltyFactor` |
| `Presentation/BoatController.cs` | Lấy `IWeightProvider` (null-safe); tính `performance` & `dragMultiplier` trong `FixedUpdate`; truyền vào `ApplyThrust`/`ApplySteering`/`ApplyWaterDrag` |
| `Editor/BoatTestRunner.cs` | Thêm nhóm test 4 `RunWeightPenaltyTests()` (10 assert toán học Weight Penalty) |

---

## 5. Công thức Weight Penalty đã áp dụng (`time-physics-rules.md`)

```
WeightRatio  = Clamp01(CurrentWeight / MaxCapacity)          // [0,1], 1 = đầy tải
Performance  = 1 - WeightRatio * MaxPenaltyFactor            // MaxPenaltyFactor ∈ BoatStats
DragMul      = 1 + WeightRatio * 0.5                          // chở nặng → cản nước tăng nhẹ

ActualThrust = forward * throttle * thrustForce * Performance
ActualTorque = up      * steering * turnTorque  * |v| * Performance
ActualDrag   = -velocity * waterDrag * DragMul
```

Với `MaxPenaltyFactor = 0.4`: ghe trống → 100% hiệu suất; ghe đầy → 60% hiệu suất + cản nước +50%.
`ApplySidewaysResistance` giữ nguyên (không phụ thuộc tải).

## 6. Map Phase ↔ Giờ (`TimeManager.GetPhaseForHour`)

| GamePhase | Khung giờ | Ý nghĩa |
|---|---|---|
| `Dawn`  | 03:00 – 09:59 | Bình Minh Giao Thương |
| `Day`   | 10:00 – 12:59 | Ban Ngày Thu Mua |
| `Dusk`  | 13:00 – 17:59 | Chiều Tà Thu Mua |
| `Night` | 18:00 – 02:59 | Tối, Bảo Trì Tại Bến |

`timeScale` = số **phút in-game / 1 giây thực** (designer chỉnh trong Inspector). `startHour` mặc định `3`.

---

## 7. Hướng dẫn Setup trong Unity Editor

### Test 1 — Time & Phase
1. Tạo GameObject `__TimeManager`, add component `TimeManager`, đặt `timeScale = 60` (1s thực = 1h game).
2. Add component `TimeLogger`, kéo `__TimeManager` vào field **Time Manager**.
3. Nhấn **Play** → Console log các mốc Phase: Bình Minh (03:00) → Ban Ngày (10:00) → Chiều Tà (13:00) → Tối (18:00).

### Test 2 — Weight Impact
1. Trên GameObject ghe: `Rigidbody` + `PCBoatInput` + `BoatController` + `WeightSimulator` (cùng 1 object).
2. Kéo `BoatStats.asset` vào field **Boat Stats** của cả `BoatController` và `WeightSimulator`.
3. Nhấn **Play** → kéo Slider **MO PHONG TAI TRONG**:
   - Weight = 0 → "Toc do: 100%, Be lai: 100%", ghe nhạy.
   - Weight = Max → "Toc do: 60%, Be lai: 60%", ghe lỳ rõ rệt.

### Tích hợp thật (sau này, không nằm trong nhiệm vụ này)
Cho `InventoryManager` implement interface, ratio = `CurrentTotalWeight / MaxWeightCapacity`:
```csharp
public class InventoryManager : MonoBehaviour, ChoNoi.Domain.IWeightProvider
{
    public float GetCurrentWeightRatio()
        => maxWeightCapacity <= 0f ? 0f : Mathf.Clamp01(currentTotalWeight / maxWeightCapacity);
}
```
Đặt `InventoryManager` cùng GameObject ghe (hoặc cho `BoatController` tham chiếu) là xong.

---

## 8. Kiểm thử tự động (build + unit test)

Chạy từ terminal (không cần mở Editor):
```bash
./build-check.sh
```
- Bước 1: mở Unity batchmode → compile toàn bộ script. Có `error CS...` → in lỗi, exit 1.
- Bước 2: chạy `ChoNoi.Editor.BoatTestRunner.Run` → in `[PASS]/[FAIL]` + tổng kết.
- Bao gồm nhóm test mới **"4. KIEM TRA CONG THUC WEIGHT PENALTY"** (10 assert):
  performance ratio=0→1.0, ratio=1→0.6, đơn điệu giảm, clamp, dragMul 1.0→1.5,
  ActualThrust/ActualTorque đầy tải < trống, `MaxPenaltyFactor ∈ [0,1)`.

> `run-tests.sh` (Phase 1) vẫn dùng được — giờ chạy luôn cả nhóm test Weight Penalty.

---

## 9. Toàn bộ hành động đã thực hiện (nhật ký)
1. Đọc `knowledge-base/common/*` + `README.md` để nắm kiến trúc, rule, namespace.
2. Đọc 3 tài liệu nhiệm vụ trong `task-phase-02-tv1-core/`.
3. Khảo sát source: `BoatController`, `BoatStats`, `IBoatInput`, `BoatTestRunner`,
   `BoatControllerValidator`, `InventoryManager`, `run-tests.sh`.
4. Lập plan, xác nhận 2 quyết định với user (namespace `ChoNoi.*`; weight = chỉ `WeightSimulator`).
5. Tạo 3 file Domain: `GamePhase`, `ITimeSystem`, `IWeightProvider`.
6. Tạo `Application/TimeManager.cs`.
7. Cập nhật `BoatStats.cs` (thêm `maxPenaltyFactor`).
8. Cập nhật `BoatController.cs` (Performance Multiplier + DragMul).
9. Tạo `Tests/TimeLogger.cs` và `Tests/WeightSimulator.cs`.
10. Mở rộng `Editor/BoatTestRunner.cs` với nhóm test Weight Penalty.
11. Tạo `build-check.sh` (+ `chmod +x`).
12. Viết báo cáo này.
13. Chạy `./build-check.sh` xác nhận compile sạch + test pass (xem mục 10).

## 10. Kết quả build-check
Đã chạy `./build-check.sh` với Unity 6000.4.7f1 (2026-06-06):

- **Compile:** ✓ Không có `error CS`.
- **Unit test:** ✓ **31/31 PASSED** (22 test Phase 1 + 9 test Weight Penalty mới).

> ⚠️ **Lưu ý kỹ thuật quan trọng (đã xử lý):** namespace mới `ChoNoi.Application`
> (cho `TimeManager`) **che mất** `UnityEngine.Application` trong mọi file thuộc cây
> `ChoNoi.*`. Lần build đầu lỗi `CS0234` tại `BoatControllerValidator` và `BoatTestRunner`
> (dùng `Application.isPlaying` / `Application.isBatchMode`). Đã sửa bằng cách qualify đầy
> đủ `UnityEngine.Application.*` tại 3 chỗ. **Quy ước cho team:** trong code namespace
> `ChoNoi.*`, luôn viết `UnityEngine.Application` (không viết tắt `Application`).

---

## 11. Cam kết không đụng vùng cấm
KHÔNG sửa: `Assets/Scenes/`, `Assets/Settings/`, `Assets/TutorialInfo/`,
`InputSystem_Actions.inputactions`, và `InventoryManager.cs` của teammate.
Toàn bộ code mới nằm trong `Assets/_Project/Scripts/` đúng phân lớp Clean Architecture.
