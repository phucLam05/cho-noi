using UnityEngine;

namespace ChoNoi.Presentation.NPC
{
    public class NpcBoatPatrol : MonoBehaviour
    {
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float moveSpeed = 1.8f;
        [SerializeField] private float turnSpeed = 3f;
        
        private int currentWaypointIndex;
        [SerializeField] private Animator npcAnimator;
        [SerializeField] private Animator boatAnimator;

        private static readonly int PaddlingHash = Animator.StringToHash("Paddling");
        private bool npcPaddlingStarted;

        public void Configure(Vector3[] points, float speed, Animator animator)
        {
            waypoints = points;
            moveSpeed = speed;
            npcAnimator = animator;
        }

        private Transform hipsTransform;

        private void Start()
        {
            ResolveBoatAnimator();
            ResolveNpcAnimator();

            if (boatAnimator != null)
            {
                boatAnimator.applyRootMotion = false;
                boatAnimator.enabled = true;
            }

            if (npcAnimator != null)
            {
                npcAnimator.applyRootMotion = false;
                FindHips(npcAnimator.transform);
            }
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

        private void ResolveBoatAnimator()
        {
            if (boatAnimator != null)
                return;

            Transform visualRoot = transform.Find("BoatVisualRoot");
            Transform boatTrans = visualRoot != null ? visualRoot.Find("BoatModel") : transform.Find("BoatModel");
            if (boatTrans != null)
            {
                boatAnimator = boatTrans.GetComponent<Animator>();
            }
        }

        private void ResolveNpcAnimator()
        {
            if (npcAnimator == null)
            {
                // Find the PaddlingNpc child explicitly to avoid finding the boat's own animator
                Transform visualRoot = transform.Find("BoatVisualRoot");
                if (visualRoot != null)
                {
                    Transform npcTrans = visualRoot.Find("PaddlingNpc");
                    if (npcTrans != null)
                    {
                        npcAnimator = npcTrans.GetComponentInChildren<Animator>();
                    }
                }

                // Generic fallback targeting the character model specifically
                if (npcAnimator == null)
                {
                    foreach (var anim in GetComponentsInChildren<Animator>())
                    {
                        if (anim.gameObject.name == "NpcModel" || anim.transform.parent.name == "PaddlingNpc")
                        {
                            npcAnimator = anim;
                            break;
                        }
                    }
                }
            }
        }

        public bool destroyOnLastWaypoint;

        private void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;

            if (boatAnimator != null && !boatAnimator.enabled)
            {
                boatAnimator.enabled = true;
            }

            PlayNpcPaddling();

            Vector3 target = waypoints[currentWaypointIndex];
            // Keep the Y coordinate level with the current Y position (water level)
            target.y = transform.position.y;

            Vector3 direction = target - transform.position;
            float distance = direction.magnitude;

            if (distance < 0.8f)
            {
                if (destroyOnLastWaypoint && currentWaypointIndex == waypoints.Length - 1)
                {
                    Debug.Log($"[NpcBoatPatrol] Spawned boat {gameObject.name} reached final waypoint. Destroying.");
                    Destroy(gameObject);
                    return;
                }
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
                return;
            }

            Vector3 moveDir = direction.normalized;
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            if (moveDir.magnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
            }
        }

        private void LateUpdate()
        {
            if (npcAnimator != null)
            {
                // Force the character model (with Animator) to stay anchored to the NPC root position
                // on the boat to prevent any root-motion drift or visual sliding off the boat.
                npcAnimator.transform.localPosition = Vector3.zero;
                npcAnimator.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            }

            if (hipsTransform != null)
            {
                hipsTransform.localPosition = new Vector3(0f, hipsTransform.localPosition.y, 0f);
            }
        }

        private void PlayNpcPaddling()
        {
            if (npcAnimator == null || npcAnimator.runtimeAnimatorController == null)
                return;

            if (!npcAnimator.HasState(0, PaddlingHash))
                return;

            AnimatorStateInfo stateInfo = npcAnimator.GetCurrentAnimatorStateInfo(0);
            if (!npcPaddlingStarted || (!stateInfo.shortNameHash.Equals(PaddlingHash) && !npcAnimator.IsInTransition(0)))
            {
                npcAnimator.CrossFadeInFixedTime(PaddlingHash, 0.08f);
                npcPaddlingStarted = true;
            }
        }
    }
}
