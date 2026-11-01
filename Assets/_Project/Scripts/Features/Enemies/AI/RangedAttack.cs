using System;
using SibJam.Combat;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Выстрел стрелка: прицеливание (подсказка) -> выстрел снарядом в точку, где игрок был в конце прицеливания -> откат.
    // Стреляет только при прямой видимости, поэтому может работать с верхних ярусов
    public class RangedAttack : MonoBehaviour, IEnemyAttack
    {
        [Header("Timing")]
        [SerializeField, Min(0.05f)] private float _windupDuration = 0.8f;
        [SerializeField, Min(0f)] private float _recoveryDuration = 0.6f;

        [Header("Projectile")]
        [SerializeField] private Projectile _projectilePrefab;
        [SerializeField] private Transform _muzzle;
        [SerializeField, Min(0.1f)] private float _projectileSpeed = 14f;

        [Header("Line of sight")]
        [Tooltip("Что загораживает обзор. Сам стрелок и игрок в проверке не учитываются")]
        [SerializeField] private LayerMask _obstacleMask = ~0;

        [Header("Feedback")]
        [SerializeField] private GameObject _windupIndicator;
        [SerializeField] private EnemyAnimator _animator;
        [Tooltip("Секунда клипа стрельбы, на которой происходит выстрел. Клип ускоряется так, чтобы этот момент совпал с концом прицеливания")]
        [SerializeField, Min(0.01f)] private float _clipFireTime = 0.5f;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private EnemyConfig _config;
        private float _damage;
        private IEnemyTarget _target;
        private float _phaseTimer;
        private float _cooldownTimer;

        public event Action Parried
        {
            add { }
            remove { }
        }

        public AttackPhase Phase { get; private set; } = AttackPhase.None;
        public bool IsRunning => Phase != AttackPhase.None;

        private Vector3 MuzzlePosition => _muzzle != null ? _muzzle.position : transform.position + Vector3.up * 1.4f;

        private void Awake() => SetIndicator(false);

        private void Update()
        {
            if (_cooldownTimer > 0f)
                _cooldownTimer -= Time.deltaTime;
        }

        public void Init(EnemyConfig config, float statsMultiplier)
        {
            _config = config;
            _damage = config.Damage * statsMultiplier;
        }

        public bool CanReach(IEnemyTarget target) => HasLineOfSight(target);

        public bool CanStart(IEnemyTarget target) =>
            _config != null && !IsRunning && _cooldownTimer <= 0f && HasLineOfSight(target);

        public void Begin(IEnemyTarget target)
        {
            _target = target;
            Phase = AttackPhase.Windup;
            _phaseTimer = _windupDuration;
            SetIndicator(true);

            if (_animator != null)
                _animator.PlayAttack(_clipFireTime / _windupDuration);
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
                    Fire();
                    Phase = AttackPhase.Recovery;
                    _phaseTimer = _recoveryDuration;
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

        private void Fire()
        {
            if (_projectilePrefab == null || _target == null || !_target.IsAlive) return;

            var origin = MuzzlePosition;
            var projectile = Instantiate(_projectilePrefab, origin, Quaternion.identity);
            projectile.Launch(_target.AimPoint - origin, _projectileSpeed, _damage, gameObject);
        }

        private bool HasLineOfSight(IEnemyTarget target)
        {
            if (target == null || !target.IsAlive) return false;

            var origin = MuzzlePosition;
            var toTarget = target.AimPoint - origin;
            var distance = toTarget.magnitude;
            var count = Physics.RaycastNonAlloc(origin, toTarget / distance, _hits, distance, _obstacleMask, QueryTriggerInteraction.Ignore);

            for (var i = 0; i < count; i++)
            {
                var hitTransform = _hits[i].collider.transform;

                if (hitTransform.IsChildOf(transform) || hitTransform.GetComponentInParent<IEnemyTarget>() != null)
                    continue;

                return false;
            }

            return true;
        }

        private void Finish()
        {
            Phase = AttackPhase.None;
            _target = null;
            _cooldownTimer = _config != null ? _config.AttackCooldown : 0f;
            SetIndicator(false);

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
