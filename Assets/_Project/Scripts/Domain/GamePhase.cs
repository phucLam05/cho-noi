/**
 * GamePhase: Enum định nghĩa 4 giai đoạn (Phase) trong một ngày của chợ nổi.
 * [Chức năng]: Dùng chung cho TimeManager và các hệ thống lắng nghe sự kiện
 *              chuyển Phase. Mỗi Phase tương ứng một khung giờ in-game.
 *   - Dawn  (03:00) — Bình Minh: bán hàng tại Cây Bẹo, mặc cả với thương lái.
 *   - Day   (10:00) — Ban Ngày: thu mua nông sản tại các kênh rạch.
 *   - Dusk  (13:00) — Chiều Tà: tiếp tục thu mua / chuẩn bị về bến.
 *   - Night (18:00) — Tối: nâng cấp ghe, bảo trì tại bến (kéo dài tới 03:00 hôm sau).
 * [Dependencies]: Không có (enum thuần C#, Domain Layer).
 */

namespace ChoNoi.Domain
{
    public enum GamePhase
    {
        Dawn,   // Bình Minh — 03:00
        Day,    // Ban Ngày  — 10:00
        Dusk,   // Chiều Tà  — 13:00
        Night   // Tối       — 18:00
    }
}
