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

        public void Configure(Vector3[] points, float speed)
        {
            localWaypoints = points;
            moveSpeed = speed;
        }

        private void Start()
        {
            origin = transform.position;
        }

        private void Update()
        {
            if (isPaused)
                return;

            if (localWaypoints == null || localWaypoints.Length == 0)
                return;

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                return;
            }

            Vector3 target = origin + localWaypoints[waypointIndex];
            Vector3 delta = target - transform.position;
            delta.y = 0f;

            if (delta.magnitude < 0.2f)
            {
                waypointIndex = (waypointIndex + 1) % localWaypoints.Length;
                waitTimer = waitAtPoint;
                return;
            }

            Vector3 direction = delta.normalized;
            transform.position += direction * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), turnSpeed * Time.deltaTime);
        }

        public void SetPaused(bool paused)
        {
            isPaused = paused;
        }
    }
}
