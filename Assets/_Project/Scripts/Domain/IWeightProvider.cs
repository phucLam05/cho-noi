/**
 * IWeightProvider: Interface cung cấp tỷ lệ tải trọng hiện tại của ghe.
 * [Chức năng]: Điểm decouple (Dependency Inversion) giữa hệ thống vật lý ghe và
 *              hệ thống kho hàng. BoatController chỉ phụ thuộc interface này, không
 *              biết gì về InventoryManager hay WeightSimulator cụ thể.
 *              Bất kỳ class nào (InventoryManager, script test...) đều có thể
 *              implement để cấp dữ liệu khối lượng cho ghe.
 * [Dependencies]: Không có (interface thuần C#, Domain Layer).
 */

namespace ChoNoi.Domain
{
    public interface IWeightProvider
    {
        /// <summary>
        /// Trả về tỷ lệ tải trọng hiện tại trong khoảng [0.0, 1.0].
        /// 0.0 = ghe trống, 1.0 = ghe đầy tải (CurrentWeight / MaxCapacity).
        /// </summary>
        float GetCurrentWeightRatio();
    }
}
