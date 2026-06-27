# Tech Stack Rules
- **Unity Version:** 2022.3 LTS.
- **Input System:** Ưu tiên sử dụng Unity New Input System (đã có sẵn `InputSystem_Actions.inputactions`).
- **Physics:** Sử dụng `Rigidbody` với `FixedUpdate` cho mọi chuyển động ghe.
- **Naming Convention:** - C# Classes: PascalCase.
  - Variables/Methods: camelCase.
  - Namespace: ChoNoi.[Layer].[SubFolder].
- **Code Style:** Cần comment giải thích logic phức tạp (đặc biệt là công thức vật lý). Luôn ưu tiên dùng `[SerializeField]` cho các biến private cần chỉnh sửa trong Inspector.