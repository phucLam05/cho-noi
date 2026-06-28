# Cơ chế Xác suất Mặc cả (Bargaining Mechanics)

Tài liệu này ghi chép công thức toán học và nguyên lý tính toán xác suất phản ứng của thương lái (NPC) trong quá trình mặc cả sỉ hàng nông sản.

---

## 1. Tổng quan
Khi người chơi đưa ra đơn giá bán đề xuất cao hơn mức giá tối đa mà thương lái có thể chấp nhận (`NpcMaxAcceptPrice`), giao dịch sẽ không bị từ chối ngay lập tức mà được quyết định bằng cơ chế xác suất ngẫu nhiên.
*   **Nếu đơn giá đề xuất $\le$ Giá trần của NPC**: NPC **lập tức đồng ý** (Xác suất thành công $100\%$).
*   **Nếu đơn giá đề xuất $>$ Giá trần của NPC**: NPC sẽ cân nhắc dựa trên mô hình **Suy giảm mũ (Exponential Decay)** để quyết định **Chấp nhận**, **Đôi co tiếp** hoặc **Quay lưng đi**.

---

## 2. Công thức tính toán

### 2.1 Tỉ lệ chênh lệch giá (Markup Ratio)
Để lượng hóa độ "chém giá" của người chơi, chúng ta tính tỉ lệ chênh lệch $Ratio$:
\[Ratio = \frac{Price_{proposed} - Price_{max\_accept}}{Price_{max\_accept}}\]

*Ví dụ: Nếu giá trần tối đa NPC chấp nhận là 10,000 VNĐ, người chơi đề xuất 12,000 VNĐ thì $Ratio = \frac{12,000 - 10,000}{10,000} = 0.20$ ($20\%$ chênh lệch).*

---

### 2.2 Xác suất NPC đồng ý (Acceptance Probability)
Xác suất NPC gật đầu đồng ý với giá cao hơn giá trần tuân theo hàm số mũ suy giảm:
\[P_{accept} = e^{-k \times Ratio}\]
*   Trong đó $k = 5.0$ là hệ số dốc điều chỉnh tốc độ sụt giảm xác suất.

#### Biểu đồ phân bố xác suất đồng ý:
| Độ chênh lệch ($Ratio$) | Xác suất đồng ý ($P_{accept}$) | Mô tả phản ứng |
|---|---|---|
| $0\%$ (Bằng giá trần) | $100\%$ | Lập tức đồng ý |
| $5\%$ chênh lệch | $e^{-0.25} \approx 77.88\%$ | Tỉ lệ đồng ý rất cao |
| $10\%$ chênh lệch | $e^{-0.50} \approx 60.65\%$ | Phân vân gật đầu |
| $20\%$ chênh lệch | $e^{-1.00} \approx 36.79\%$ | Khá đắt, tỉ lệ mua thấp |
| $30\%$ chênh lệch | $e^{-1.50} \approx 22.31\%$ | Khó chịu, ít khi mua |
| $50\%$ chênh lệch | $e^{-2.50} \approx 8.21\%$ | Rất hiếm khi đồng ý |
| $100\%$ chênh lệch | $e^{-5.00} \approx 0.67\%$ | Gần như không thể |

---

### 2.3 Xác suất NPC bỏ đi (Walk Away Probability)
Nếu NPC không đồng ý giá đề xuất, họ sẽ cân nhắc việc từ chối giao dịch tiếp và bỏ đi ngay lập tức:
\[P_{walk\_away} = 1.0 - e^{-c \times Ratio \times (Turn + 1)}\]
*   Trong đó $c = 3.0$ là hệ số điều chỉnh tốc độ bỏ đi.
*   $Turn$ là số lượt đôi co hiện tại (Lượt đầu $= 0$, Lượt hai $= 1$, Lượt ba $= 2$).
*   Ở lượt thương thảo cuối cùng ($Turn \ge 3$), nếu không chấp nhận giá thì $P_{walk\_away}$ mặc định là $100\%$.
*   Để giữ game cân bằng, chúng ta thiết lập mức sàn tối thiểu của $P_{walk\_away}$ tăng dần theo lượt (Lượt 1: $10\%$, Lượt 2: $30\%$).

#### Biểu đồ phân bố xác suất bỏ đi theo lượt và chênh lệch giá:
*   **Lượt đề xuất đầu tiên (Turn = 0)**:
    *   $Ratio = 10\% \implies P_{walk\_away} = 1 - e^{-0.3} \approx 25.9\%$
    *   $Ratio = 50\% \implies P_{walk\_away} = 1 - e^{-1.5} \approx 77.6\%$
*   **Lượt đôi co thứ hai (Turn = 1)**:
    *   $Ratio = 10\% \implies P_{walk\_away} = \max(10\%, 1 - e^{-0.6}) \approx 45.1\%$
    *   $Ratio = 50\% \implies P_{walk\_away} = \max(10\%, 1 - e^{-3.0}) \approx 95.0\%$
*   **Lượt đôi co thứ ba (Turn = 2)**:
    *   $Ratio = 10\% \implies P_{walk\_away} = \max(30\%, 1 - e^{-0.9}) \approx 59.3\%$
    *   $Ratio = 50\% \implies P_{walk\_away} = \max(30\%, 1 - e^{-4.5}) \approx 98.9\%$
*   **Vượt quá 3 lượt**: $100\%$ bỏ đi.
