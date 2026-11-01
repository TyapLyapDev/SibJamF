using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace _Project.Scripts.Enemies
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyMotor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _turnSpeed = 540f;

        [Header("Отбрасывание")]
        [SerializeField, Min(0f)] private float _knockbackGravity = 30f;
        [Tooltip("Насколько быстро гасится горизонтальная скорость отлёта на земле")]
        [SerializeField, Min(0f)] private float _knockbackGroundDrag = 10f;
        [SerializeField, Min(0.1f)] private float _knockbackMaxDuration = 2f;

        [Header("Падение за пределы локации")]
        [Tooltip("Если в этом радиусе нет навмеша, связанного с местом удара (крыши не считаются), — враг вылетел за пределы и падает")]
        [SerializeField, Min(0.1f)] private float _offMeshRadius = 1.5f;
        [Tooltip("На сколько ниже последней земли нужно упасть, чтобы умереть")]
        [SerializeField, Min(0.1f)] private float _fallKillDepth = 5f;

        public event System.Action FellOff;

        private NavMeshAgent _agent;
        private Coroutine _knockbackRoutine;
        private bool _isDisabled;
        private NavMeshPath _connectivityPath;

        public bool IsKnockedBack { get; private set; }

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

        // Расталкивание: игрок проходит сквозь врага, тот сдвигается в сторону по навмешу
        public void Shove(Vector3 offset)
        {
            if (_isDisabled || IsKnockedBack || !_agent.enabled || !_agent.isOnNavMesh) return;

            offset.y = 0f;
            _agent.Move(offset);
        }

        public void Disable()
        {
            _isDisabled = true;

            if (_knockbackRoutine != null)
                StopCoroutine(_knockbackRoutine);

            IsKnockedBack = false;
            Stop();
            _agent.enabled = false;
        }

        // Отлёт от удара: агент выключается, тело летит по баллистике и приземляется обратно на навмеш
        public void Knockback(Vector3 velocity)
        {
            if (_isDisabled || (!_agent.enabled && !IsKnockedBack)) return;

            if (_knockbackRoutine != null)
                StopCoroutine(_knockbackRoutine);

            _knockbackRoutine = StartCoroutine(KnockbackRoutine(velocity));
        }

        private IEnumerator KnockbackRoutine(Vector3 velocity)
        {
            IsKnockedBack = true;
            Stop();
            _agent.enabled = false;

            Vector3 position = transform.position;
            Vector3 origin = position;
            if (NavMesh.SamplePosition(position, out NavMeshHit originHit, 2f, NavMesh.AllAreas))
                origin = originHit.position;
            float groundY = position.y;
            float lastGroundY = groundY;
            bool isAirborne = velocity.y > 0f;

            for (float time = 0f; time < _knockbackMaxDuration || float.IsNegativeInfinity(groundY); time += Time.deltaTime)
            {
                float deltaTime = Time.deltaTime;
                Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
                Vector3 step = horizontal * deltaTime;

                // Упёрлись в стену — горизонтальный отлёт гасится
                if (step.sqrMagnitude > 0f && Physics.Raycast(position + Vector3.up * 0.5f, step.normalized, step.magnitude + 0.3f, ~0, QueryTriggerInteraction.Ignore))
                {
                    velocity.x = 0f;
                    velocity.z = 0f;
                    step = Vector3.zero;
                }

                position += step;

                // Под ногами нет навмеша — вылетел за пределы локации, дальше только падение
                if (TrySampleConnected(position, origin, _offMeshRadius, out NavMeshHit navHit))
                    groundY = navHit.position.y;
                else
                {
                    isAirborne = true;
                    groundY = float.NegativeInfinity;
                }

                if (position.y < lastGroundY - _fallKillDepth)
                {
                    transform.position = position;
                    IsKnockedBack = false;
                    _knockbackRoutine = null;
                    FellOff?.Invoke();
                    yield break;
                }

                if (!float.IsNegativeInfinity(groundY))
                    lastGroundY = groundY;

                if (isAirborne)
                {
                    velocity.y -= _knockbackGravity * deltaTime;
                    position.y += velocity.y * deltaTime;

                    if (velocity.y <= 0f && position.y <= groundY)
                    {
                        position.y = groundY;
                        isAirborne = false;
                    }
                }
                else
                {
                    position.y = groundY;
                    velocity = Vector3.MoveTowards(horizontal, Vector3.zero, _knockbackGroundDrag * deltaTime);

                    if (velocity.sqrMagnitude < 0.04f)
                        break;
                }

                transform.position = position;
                yield return null;
            }

            if (!TrySampleConnected(transform.position, origin, 5f, out NavMeshHit landHit))
            {
                IsKnockedBack = false;
                _knockbackRoutine = null;
                FellOff?.Invoke();
                yield break;
            }

            transform.position = landHit.position;

            _agent.enabled = true;
            _agent.Warp(transform.position);
            IsKnockedBack = false;
            _knockbackRoutine = null;
        }

        // Навмеш есть и на крышах — засчитываем только тот, что связан путём с местом удара
        private bool TrySampleConnected(Vector3 position, Vector3 origin, float radius, out NavMeshHit hit)
        {
            if (!NavMesh.SamplePosition(position, out hit, radius, NavMesh.AllAreas))
                return false;

            _connectivityPath ??= new NavMeshPath();
            return NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, _connectivityPath)
                && _connectivityPath.status == NavMeshPathStatus.PathComplete;
        }
    }
}
