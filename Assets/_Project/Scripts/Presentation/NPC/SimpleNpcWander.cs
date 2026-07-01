using UnityEngine;

namespace ChoNoi.Presentation.NPC
{
    public class SimpleNpcWander : MonoBehaviour
    {
        [SerializeField] private Vector3[] localWaypoints;
        [SerializeField] private float moveSpeed = 1.6f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float waitAtPoint = 1.2f;
        [SerializeField] private float moveSmoothTime = 0.12f;

        private Vector3 originLocalPosition;
        private Vector3 localMoveVelocity;
        private int waypointIndex;
        private float waitTimer;
        private bool isPaused;
        private Animator animator;
        private int currentAnimationHash;
        private float nextPlayerSearchTime;
        private float visualYawOffset;
        private bool isCurrentlyWaving;
        private Transform hipsTransform;

        private float waveDurationTimer;
        private bool hasFinishedWaving;

        [Header("Animation Options")]
        [SerializeField] private float waveDistance = 6f;
        [SerializeField] private float waveDuration = 4.5f; // Duration for roughly 3-4 wave actions
        [SerializeField] private bool useHappyIdle;
        [SerializeField] private bool useTwoHandWave;

        private Transform playerTransform;

        public void Configure(Vector3[] points, float speed)
        {
            localWaypoints = points;
            moveSpeed = speed;
        }

        private void Start()
        {
            originLocalPosition = transform.localPosition;
            animator = GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.applyRootMotion = false;
                visualYawOffset = Mathf.DeltaAngle(0f, animator.transform.localEulerAngles.y);
                FindHips(animator.transform);
            }
            
            // Randomize NPC behavior options for variety
            useHappyIdle = (Random.value > 0.5f);
            useTwoHandWave = (Random.value > 0.5f);
        }

        private void FindHips(Transform parent)
        {
            if (parent.name.ToLower().Contains("hips") || parent.name.ToLower().Contains("pelvis"))
            {
                hipsTransform = parent;
                return;
            }
            for (int i = 0; i < parent.childCount; i++)
            {
                FindHips(parent.GetChild(i));
                if (hipsTransform != null) return;
            }
        }

        private void FindPlayer()
        {
            if (playerTransform != null && playerTransform.gameObject.activeInHierarchy)
                return;

            if (Time.time < nextPlayerSearchTime)
                return;

            nextPlayerSearchTime = Time.time + 0.5f;
            playerTransform = null;

            var playerObj = GameObject.Find("PlayerOnFoot");
            if (playerObj != null && playerObj.activeInHierarchy)
            {
                playerTransform = playerObj.transform;
                return;
            }

            playerObj = GameObject.Find("PlayerBoat");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }

        private void Update()
        {
            FindPlayer();

            // Proximity interaction: Check if player is nearby with hysteresis to prevent oscillation
            bool isNearPlayer = false;
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                float activeWaveDistance = isCurrentlyWaving ? (waveDistance + 2f) : waveDistance;
                if (dist < activeWaveDistance)
                {
                    isNearPlayer = true;
                }
            }
            if (isNearPlayer)
            {
                isCurrentlyWaving = true;
                if (!hasFinishedWaving)
                {
                    // Play waving animation and look at player
                    string waveAnim = useTwoHandWave ? "Waving_twohands" : "Waving_onehand";
                    PlayAnimation(waveAnim);
                    localMoveVelocity = Vector3.zero;

                    Vector3 lookDir = playerTransform.position - transform.position;
                    FaceVisualToward(lookDir);

                    waveDurationTimer += Time.deltaTime;
                    if (waveDurationTimer >= waveDuration)
                    {
                        hasFinishedWaving = true;
                    }
                    return; // Hold movement while waving
                }
            }
            else
            {
                // Reset waving sequence once player moves away
                isCurrentlyWaving = false;
                hasFinishedWaving = false;
                waveDurationTimer = 0f;
            }

            if (isPaused)
            {
                string idleAnim = useHappyIdle ? "Happy Idle" : "Neutral Idle";
                PlayAnimation(idleAnim);
                localMoveVelocity = Vector3.zero;
                return;
            }

            if (localWaypoints == null || localWaypoints.Length == 0)
                return;

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                string idleAnim = useHappyIdle ? "Happy Idle" : "Neutral Idle";
                PlayAnimation(idleAnim);
                localMoveVelocity = Vector3.zero;
                return;
            }

            Vector3 target = originLocalPosition + localWaypoints[waypointIndex];
            Vector3 delta = target - transform.localPosition;
            delta.y = 0f;

            if (delta.magnitude < 0.2f)
            {
                waypointIndex = (waypointIndex + 1) % localWaypoints.Length;
                waitTimer = waitAtPoint;
                string idleAnim = useHappyIdle ? "Happy Idle" : "Neutral Idle";
                PlayAnimation(idleAnim);
                localMoveVelocity = Vector3.zero;
                return;
            }

            Vector3 flatTarget = new Vector3(target.x, transform.localPosition.y, target.z);
            transform.localPosition = Vector3.SmoothDamp(
                transform.localPosition,
                flatTarget,
                ref localMoveVelocity,
                moveSmoothTime,
                moveSpeed,
                Time.deltaTime);

            Vector3 localFacingDirection = localMoveVelocity.sqrMagnitude > 0.0001f ? localMoveVelocity.normalized : delta.normalized;
            Vector3 worldDirection = transform.parent != null ? transform.parent.TransformDirection(localFacingDirection) : localFacingDirection;
            if (worldDirection.sqrMagnitude > 0.0001f)
            {
                FaceVisualToward(worldDirection);
            }

            PlayAnimation("Walking");
        }

        private void LateUpdate()
        {
            if (animator != null)
            {
                // Force the child model (containing the Animator) to stay centered at local zero
                // to prevent any root-motion-like offsets or translation drift built into the animation clip.
                animator.transform.localPosition = Vector3.zero;
                animator.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (hipsTransform != null)
            {
                // Keep the vertical bobbing (Y) but force horizontal offsets (X, Z) to zero
                // to completely eliminate the walk cycle sliding forward and snapping back.
                hipsTransform.localPosition = new Vector3(0f, hipsTransform.localPosition.y, 0f);
            }
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }

        private void PlayAnimation(string stateName)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
                return;

            int stateHash = Animator.StringToHash(stateName);
            if (currentAnimationHash == stateHash)
                return;

            if (animator.HasState(0, stateHash))
            {
                animator.CrossFadeInFixedTime(stateHash, 0.12f);
                currentAnimationHash = stateHash;
            }
        }

        private void FaceVisualToward(Vector3 worldVisualDirection)
        {
            worldVisualDirection.y = 0f;
            if (worldVisualDirection.sqrMagnitude <= 0.0001f)
                return;

            Vector3 rootForward = Quaternion.AngleAxis(-visualYawOffset, Vector3.up) * worldVisualDirection.normalized;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(rootForward, Vector3.up),
                turnSpeed * Time.deltaTime);
        }
    }
}
