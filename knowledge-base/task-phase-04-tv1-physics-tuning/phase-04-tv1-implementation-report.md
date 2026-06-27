# Phase 04 (TV1 – Core) — Implementation Report

> Báo cáo plan & toàn bộ hành động cho nhiệm vụ Phase 4 của TV1 (Core Programmer):
> Physics Tuning & Anti-Bug — Durability Velocity Clamp + chống xuyên tường/lật ghe.
> Ngày thực hiện: 2026-06-06. Build: Unity 6000.4.7f1.

---

## 1. Mục tiêu (theo `phase-04-tv1-execution-plan.md`)
1. Tích hợp Độ bền (Durability) → **giới hạn trần vận tốc** khi ghe hỏng (KHÔNG giảm thrustForce).
2. Sửa lỗi xuyên tường (tunneling) + chống ghe văng/lật khi va chạm.

## 2. Quyết định kiến trúc đã chốt với user
- **Clamp Full 3D** đúng nguyên văn rule (`rb.linearVelocity.magnitude`).
  - *Caveat đã chấp nhận:* khi độ bền thấp, trần tốc độ thấp sẽ ghìm cả tốc độ rơi Y (buoyancy/thủy
    triều Phase 3). `baseMaxSpeed` mặc định 10 đủ cao nên ít ảnh hưởng lúc độ bền cao.
- **PhysicMaterial**: tạo qua **Editor helper menu** (không làm thủ công).
- Unity 6 đã đổi tên `PhysicMaterial` → **`PhysicsMaterial`** → code dùng tên mới (đã verify compile).

## 3. Tuân thủ `physics-tuning-rules.md`
- ✅ Velocity clamp: `currentMaxSpeed = baseMaxSpeed * Mathf.Clamp(ratio, 0.3, 1.0)`; vượt trần →
  `velocity = velocity.normalized * currentMaxSpeed`. **Không** giảm `thrustForce`.
- ✅ `rb.collisionDetectionMode = CollisionDetectionMode.Continuous` (Awake).
- ✅ `rb.constraints = FreezeRotationX | FreezeRotationZ` (Awake) — chỉ xoay quanh Y.
- ✅ PhysicsMaterial Bounciness = 0 (helper) → đâm bờ trượt dọc, không nảy.

---

## 4. Danh sách file TẠO MỚI

| File | Layer / Namespace | Vai trò |
|---|---|---|
| `Domain/IDurabilityProvider.cs` | `ChoNoi.Domain` | Interface `float GetDurabilityRatio()` — decouple như `IWeightProvider` |
| `Tests/DurabilitySimulator.cs` | `ChoNoi.Tests` | Implement `IDurabilityProvider`; Slider 0–100 + HUD vận tốc/trần + nút "LAO TOI" (ram x10) |
| `Editor/BoatPhysicMaterialCreator.cs` | `ChoNoi.Editor` | Menu `ChoNoi > Create Boat PhysicMaterial` tạo asset Bounciness=0 |
| `build-check-phase04.sh` (root) | — | Compile + chạy unit test qua Unity batchmode |

## 5. Danh sách file CẬP NHẬT

| File | Thay đổi |
|---|---|
| `Infrastructure/BoatStats.cs` | Thêm `baseMaxSpeed` (mặc định 10) + property `BaseMaxSpeed` |
| `Presentation/BoatController.cs` | Field `durabilityProvider`; `Awake` set Continuous + freeze rotation X/Z; method `ClampVelocityByDurability()` gọi cuối `FixedUpdate` |
| `Editor/BoatTestRunner.cs` | Thêm nhóm test 6 `RunDurabilityTests()` (9 assert) |

---

## 6. Công thức & vị trí code

### 6.1 Velocity Clamp (BoatController, cuối FixedUpdate)
```csharp
float durabilityRatio = durabilityProvider != null ? durabilityProvider.GetDurabilityRatio() : 1f;
float currentMaxSpeed = boatStats.BaseMaxSpeed * Mathf.Clamp(durabilityRatio, 0.3f, 1f);
if (rb.linearVelocity.magnitude > currentMaxSpeed)
    rb.linearVelocity = rb.linearVelocity.normalized * currentMaxSpeed;
```
- ratio=1 → trần = baseMaxSpeed (10). ratio=0.1 → trần = 30% = 3 m/s (khớp Test 1).
- Sàn 30% giữ ghe không kẹt chết khi độ bền = 0.

### 6.2 Anti-bug config (BoatController.Awake)
```csharp
rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
```

### 6.3 PhysicsMaterial (Editor helper)
`PhysicsMaterial` Bounciness=0, friction 0.2, `frictionCombine`/`bounceCombine = Minimum`.

> Độ bền hoàn toàn **độc lập** với thrustForce/weight/grounding — chỉ thêm 1 bước clamp cuối FixedUpdate.
> Khi không gắn `IDurabilityProvider`, ratio = 1.0 → trần = baseMaxSpeed (hành vi Phase 1/2/3 giữ nguyên).

---

## 7. Hướng dẫn Setup trong Unity Editor

### Test 1 — Durability Speed Limit
1. Trên GameObject ghe: `Rigidbody` + `PCBoatInput` + `BoatController` + `DurabilitySimulator`
   (kéo Rigidbody + BoatStats vào `DurabilitySimulator`).
2. Play → kéo Slider **Durability**:
   - = 100: ghe đạt trần ~`baseMaxSpeed`.
   - = 10: dù giữ ga, vận tốc trần khóa ~3 m/s; gia tốc khởi hành vẫn mạnh (thrust không đổi).
3. HUD hiển thị `Van toc hien tai` + `Tran toc do (max)` real-time; Console cũng log.

### Test 2 — High-Speed Wall Ramming
1. Menu `ChoNoi > Create Boat PhysicMaterial` → asset `Assets/_Project/BoatPhysicsMaterial.physicMaterial`.
2. Gán material đó vào Collider của **ghe** và **tường**.
3. Tường: BoxCollider mỏng (dày 0.1), **KHÔNG gắn Rigidbody** (vật cản tĩnh).
4. Play → bấm nút **"LAO TOI (Ram x10)"** → ghe lao vào tường:
   - Bị chặn hoàn toàn, **không xuyên qua** (nhờ `CollisionDetectionMode.Continuous`).
   - Đâm xiên thì trượt dọc, không nảy ngược mạnh (Bounciness=0), không lật úp (freeze rotation X/Z).

---

## 8. Kiểm thử tự động
```bash
./build-check-phase04.sh
```
- **Compile:** ✓ Không có `error CS` (đã xác nhận API Unity 6 `PhysicsMaterial`/`PhysicsMaterialCombine`).
- **Unit test:** ✓ **49/49 PASSED** (40 test Phase 1–3 + 9 test Durability).
- Nhóm 6 "DO BEN & GIOI HAN VAN TOC": trần theo ratio (1.0/0.0/0.1/0.5), sàn 30%, đơn điệu,
  clamp vượt/dưới trần, thrust độc lập với trần tốc độ.

---

## 9. Toàn bộ hành động đã thực hiện (nhật ký)
1. Đọc `knowledge-base/common/*` + `README.md` + 3 tài liệu Phase 4.
2. Khảo sát code: `BoatController`, `BoatStats`, `IWeightProvider`, `WeightSimulator`, `BoatTestRunner`.
3. Lập plan, xác nhận 2 quyết định với user (clamp Full 3D; PhysicMaterial qua Editor helper).
4. Tạo `IDurabilityProvider.cs`.
5. Cập nhật `BoatStats.cs` (thêm `baseMaxSpeed`).
6. Cập nhật `BoatController.cs` (Awake config + `ClampVelocityByDurability`).
7. Tạo `DurabilitySimulator.cs` (slider + HUD vận tốc + ram test).
8. Tạo `BoatPhysicMaterialCreator.cs` (Editor menu).
9. Mở rộng `BoatTestRunner.cs` với nhóm test Durability.
10. Tạo `build-check-phase04.sh` (+ `chmod +x`).
11. Chạy `./build-check-phase04.sh` → compile sạch, 49/49 PASS.
12. Viết báo cáo này.

## 10. Cam kết không đụng vùng cấm
KHÔNG sửa: `Assets/Scenes/`, `Assets/Settings/`, `Assets/TutorialInfo/`,
`InputSystem_Actions.inputactions`, `InventoryManager.cs`. Code mới nằm trong
`Assets/_Project/` đúng phân lớp Clean Architecture (`ChoNoi.*`).
