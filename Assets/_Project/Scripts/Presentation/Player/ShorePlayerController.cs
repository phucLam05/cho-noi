using UnityEngine;
using UnityEngine.InputSystem;

namespace ChoNoi.Presentation.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class ShorePlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 4.2f;
        [SerializeField] private float sprintMultiplier = 1.45f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private Transform cameraTransform;

        private CharacterController characterController;
        private Animator animator;
        private string currentAnimState = "";
        private Vector3 verticalVelocity;
        private bool canMove = true;
        private float coyoteTimer;
        private float jumpBufferTimer;

        public bool CanMove
        {
            get => canMove;
            set
            {
                canMove = value;
                if (!canMove)
                {
                    verticalVelocity = Vector3.zero;
                    PlayAnimation("Neutral Idle");
                }
            }
        }

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
            animator = GetComponentInChildren<Animator>();
        }

        private void Update()
        {
            if (!canMove || characterController == null || !characterController.enabled)
            {
                PlayAnimation("Neutral Idle");
                return;
            }

            float currentScale = 1f;
            Transform visual = transform.Find("PlayerVisualRoot");
            if (visual != null)
            {
                currentScale = visual.localScale.y;
            }

            Vector2 input = ReadMoveInput();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (Keyboard.current?.spaceKey.wasPressedThisFrame == true)
                jumpBufferTimer = jumpBufferTime;
            else
                jumpBufferTimer -= Time.deltaTime;

            Vector3 move = GetCameraRelativeMove(input);
            move = Vector3.ClampMagnitude(move, 1f);

            Vector3 movementVelocity = Vector3.zero;
            if (move.sqrMagnitude > 0.001f)
            {
                float speed = walkSpeed * currentScale;
                if (Keyboard.current?.leftShiftKey.isPressed == true)
                    speed *= sprintMultiplier;

                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                movementVelocity = move * speed;

                PlayAnimation("Walking");
            }
            else
            {
                PlayAnimation("Neutral Idle");
            }

            if (characterController.isGrounded)
            {
                coyoteTimer = coyoteTime;

                if (verticalVelocity.y < 0f)
                    verticalVelocity.y = -1.5f * currentScale;
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }

            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * currentScale * -2f * (gravity * currentScale));
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
            }

            verticalVelocity.y += gravity * currentScale * Time.deltaTime;
            
            Vector3 combinedMove = (movementVelocity + verticalVelocity) * Time.deltaTime;
            characterController.Move(combinedMove);
        }

        private void PlayAnimation(string stateName)
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator == null || !animator.enabled)
                return;

            if (currentAnimState != stateName)
            {
                currentAnimState = stateName;
                animator.CrossFadeInFixedTime(stateName, 0.15f);
            }
        }

        private Vector3 GetCameraRelativeMove(Vector2 input)
        {
            if (cameraTransform == null)
                return new Vector3(input.x, 0f, input.y);

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();
            return forward * input.y + right * input.x;
        }

        private Vector2 ReadMoveInput()
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
