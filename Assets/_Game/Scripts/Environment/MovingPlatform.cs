using UnityEngine;

namespace SunlessReach.Environment
{
    public class MovingPlatform : MonoBehaviour
    {
        [SerializeField] private Vector3[] waypoints;
        [SerializeField] private float speed = 3f;
        [SerializeField] private float waitTime = 0.5f;

        private int _currentWaypoint;
        private float _waitTimer;
        private Vector3 _lastPosition;

        private void Start()
        {
            if (waypoints.Length == 0)
            {
                waypoints = new[] { transform.position, transform.position + Vector3.right * 5 };
            }
            transform.position = waypoints[0];
            _lastPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (_waitTimer > 0)
            {
                _waitTimer -= Time.fixedDeltaTime;
                return;
            }

            _lastPosition = transform.position;
            Vector3 target = waypoints[_currentWaypoint];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.fixedDeltaTime);

            if (Vector3.Distance(transform.position, target) < 0.01f)
            {
                _currentWaypoint = (_currentWaypoint + 1) % waypoints.Length;
                _waitTimer = waitTime;
            }
        }

        private void OnCollisionStay(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                Vector3 delta = transform.position - _lastPosition;
                collision.transform.position += delta;
            }
        }
    }
}
