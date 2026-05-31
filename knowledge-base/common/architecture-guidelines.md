# Architecture Guidelines (Quy tắc kiến trúc)
1. **Phân lớp:**
   - `Domain`: Logic thuần C#, không MonoBehaviour.
   - `Application`: Điều phối, Services trung gian.
   - `Infrastructure`: Data (ScriptableObjects), cấu hình.
   - `Presentation`: MonoBehaviour, Input, Vật lý, UI.
2. **Nguyên tắc:**
   - Dependency Inversion: Class cấp cao không phụ thuộc class cấp thấp, cả hai cùng phụ thuộc Interface.
   - No God Classes: Mỗi class chỉ giải quyết 1 nhiệm vụ duy nhất (Single Responsibility).
   - Data-Driven: Mọi chỉ số phải nằm trong ScriptableObjects, không hard-code.
3. **Thư mục dự án:** Mọi code mới phải nằm trong `Assets/_Project/`. Tuyệt đối không xóa/sửa folder `Scenes`, `Settings`, `TutorialInfo` gốc của Unity.