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
        private Vector2 actionMoveInput;
        private Vector2 smoothedMoveInput;
        [SerializeField] private float inputResponsiveness = 8f;

        public float Throttle => smoothedMoveInput.y;
        public float Steering => smoothedMoveInput.x;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogWarning("[PCBoatInput] InputActionAsset is missing. Keyboard polling fallback will still work.");
                return;
            }

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
            if (moveAction != null)
            {
                moveAction.Enable();
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
                moveAction.Disable();
            }
            if (inputActions != null)
            {
                inputActions.Disable();
            }
        }

        private void Update()
        {
            Vector2 keyboardInput = ReadKeyboardMove();
            Vector2 targetInput = keyboardInput.sqrMagnitude > 0.001f ? keyboardInput : actionMoveInput;

            smoothedMoveInput = Vector2.Lerp(
                smoothedMoveInput,
                Vector2.ClampMagnitude(targetInput, 1f),
                1f - Mathf.Exp(-inputResponsiveness * Time.deltaTime));
        }

        /// <summary>
        /// Callback nhận giá trị Vector2 từ Action "Move" (WASD / Left Stick).
        /// </summary>
        private void OnMove(InputAction.CallbackContext context)
        {
            actionMoveInput = context.ReadValue<Vector2>();
        }

        private Vector2 ReadKeyboardMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            float x = 0f;
            float y = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) y += 1f;

            return new Vector2(x, y);
        }
    }
}
