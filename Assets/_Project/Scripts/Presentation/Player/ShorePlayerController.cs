using UnityEngine;
using UnityEngine.InputSystem;

namespace ChoNoi.Presentation.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class ShorePlayerController : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 2.35f;
        [SerializeField] private float sprintMultiplier = 1.3f;
        [SerializeField] private float rotationSpeed = 12f;
        [SerializeField] private float gravity = -18f;
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float coyoteTime = 0.12f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float groundedStickForce = 3.5f;
        [SerializeField] private float groundProbeDistance = 0.45f;
        [SerializeField] private float groundSnapDistance = 0.35f;
        [SerializeField] private float slopeProjectionBlend = 0.9f;

        private CharacterController characterController;
        private Animator animator;
        private Transform animatorTransform;
        private Transform hipsTransform;
        private Vector3 animatorLocalPosition;
        private Quaternion animatorLocalRotation;
        private string currentAnimState = "";
        private Vector3 verticalVelocity;
        private bool canMove = true;
        private float coyoteTimer;
        private float jumpBufferTimer;
        private RaycastHit lastGroundHit;
        private bool hasGroundHit;

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
            CacheAnimationTransforms();
            ConfigureCharacterController();
        }

        private void LateUpdate()
        {
            // Generic Mixamo clips can animate the model root and hips even with root motion disabled.
            // Keep gameplay movement on the CharacterController and retain only the hips' vertical bob.
            if (animatorTransform != null)
            {
                animatorTransform.localPosition = animatorLocalPosition;
                animatorTransform.localRotation = animatorLocalRotation;
            }

            if (hipsTransform != null)
            {
                Vector3 hipsPosition = hipsTransform.localPosition;
                hipsPosition.x = 0f;
                hipsPosition.z = 0f;
                hipsTransform.localPosition = hipsPosition;
            }
        }

        private void Update()
        {
            if (!canMove || characterController == null || !characterController.enabled)
            {
                PlayAnimation("Neutral Idle");
                return;
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
                float speed = walkSpeed;
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

            bool isGrounded = IsGrounded(out RaycastHit groundHit);
            if (isGrounded)
            {
                coyoteTimer = coyoteTime;
                hasGroundHit = true;
                lastGroundHit = groundHit;

                if (verticalVelocity.y < 0f)
                    verticalVelocity.y = -groundedStickForce;

                if (movementVelocity.sqrMagnitude > 0.001f)
                {
                    Vector3 projectedMove = Vector3.ProjectOnPlane(movementVelocity, groundHit.normal);
                    movementVelocity = Vector3.Lerp(movementVelocity, projectedMove, slopeProjectionBlend);
                }
            }
            else
            {
                coyoteTimer -= Time.deltaTime;
            }

            bool jumpedThisFrame = false;
            if (jumpBufferTimer > 0f && coyoteTimer > 0f)
            {
                verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                jumpBufferTimer = 0f;
                coyoteTimer = 0f;
                jumpedThisFrame = true;
            }

            verticalVelocity.y += gravity * Time.deltaTime;
            
            Vector3 combinedMove = (movementVelocity + verticalVelocity) * Time.deltaTime;
            characterController.Move(combinedMove);

            if (!jumpedThisFrame)
                SnapToGround();
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

        private void ConfigureCharacterController()
        {
            if (characterController == null)
                return;

            characterController.minMoveDistance = 0f;
            characterController.skinWidth = Mathf.Max(characterController.skinWidth, 0.08f);
            characterController.stepOffset = Mathf.Max(characterController.stepOffset, 0.6f);
            characterController.slopeLimit = Mathf.Max(characterController.slopeLimit, 50f);
        }

        private void CacheAnimationTransforms()
        {
            if (animator == null)
                return;

            animator.applyRootMotion = false;
            animatorTransform = animator.transform;
            animatorLocalPosition = animatorTransform.localPosition;
            animatorLocalRotation = animatorTransform.localRotation;

            Transform[] children = animator.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name != "mixamorig:Hips" && child.name != "Hips")
                    continue;

                hipsTransform = child;
                break;
            }
        }

        private bool IsGrounded(out RaycastHit hit)
        {
            if (characterController == null)
            {
                hit = default;
                return false;
            }

            if (characterController.isGrounded && ProbeGround(groundProbeDistance + 0.1f, out hit))
                return true;

            return ProbeGround(groundProbeDistance, out hit);
        }

        private bool ProbeGround(float probeDistance, out RaycastHit hit)
        {
            Vector3 origin = GetGroundProbeOrigin();
            float radius = Mathf.Max(0.05f, characterController.radius * 0.92f);
            float castDistance = probeDistance + 0.08f;

            if (Physics.SphereCast(origin, radius, Vector3.down, out hit, castDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null && !hit.collider.transform.IsChildOf(transform))
                    return true;
            }

            return false;
        }

        private Vector3 GetGroundProbeOrigin()
        {
            float halfHeight = Mathf.Max(characterController.height * 0.5f, characterController.radius);
            Vector3 centerWorld = transform.position + characterController.center;
            float bottomOffset = halfHeight - characterController.radius;
            return centerWorld - Vector3.up * bottomOffset + Vector3.up * 0.08f;
        }

        private void SnapToGround()
        {
            if (!hasGroundHit || verticalVelocity.y > 0f)
                return;

            if (!ProbeGround(groundSnapDistance, out RaycastHit hit))
                return;

            float snapDistance = Mathf.Max(0f, hit.distance - 0.05f);
            if (snapDistance <= 0.001f)
                return;

            characterController.Move(Vector3.down * Mathf.Min(snapDistance, groundSnapDistance));
            lastGroundHit = hit;
        }
    }
}
