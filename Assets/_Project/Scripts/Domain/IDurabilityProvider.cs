/**
 * IDurabilityProvider: Interface cung cấp tỷ lệ độ bền hiện tại của ghe.
 * [Chức năng]: Điểm decouple (Dependency Inversion) giữa hệ thống vật lý ghe và hệ thống
 *              độ bền (hư hỏng). BoatController chỉ phụ thuộc interface này, không biết
 *              class cụ thể (script test hay hệ thống nâng cấp ghe sau này).
 *              Tương tự IWeightProvider ở Phase 2.
 * [Dependencies]: Không có (interface thuần C#, Domain Layer).
 */

namespace ChoNoi.Domain
{
    public interface IDurabilityProvider
    {
        /// <summary>
        /// Trả về tỷ lệ độ bền hiện tại trong khoảng [0.0, 1.0].
        /// 1.0 = ghe nguyên vẹn (tốc độ tối đa), 0.0 = hỏng nặng (trần tốc độ thấp nhất).
        /// </summary>
        float GetDurabilityRatio();
    }
}
