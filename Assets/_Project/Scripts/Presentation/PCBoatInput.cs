/**
 * PCBoatInput: Triển khai IBoatInput cho bàn phím/gamepad qua Unity New Input System.
 * [Chức năng]: Đọc Action "Move" từ ActionMap "Player" trong InputActionAsset
 *              và cung cấp Throttle/Steering cho BoatController qua interface IBoatInput.
 *              Dùng InputActionAsset trực tiếp thay vì generated wrapper class để tránh
 *              phụ thuộc vào bước code-gen của Unity Editor.
 * [Dependencies]: IBoatInput (Domain), UnityEngine.InputSystem.
 */

using UnityEngine;
using UnityEngine.InputSystem;
using ChoNoi.Domain;

namespace ChoNoi.Presentation
{
    public class PCBoatInput : MonoBehaviour, IBoatInput
    {
        // Kéo thả file InputSystem_Actions.inputactions vào đây trong Inspector
        [SerializeField] private InputActionAsset inputActions;

        public InputActionAsset InputActions
        {
            get => inputActions;
            set => inputActions = value;
        }

        private InputAction moveAction;
        private Vector2 moveInput;

        public float Throttle => moveInput.y;
        public float Steering => moveInput.x;

        private void Awake()
        {
            // Bước 1: Tìm ActionMap "Player" từ asset, ném lỗi rõ ràng nếu tên sai
            var playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);

            // Bước 2: Tìm Action "Move" trong map vừa lấy
            moveAction = playerMap.FindAction("Move", throwIfNotFound: true);
        }

        private void OnEnable()
        {
            if (inputActions != null)
            {
                inputActions.Enable();
            }
            moveAction.Enable();
            moveAction.performed += OnMove;
            moveAction.canceled  += OnMove;
        }

        private void OnDisable()
        {
            moveAction.performed -= OnMove;
            moveAction.canceled  -= OnMove;
            moveAction.Disable();
            if (inputActions != null)
            {
                inputActions.Disable();
            }
        }

        /// <summary>
        /// Callback nhận giá trị Vector2 từ Action "Move" (WASD / Left Stick).
        /// </summary>
        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
            Debug.Log($"[PCBoatInput] Nhận input di chuyển: {moveInput}");
        }
    }
}
