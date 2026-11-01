using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;

        private NavMeshAgent _agent;

        public float Speed => _agent.velocity.magnitude;
        public bool IsOnNavMesh => _agent.isOnNavMesh;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
        }

        public void Init(float moveSpeed)
        {
            _agent.speed = moveSpeed;
        }

        public void MoveTo(Vector3 destination)
        {
            if (!_agent.isOnNavMesh) return;

            _agent.isStopped = false;
            _agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (!_agent.isOnNavMesh) return;

            _agent.isStopped = true;
            _agent.ResetPath();
        }

        public void FaceTowards(Vector3 point, float deltaTime)
        {
            var direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            var targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * deltaTime);
        }

        public void Disable()
        {
            Stop();
            _agent.enabled = false;
        }
    }
}
