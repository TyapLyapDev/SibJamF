using System;
using SibJam.Combat;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Тычок ближника. Урон проверяется один раз в начале Active: если игрок ушёл из зоны — промах
    public class MeleeAttack : MonoBehaviour, IEnemyAttack
    {
        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float _windupDuration = 0.9f;
        [SerializeField, Min(0f)] private float _activeDuration = 0.15f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.5f;

        [Header("Hit zone")]
        [SerializeField, Min(0f)] private float _hitReach = 2f;
        [SerializeField, Range(0f, 180f)] private float _hitAngle = 60f;

        [Header("Feedback")]
        [Tooltip("Включается на время замаха — подсказка игроку, что сейчас ударят")]
        [SerializeField] private GameObject _windupIndicator;
        [SerializeField] private EnemyAnimator _animator;
        [Tooltip("Секунда клипа атаки, на которой рука попадает. Клип ускоряется так, чтобы этот момент совпал с концом замаха")]
        [SerializeField, Min(0.01f)] private float _clipImpactTime = 1.4f;

        private EnemyConfig _config;
        private float _damage;
        private IEnemyTarget _target;
        private float _phaseTimer;
        private float _cooldownTimer;

        public event Action Parried;

        public AttackPhase Phase { get; private set; } = AttackPhase.None;
        public bool IsRunning => Phase != AttackPhase.None;

        private void Awake() => SetIndicator(false);

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        private void OnDestroy() => EnemyAttackTokens.Release(this);

        public void Init(EnemyConfig config, float statsMultiplier)
        {
            _config = config;
            _damage = config.Damage * statsMultiplier;
        }

        public bool CanReach(IEnemyTarget target) => true;

        public bool CanStart(IEnemyTarget target)
        {
            return _config != null && !IsRunning && _cooldownTimer <= 0f && EnemyAttackTokens.TryTake(this);
        }

        public void Begin(IEnemyTarget target)
        {
            _target = target;
            EnterPhase(AttackPhase.Windup, _windupDuration);
            SetIndicator(true);

            if (_animator != null)
                _animator.PlayAttack(_clipImpactTime / _windupDuration);
        }

        public void Tick(float deltaTime)
        {
            if (!IsRunning) return;

            _phaseTimer -= deltaTime;
            if (_phaseTimer > 0f) return;

            switch (Phase)
            {
                case AttackPhase.Windup:
                    SetIndicator(false);
                    EnterPhase(AttackPhase.Active, _activeDuration);
                    TryHit();
                    break;
                case AttackPhase.Active:
                    EnterPhase(AttackPhase.Recovery, _recoveryDuration);
                    break;
                case AttackPhase.Recovery:
                    Finish();
                    break;
            }
        }

        public void Cancel()
        {
            if (!IsRunning) return;
            Finish();
        }

        private void TryHit()
        {
            if (_target == null || !_target.IsAlive || !IsInHitZone(_target.Position)) return;

            var direction = (_target.Position - transform.position).normalized;
            var result = _target.TakeHit(new HitInfo(_damage, gameObject, direction, parryable: true));

            if (result == HitResult.Parried)
            {
                Finish();
                Parried?.Invoke();
            }
        }

        private bool IsInHitZone(Vector3 point)
        {
            var toTarget = point - transform.position;
            toTarget.y = 0f;
            return toTarget.magnitude <= _hitReach && Vector3.Angle(transform.forward, toTarget) <= _hitAngle;
        }

        private void EnterPhase(AttackPhase phase, float duration)
        {
            Phase = phase;
            _phaseTimer = duration;
        }

        private void Finish()
        {
            Phase = AttackPhase.None;
            _target = null;
            _cooldownTimer = _config != null ? _config.AttackCooldown : 0f;
            SetIndicator(false);
            EnemyAttackTokens.Release(this);

            if (_animator != null)
                _animator.EndAttack();
        }

        private void SetIndicator(bool active)
        {
            if (_windupIndicator != null)
                _windupIndicator.SetActive(active);
        }
    }
}
