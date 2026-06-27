# Time & Physics Rules

## 1. Luật của Time System
- **Không dính dáng đến UI:** `TimeManager` TUYỆT ĐỐI không gọi lệnh update TextUI. Nó chỉ được phép phát ra các sự kiện (Events) bằng C# `Action` hoặc `UnityEvent`.
  - `public event Action<int, int> OnTimeChanged; // Trả về giờ, phút`
  - `public event Action<GamePhase> OnPhaseChanged; // Trả về Phase hiện tại`
- **Time Scale:** Cần có một biến `[SerializeField] float timeScale` để Game Designer chỉnh tốc độ trôi của thời gian.

## 2. Công thức Vật lý Tải trọng (Weight Penalty)
Ghe chở càng nặng, gia tốc càng chậm và bẻ lái càng lỳ (trễ). Hãy áp dụng công thức sau vào `BoatController`:

1.  **Lấy tỷ lệ tải trọng:**
    $WeightRatio = \frac{CurrentWeight}{MaxCapacity}$
    *(Giá trị từ 0.0 đến 1.0, trong đó 1.0 là đầy tải)*

2.  **Tính toán hệ số hiệu suất (Performance Multiplier):**
    $Performance = 1.0f - (WeightRatio \times MaxPenaltyFactor)$
    *(Trong đó MaxPenaltyFactor là biến cấu hình nằm trong BoatStats. Nếu MaxPenaltyFactor = 0.4f, khi ghe đầy hàng thì hiệu suất chỉ còn 60%)*

3.  **Áp dụng vào Vật lý (trong FixedUpdate):**
    - $ActualThrust = forwardThrust \times Performance$
    - $ActualTorque = turnTorque \times Performance$
    - Lực cản nước (Drag) có thể tăng nhẹ khi chở nặng: $ActualDrag = waterDrag \times (1.0f + (WeightRatio \times 0.5f))$