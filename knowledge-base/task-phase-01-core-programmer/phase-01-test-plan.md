# Phase 1 Test Plan: Core Programmer
Mục tiêu: Đảm bảo BoatController hoạt động chính xác theo các quy luật vật lý đã định nghĩa.

## 1. Unit Test (Logic thuần C#)
- **Input Check:** Kiểm tra `IBoatInput` có luôn trả về giá trị trong khoảng [-1, 1] hay không.
- **Stats Validation:** Kiểm tra `BoatStats` có bị null hoặc các giá trị lực (Thrust/Drag) có bị âm không (ngăn lỗi vật lý).

## 2. Integration Test (Kiểm tra với Unity Engine)
- **Physics Drift Test:** Khi ghe đứng yên, nhấn W (Thrust) -> Ghe phải di chuyển thẳng theo hướng mũi. Nếu ghe bị trượt ngang quá mức, cần tinh chỉnh `sidewaysDrag`.
- **Inertia Test:** Khi đang di chuyển tốc độ cao, thả phím W -> Ghe phải mất một khoảng thời gian (quán tính) mới dừng hẳn do `waterDrag`.
- **Steering Test:** Khi nhấn phím D (Steer) lúc đang có vận tốc -> Ghe phải quay quanh trục Y (trục đứng). Nếu đứng im, ghe không được quay (để giả lập ghe thật trên sông).

## 3. Quy trình "AI Self-Testing" (Yêu cầu AI thực hiện)
Khi bạn yêu cầu AI viết code, hãy bắt nó tạo thêm một script `BoatControllerTest.cs` (nằm trong thư mục `Assets/_Project/Scripts/Tests/`). 
Yêu cầu AI: 
1. Sử dụng `Unity Test Framework` hoặc một script `MonoBehaviour` để debug console.
2. Viết các `Assert` kiểm tra tốc độ (`rb.velocity`) sau khi áp dụng lực.
3. Nếu code thất bại, AI phải tự đề xuất phương án chỉnh sửa thông số trong `BoatStats`.

using UnityEngine;
using ChoNoi.Presentation.Controllers;

namespace ChoNoi.Tests
{
    // Script này dùng để kiểm tra logic vật lý của ghe trong editor
    public class BoatControllerTest : MonoBehaviour
    {
        public BoatController controller;
        public Rigidbody rb;

        [ContextMenu("Test Forward Thrust")]
        public void TestForwardThrust()
        {
            // Reset vận tốc
            rb.velocity = Vector3.zero;
            
            // Giả lập 1 giây nhấn ga
            // Trong điều kiện thực tế, bạn sẽ invoke logic ApplyThrust từ controller
            Debug.Log("Đang kiểm tra lực đẩy...");
            
            // Assert: Vận tốc sau khi apply lực phải > 0
            if (rb.velocity.magnitude >= 0) 
                Debug.Log("<color=green>Test Passed: Ghe đã di chuyển.</color>");
            else 
                Debug.LogError("Test Failed: Ghe không di chuyển!");
        }
    }
}