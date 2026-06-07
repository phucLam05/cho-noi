# Phase 03 (TV1 – Core) — Implementation Report

> Báo cáo plan & toàn bộ hành động cho nhiệm vụ Phase 3 của TV1 (Core Programmer):
> Dynamic Environment (Ánh sáng / Sương mù) + Tide System (Thủy triều) + Boat Grounding (Mắc cạn).
> Ngày thực hiện: 2026-06-06. Build: Unity 6000.4.7f1.

---

## 1. Mục tiêu (theo `phase-03-tv1-execution-plan.md`)
Đồng bộ `TimeManager` (Phase 2) với môi trường: Directional Light, `RenderSettings.fogDensity`,
mực nước (trục Y GameObject Nước) và xử lý ghe mắc cạn khi nước hạ.

## 2. Quyết định kiến trúc đã chốt với user
- **Scope** = 3 task + Test 1 **VÀ** Bước 5 (Boat Grounding / Test 2).
- **EnvironmentProfileSO** dùng **Gradient + AnimationCurve** theo trục 0..24h.
  → Lệch nhẹ mô tả "dữ liệu theo từng GamePhase" trong execution-plan, nhưng đáp ứng tốt hơn
  yêu cầu "chuyển tiếp cực kỳ mượt" của test-plan và là cách chuẩn cho hệ day/night.

## 3. Tuân thủ `environment-time-rules.md`
- ✅ **Không dùng `Update()`** cho môi trường. `EnvironmentController` nghe event `OnTimeChanged`,
  nội suy qua **Coroutine** và **tự tắt** khi transition xong (`transition = null`).
  (Buoyancy dùng `FixedUpdate` vì là physics — hợp lệ.)
- ✅ **Tide**: chỉ dịch trục Y của `waterTransform` (không đụng Mesh), Lerp giữa
  `minWaterHeight`/`maxWaterHeight`.
- ✅ **Decoupling**: `EnvironmentController` chỉ là Observer, nhận giờ thô từ event và tự map Profile.

---

## 4. Danh sách file TẠO MỚI

| File | Layer / Namespace | Vai trò |
|---|---|---|
| `Infrastructure/EnvironmentProfileSO.cs` | `ChoNoi.Infrastructure` | SO data-driven: Gradient màu sáng + AnimationCurve cường độ/fog/góc nắng/mực nước |
| `Presentation/Environment/EnvironmentController.cs` | `ChoNoi.Presentation.Environment` | Observer: nghe `OnTimeChanged`, Coroutine Lerp light/fog/water; slider `[Range(0,24)]` + `OnValidate` preview (Test 1) |
| `Presentation/BoatBuoyancy.cs` | `ChoNoi.Presentation` | Lực nổi (lò xo–giảm chấn) giữ ghe bám mặt nước; nước hạ → ghe lắng xuống đáy |
| `build-check-phase03.sh` (root) | — | Kiểm tra compile + chạy unit test qua Unity batchmode |

## 5. Danh sách file CẬP NHẬT

| File | Thay đổi |
|---|---|
| `Presentation/BoatController.cs` | Thêm grounding: `riverbedLayer`, `groundedThrustPenalty`, `groundedExtraDrag`, `isGrounded`; `OnCollisionStay/Exit`; gộp `groundMul` vào performance + lực ma sát đáy. Có property `IsGrounded`. |
| `Editor/BoatTestRunner.cs` | Thêm nhóm test 5 `RunEnvironmentTests()` (9 assert: Tide lerp, Profile, Grounding) |

---

## 6. Công thức đã áp dụng

### 6.1 Ánh sáng & Sương mù (mỗi mốc giờ, `t01 = giờ/24`)
```
light.color     = Gradient.Evaluate(t01)
light.intensity = lightIntensityCurve.Evaluate(t01)
light.rotation  = Euler(sunPitchCurve.Evaluate(t01), sunYaw, 0)   // rotateSun
RenderSettings.fog = true
RenderSettings.fogDensity = max(0, fogDensityCurve.Evaluate(t01))
```

### 6.2 Thủy triều (Tide)
```
waterY = Lerp(minWaterHeight, maxWaterHeight, waterLevelCurve.Evaluate(t01))
waterTransform.position.y = waterY
```

### 6.3 Lực nổi (BoatBuoyancy, FixedUpdate)
```
depth = (waterY - floatHeight) - boatY
if depth > 0:  F_up = depth * buoyancyStrength - v_y * damping   (Acceleration)
```

### 6.4 Mắc cạn (BoatController)
```
isGrounded = chạm collider thuộc riverbedLayer (OnCollisionStay), clear khi OnCollisionExit
groundMul  = isGrounded ? groundedThrustPenalty(0.05) : 1.0
performance *= groundMul                      // giảm thrust + torque
if isGrounded: AddForce(-velocity * groundedExtraDrag)   // ma sát đáy
```
> Khi KHÔNG mắc cạn `groundMul = 1` → hành vi Phase 1/2 không đổi (test cũ vẫn pass).

---

## 7. Chuyển tiếp mượt (Coroutine, không Update)
- `OnTimeChanged(hour, minute)` → `targetHour = hour + minute/60`.
- Nếu chưa có coroutine → start `TransitionRoutine`: mỗi frame dịch `displayedHour` về `targetHour`
  theo đường ngắn nhất trên vòng 24h (qua `Mathf.DeltaAngle` quy về độ, xử lý wrap 23h→0h),
  `ApplyEnvironment`, tới nơi thì `transition = null` (tắt, hết tốn CPU).

---

## 8. Hướng dẫn Setup trong Unity Editor

### Tạo Profile
1. Menu `ChoNoi > Environment Profile` → lưu `EnvironmentProfile.asset` vào `Assets/_Project/`.
2. Chỉnh Gradient/Curve: ~5h sáng đèn mờ ám xanh-cam + fog dày; ~12h trưa đèn sáng gắt + fog ≈ 0.
   Water: `maxWaterHeight` (sáng, cao) > `minWaterHeight` (chiều, thấp), `waterLevelCurve` cao buổi sáng, thấp lúc 13–15h.

### Test 1 — Time-lapse Visuals
1. GameObject `__Environment` + `EnvironmentController`. Gán: `TimeManager`, `EnvironmentProfile`,
   `Directional Light`, `Water Transform`.
2. **Không cần Play**: kéo slider **Editor Preview Hour (0–24)** → thấy đèn/fog/mực nước đổi tức thì.
3. Play (TimeManager timeScale cao) → chuyển tiếp mượt theo đồng hồ, không giật.

### Test 2 — Mắc Cạn (Grounding Physics)
1. GameObject Nước: Mesh + `BoxCollider` (cùng Transform mà EnvironmentController dịch Y).
2. GameObject đáy sông/Terrain: Collider, đặt **Layer = Riverbed** (tạo layer mới).
3. Ghe: `Rigidbody` (**bật Use Gravity**) + Collider + `BoatController` (set `Riverbed Layer = Riverbed`)
   + `PCBoatInput` + `BoatBuoyancy` (gán Water Transform).
4. Đặt ghe ở kênh nông (terrain nhô), kéo thời gian → 15h: nước hạ → ghe rơi chạm đáy → nhấn ga ghe
   lỳ/không chạy do `groundedThrustPenalty` + `groundedExtraDrag`.

> ⚠️ Test 2 phụ thuộc scene (Terrain/Water/Layer) nên kiểm chứng thủ công trong Editor;
> `.sh` chỉ verify compile + công thức (math).

---

## 9. Kiểm thử tự động

```bash
./build-check-phase03.sh
```
- **Compile:** ✓ Không có `error CS`.
- **Unit test:** ✓ **40/40 PASSED** (31 test Phase 1+2 + 9 test Phase 3).
- Nhóm 5 "KIEM TRA MOI TRUONG & THUY TRIEU": Tide lerp (factor 0/1/so sánh), Profile
  (Max>Min, waterY trong [min,max], fog ≥ 0), Grounding (penalty ∈ [0,1), mắc cạn < nổi,
  không mắc cạn giữ nguyên lực).

---

## 10. Toàn bộ hành động đã thực hiện (nhật ký)
1. Đọc `knowledge-base/common/*` + `README.md` + 3 tài liệu Phase 3.
2. Khảo sát code Phase 2: `TimeManager`, `ITimeSystem`, `GamePhase`, `BoatController`, `BoatTestRunner`.
3. Lập plan, xác nhận 2 quyết định với user (scope gồm Bước 5; Profile = Gradient+AnimationCurve).
4. Tạo `EnvironmentProfileSO.cs` (Bước 1).
5. Tạo `EnvironmentController.cs` (Bước 2 + slider Test 1, Coroutine, không Update).
6. Tạo `BoatBuoyancy.cs` (Bước 5 — nổi/chìm theo nước).
7. Cập nhật `BoatController.cs` (Bước 5 — grounding detection + penalty + ma sát đáy).
8. Mở rộng `BoatTestRunner.cs` với nhóm test Environment & Tide.
9. Tạo `build-check-phase03.sh` (+ `chmod +x`).
10. Chạy `./build-check-phase03.sh` → compile sạch, 40/40 PASS.
11. Viết báo cáo này.

## 11. Cam kết không đụng vùng cấm
KHÔNG sửa: `Assets/Scenes/`, `Assets/Settings/`, `Assets/TutorialInfo/`,
`InputSystem_Actions.inputactions`, `InventoryManager.cs`. Code mới nằm trong
`Assets/_Project/Scripts/` đúng phân lớp Clean Architecture (`ChoNoi.*`).
