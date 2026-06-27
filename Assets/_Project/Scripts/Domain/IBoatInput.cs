/**
 * IBoatInput: Interface định nghĩa đầu vào điều khiển ghe.
 * [Chức năng]: Trừu tượng hóa nguồn đầu vào (bàn phím, gamepad, AI...)
 *              để BoatController không phụ thuộc vào thiết bị cụ thể.
 * [Dependencies]: Không có.
 */

namespace ChoNoi.Domain
{
    public interface IBoatInput
    {
        /// <summary>
        /// Giá trị ga/thắng theo trục dọc, từ -1 (lùi) đến 1 (tiến).
        /// </summary>
        float Throttle { get; }

        /// <summary>
        /// Giá trị lái theo trục ngang, từ -1 (trái) đến 1 (phải).
        /// </summary>
        float Steering { get; }
    }
}
