# Cấu trúc ghi chú code (Template)
Mọi script C# phải có phần chú thích đầu file và từng phương thức theo format sau:

/**
 * [Tên Lớp]: Tóm tắt chức năng.
 * [Chức năng]: Giải thích chi tiết lớp này làm gì.
 * [Dependencies]: Các class/interface phụ thuộc.
 */

// Ví dụ cho phương thức:
/// <summary>
/// Thực hiện áp dụng lực đẩy tới trước cho ghe dựa trên đầu vào của người chơi.
/// </summary>
/// <param name="throttle">Giá trị từ -1 đến 1.</param>
private void ApplyThrust(float throttle) 
{
    // 1. Tính toán vector lực theo hướng mũi ghe (transform.forward)
    // 2. Nhân với biến boatStats.forwardThrust để có lực thực tế
    // 3. Sử dụng ForceMode.Acceleration để bỏ qua khối lượng vật thể trong tính toán
}