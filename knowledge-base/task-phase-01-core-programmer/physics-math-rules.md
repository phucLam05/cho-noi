# Physics Rules & Formulas
Dành cho việc code `BoatController.cs`:

1. **Thrust (Lực đẩy):** Sử dụng `Rigidbody.AddForce(transform.forward * throttle * stats.thrustForce, ForceMode.Acceleration);`
2. **Drag (Lực cản nước):** Phải tạo lực cản ngược chiều vận tốc để ghe không bị trượt mãi mãi.
   `Vector3 resistance = -rb.velocity * stats.waterDrag;`
3. **Sideways Resistance:** Quan trọng nhất để ghe chạy đúng hướng mũi.
   `Vector3 sidewaysVelocity = transform.right * Vector3.Dot(rb.velocity, transform.right);`
   `rb.AddForce(-sidewaysVelocity * stats.sidewaysDrag, ForceMode.Acceleration);`
4. **Steering (Bẻ lái):** Chỉ áp dụng khi ghe có vận tốc.
   `rb.AddTorque(transform.up * steerInput * stats.turnTorque * rb.velocity.magnitude, ForceMode.Acceleration);`