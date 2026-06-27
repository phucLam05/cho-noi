/**
 * ITimeSystem: Interface trừu tượng hóa hệ thống thời gian in-game.
 * [Chức năng]: Định nghĩa hợp đồng (contract) cho đồng hồ game — chỉ phát ra
 *              sự kiện, KHÔNG đụng tới UI. Mọi hệ thống khác (UI, gameplay,
 *              ánh sáng...) lắng nghe các event này để phản ứng theo thời gian.
 * [Dependencies]: GamePhase (Domain).
 */

using System;

namespace ChoNoi.Domain
{
    public interface ITimeSystem
    {
        /// <summary>
        /// Bắn ra mỗi khi phút in-game thay đổi. Tham số: (giờ 0-23, phút 0-59).
        /// </summary>
        event Action<int, int> OnTimeChanged;

        /// <summary>
        /// Bắn ra khi đồng hồ bước sang một GamePhase mới (đổi khung giờ).
        /// </summary>
        event Action<GamePhase> OnPhaseChanged;

        /// <summary>
        /// Phase hiện tại của đồng hồ game.
        /// </summary>
        GamePhase CurrentPhase { get; }
    }
}
