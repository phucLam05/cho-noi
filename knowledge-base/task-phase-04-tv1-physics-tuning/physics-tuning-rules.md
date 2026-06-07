# Physics Tuning & Anti-Bug Rules

## 1. Luật Giới hạn Vận tốc (Velocity Clamping)
Khác với sức nặng (làm giảm gia tốc), độ hỏng hóc sẽ làm **giới hạn vận tốc tối đa**.
- Không giảm biến `thrustForce` (để ghe hỏng vẫn có lực nhích đi từ từ).
- Thay vào đó, trong `FixedUpdate`, hãy kiểm tra độ lớn của `rb.velocity.magnitude`.
- *Công thức:* 
  `float currentMaxSpeed = baseMaxSpeed * Mathf.Clamp(DurabilityRatio, 0.3f, 1.0f);` 
  *(Giữ lại ít nhất 30% tốc độ để ghe không bị kẹt chết ở giữa sông khi độ bền = 0).*
  Nếu `rb.velocity.magnitude > currentMaxSpeed`, hãy normalize vector vận tốc và nhân với `currentMaxSpeed`.

## 2. Xử lý lỗi xuyên vật thể (Tunneling/Clipping)
- Lỗi xuyên tường xảy ra khi vật thể di chuyển quá nhanh so với thời gian cập nhật của `FixedUpdate` (`Time.fixedDeltaTime` mặc định là 0.02s).
- **Quy tắc bắt buộc:** Rigidbody của `BoatController` phải được set `rb.collisionDetectionMode = CollisionDetectionMode.Continuous`.
- Các vật cản tĩnh (như bờ sông, nhà cửa) phải có `Collider` nhưng KHÔNG gắn `Rigidbody`.

## 3. Phản hồi va chạm tự nhiên (Natural Impact)
- Để tránh việc game bị văng (crash) hoặc vật lý văng lên trời do force phản lực quá lớn, hãy khóa các trục xoay không cần thiết của ghe.
- Set `rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ` để ghe chỉ có thể xoay quanh trục Y (trục đứng) khi va chạm.