using SibJam.Combat;
using SibJam.Feedback;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Манекен на стартовой локации: ведёт себя как враг, но не ходит — только поворачивается к игроку и бьёт, когда тот рядом
    [RequireComponent(typeof(Enemy))]
    public class TrainingDummy : MonoBehaviour
    {
        [SerializeField] private EnemyConfig _config;
        [SerializeField] private PlayerTarget _target;
        [SerializeField, Min(0f)] private float _turnSpeed = 360f;
        [Tooltip("Сколько манекен стоит оглушённым после того, как его удар отпарировали")]
        [SerializeField, Min(0f)] private float _parryStaggerDuration = 1.2f;

        private Enemy _enemy;
        private IEnemyAttack _attack;
        private float _staggerTimer;
        private bool _initialized;

        public EnemyConfig Config => _config;
        public bool IsInitialized => _initialized;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _attack = GetComponent<IEnemyAttack>();
        }

        private void Start()
        {
            if (_initialized) return;
            if (_config == null)
            {
                Debug.LogError("[TrainingDummy] Enemy config is missing", this);
                return;
            }

            TrainingDummyWaveSpawner.CreateForScene(this);
        }

        public void Init(float statsMultiplier)
        {
            if (_target == null)
                _target = FindFirstObjectByType<PlayerTarget>();

            _initialized = true;
            _enemy.Init(_config, _target, statsMultiplier);
            _attack?.Init(_config, _enemy.StatsMultiplier);

            if (!TryGetComponent(out EnemySpawnHologram hologram))
                hologram = gameObject.AddComponent<EnemySpawnHologram>();
            hologram.Play();
        }

        private void OnEnable()
        {
            if (_attack != null)
                _attack.Parried += OnAttackParried;
        }

        private void OnDisable()
        {
            if (_attack != null)
            {
                _attack.Parried -= OnAttackParried;
                _attack.Cancel();
            }
        }

        private void Update()
        {
            if (_config == null || _attack == null || _enemy.IsDead) return;

            var deltaTime = Time.deltaTime;

            if (_staggerTimer > 0f)
            {
                _staggerTimer -= deltaTime;
                return;
            }

            if (_attack.IsRunning)
            {
                // Доворачиваемся только до начала удара, чтобы от него можно было уйти
                if (_attack.Phase == AttackPhase.Windup && HasTarget())
                    FaceTowards(_target.Position, deltaTime);

                _attack.Tick(deltaTime);
                return;
            }

            if (!HasTarget() || Vector3.Distance(transform.position, _target.Position) > _config.AttackRange) return;

            FaceTowards(_target.Position, deltaTime);

            if (_attack.CanStart(_target))
                _attack.Begin(_target);
        }

        private void OnAttackParried()
        {
            _attack.Cancel();
            _staggerTimer = _parryStaggerDuration;
        }

        private void FaceTowards(Vector3 point, float deltaTime)
        {
            var direction = point - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f) return;

            var targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _turnSpeed * deltaTime);
        }

        private bool HasTarget() => _target != null && _target.IsAlive;
    }
}
