using UnityEngine;

namespace ChoNoi.Presentation.NPC
{
    public class SimpleNpcWander : MonoBehaviour
    {
        [SerializeField] private Vector3[] localWaypoints;
        [SerializeField] private float moveSpeed = 1.6f;
        [SerializeField] private float turnSpeed = 5f;
        [SerializeField] private float waitAtPoint = 1.2f;

        private Vector3 origin;
        private int waypointIndex;
        private float waitTimer;
        private bool isPaused;
        private Animator animator;

        [Header("Animation Options")]
        [SerializeField] private float waveDistance = 6f;
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
            origin = transform.position;
            animator = GetComponentInChildren<Animator>();
            
            // Randomize NPC behavior options for variety
            useHappyIdle = (Random.value > 0.5f);
            useTwoHandWave = (Random.value > 0.5f);
        }

        private void FindPlayer()
        {
            if (playerTransform == null)
            {
                var playerObj = GameObject.Find("PlayerOnFoot");
                if (playerObj != null && playerObj.activeInHierarchy)
                {
                    playerTransform = playerObj.transform;
                }
                else
                {
                    playerObj = GameObject.Find("PlayerBoat");
                    if (playerObj != null)
                    {
                        playerTransform = playerObj.transform;
                    }
                }
            }
        }

        private void Update()
        {
            FindPlayer();

            if (animator != null)
            {
                animator.applyRootMotion = false; // Force disable root motion to prevent jumping/glitching positions
            }

            // Proximity interaction: Check if player is nearby
            bool isNearPlayer = false;
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist < waveDistance)
                {
                    isNearPlayer = true;
                }
            }

            if (isNearPlayer)
            {
                // Play waving animation and look at player
                string waveAnim = useTwoHandWave ? "Waving_twohands" : "Waving_onehand";
                if (animator != null) animator.Play(waveAnim);

                Vector3 lookDir = playerTransform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.magnitude > 0.1f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir, Vector3.up), turnSpeed * Time.deltaTime);
                }
                return;
            }

            if (isPaused)
            {
                string idleAnim = useHappyIdle ? "Happy Idle" : "Neutral Idle";
                if (animator != null) animator.Play(idleAnim);
                return;
            }

            if (localWaypoints == null || localWaypoints.Length == 0)
                return;

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                string idleAnim = useHappyIdle ? "Happy Idle" : "Neutral Idle";
                if (animator != null) animator.Play(idleAnim);
                return;
            }

            Vector3 target = origin + localWaypoints[waypointIndex];
            Vector3 delta = target - transform.position;
            delta.y = 0f;

            if (delta.magnitude < 0.2f)
            {
                waypointIndex = (waypointIndex + 1) % localWaypoints.Length;
                waitTimer = waitAtPoint;
                string idleAnim = useHappyIdle ? "Happy Idle" : "Neutral Idle";
                if (animator != null) animator.Play(idleAnim);
                return;
            }

            Vector3 direction = delta.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed * Time.deltaTime);
            if (animator != null) animator.Play("Walking");
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
    }
}
