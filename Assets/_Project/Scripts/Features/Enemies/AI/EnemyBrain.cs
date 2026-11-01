using SibJam.Combat;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    public enum EnemyState
    {
        Idle,
        Chase,
        Attack,
        Stagger,
        Dead
    }

    // Простая машина состояний врага. Конкретное поведение атаки — в компоненте IEnemyAttack
    [RequireComponent(typeof(Enemy), typeof(EnemyMotor))]
    public class EnemyBrain : MonoBehaviour
    {
        [SerializeField, Min(0.02f)] private float _repathInterval = 0.2f;
        [SerializeField, Min(0f)] private float _aggroRange = 40f;
        [Tooltip("Сколько враг стоит оглушённым после того, как его удар отпарировали")]
        [SerializeField, Min(0f)] private float _parryStaggerDuration = 1.2f;

        private Enemy _enemy;
        private EnemyMotor _motor;
        private IEnemyAttack _attack;
        private IEnemyTarget _target;
        private EnemyConfig _config;

        private float _repathTimer;
        private float _staggerTimer;

        public EnemyState State { get; private set; } = EnemyState.Idle;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _motor = GetComponent<EnemyMotor>();
            _attack = GetComponent<IEnemyAttack>();
        }

        private void OnEnable()
        {
            _enemy.Died += OnDied;
            if (_attack != null)
                _attack.Parried += OnAttackParried;
        }

        private void OnDisable()
        {
            _enemy.Died -= OnDied;
            if (_attack != null)
                _attack.Parried -= OnAttackParried;
        }

        public void Init(EnemyConfig config, IEnemyTarget target)
        {
            _config = config;
            _target = target;
            _motor.Init(config.MoveSpeed);
            _attack?.Init(config, _enemy.StatsMultiplier);
            SetState(EnemyState.Idle);
        }

        // Вызывается при успешном парировании удара этого врага
        public void Stagger(float duration)
        {
            if (State == EnemyState.Dead) return;

            _attack?.Cancel();
            _staggerTimer = duration;
            SetState(EnemyState.Stagger);
        }

        private void Update()
        {
            if (_config == null || State == EnemyState.Dead) return;

            var deltaTime = Time.deltaTime;

            switch (State)
            {
                case EnemyState.Idle: TickIdle(); break;
                case EnemyState.Chase: TickChase(deltaTime); break;
                case EnemyState.Attack: TickAttack(deltaTime); break;
                case EnemyState.Stagger: TickStagger(deltaTime); break;
            }
        }

        private void TickIdle()
        {
            if (HasTarget() && DistanceToTarget() <= _aggroRange)
                SetState(EnemyState.Chase);
        }

        private void TickChase(float deltaTime)
        {
            if (!HasTarget())
            {
                SetState(EnemyState.Idle);
                return;
            }

            if (DistanceToTarget() <= _config.AttackRange)
            {
                _motor.Stop();
                _motor.FaceTowards(_target.Position, deltaTime);

                if (_attack != null && _attack.CanStart(_target))
                    SetState(EnemyState.Attack);

                return;
            }

            _repathTimer -= deltaTime;
            if (_repathTimer <= 0f)
            {
                _repathTimer = _repathInterval;
                _motor.MoveTo(_target.Position);
            }
        }

        private void TickAttack(float deltaTime)
        {
            // Доворачиваемся только до начала удара, чтобы от него можно было уйти перебежкой
            if (_attack.Phase == AttackPhase.Windup && HasTarget())
                _motor.FaceTowards(_target.Position, deltaTime);

            _attack.Tick(deltaTime);

            if (!_attack.IsRunning)
                SetState(EnemyState.Chase);
        }

        private void TickStagger(float deltaTime)
        {
            _staggerTimer -= deltaTime;
            if (_staggerTimer <= 0f)
                SetState(EnemyState.Chase);
        }

        private void SetState(EnemyState state)
        {
            State = state;

            switch (state)
            {
                case EnemyState.Chase:
                    _repathTimer = 0f;
                    break;
                case EnemyState.Attack:
                    _motor.Stop();
                    _attack.Begin(_target);
                    break;
                case EnemyState.Idle:
                case EnemyState.Stagger:
                    _motor.Stop();
                    break;
            }
        }

        private void OnAttackParried() => Stagger(_parryStaggerDuration);

        private void OnDied(Enemy enemy)
        {
            _attack?.Cancel();
            State = EnemyState.Dead;
            _motor.Disable();
        }

        private bool HasTarget() => _target != null && _target.IsAlive;

        private float DistanceToTarget() => Vector3.Distance(transform.position, _target.Position);
    }
}
