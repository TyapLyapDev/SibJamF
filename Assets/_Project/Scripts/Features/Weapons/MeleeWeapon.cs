using System;
using System.Collections;
using _Project.Scripts.Enemies;
using SibJam.SecurityCameras;
using UnityEngine;
using VContainer;

namespace SibJam.Weapons
{
    public enum IncomingHitResult
    {
        Hit,
        Blocked,
        Parried
    }

    // Меч: по ЛКМ — рубящий удар сферой перед прицелом, урон врагам и удары по камерам; по ПКМ — короткое парирование (кнопку не зажать, есть перезарядка).
    // С руками (_arms) удар наносится в кадр анимации, без них — сразу, а модель машет кодом
    public class MeleeWeapon : MonoBehaviour
    {
        [SerializeField] private Transform _aimTransform;
        [SerializeField] private PlayerArms _arms;
        [SerializeField] private WeaponConfig _config;

        [Header("Без рук: модель и параметры по умолчанию")]
        [SerializeField] private Transform _model;
        [SerializeField, Min(0f)] private float _damage = 50f;
        [SerializeField, Min(0f)] private float _range = 2.5f;
        [SerializeField, Min(0f)] private float _radius = 0.5f;
        [SerializeField, Min(0f)] private float _cooldown = 0.45f;
        [SerializeField] private LayerMask _hitMask = ~0;

        [Header("Блок")]
        [Tooltip("Сколько ударов или снарядов выдерживает блок, последний пробивает его")]
        [SerializeField, Min(1)] private int _guardHits = 2;
        [Tooltip("Если по блоку не били столько секунд — он снова держит все удары")]
        [SerializeField, Min(0f)] private float _guardRecoverDelay = 2f;
        [Tooltip("Сколько нельзя снова поднять блок после пробития")]
        [SerializeField, Min(0f)] private float _guardBreakDuration = 1.2f;

        [Header("Контратака")]
        [Tooltip("Сколько после блока или парирования есть на удар с бонусным уроном")]
        [SerializeField, Min(0f)] private float _counterWindow = 0.8f;

        private IPlayerInputReader _inputReader;
        private bool _wasAttackPressed;
        private int _guardLeft;
        private float _lastGuardHitTime;
        private float _blockLockedUntil;
        private float _counterUntil = -1f;
        private bool _isCounterAttack;
        private float _nextAttackTime;
        private float _blockStartTime;
        private Quaternion _modelRestRotation;

        public event Action Blocked;
        public event Action Parried;
        public event Action GuardBroken;

        public bool IsBlocking { get; private set; }

        public bool IsInParryWindow => IsBlocking && Time.time - _blockStartTime <= ParryWindow;
        public bool IsCounterReady => Time.time <= _counterUntil;

        private float Damage => _config != null ? _config.Damage : _damage;
        private float Range => _config != null ? _config.Range : _range;
        private float Radius => _config != null ? _config.Radius : _radius;
        private float Cooldown => _config != null ? _config.Cooldown : _cooldown;
        private float ParryWindow => _config != null ? _config.ParryWindow : 0.2f;

        [Inject]
        public void Construct(IPlayerInputReader inputReader)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        }

        private void Awake()
        {
            if (_model != null)
                _modelRestRotation = _model.localRotation;

            _guardLeft = _guardHits;

            if (_arms != null && _config != null)
                _arms.Equip(_config);
        }

        private void OnEnable()
        {
            SetModelVisible(true);

            if (_arms != null)
                _arms.AttackHit += DealDamage;
        }

        private void OnDisable()
        {
            SetModelVisible(false);
            SetBlocking(false);

            if (_arms != null)
                _arms.AttackHit -= DealDamage;
        }

        private void Update()
        {
            bool canAct = CursorLocker.IsLocked;

            if (Time.time - _lastGuardHitTime >= _guardRecoverDelay)
                _guardLeft = _guardHits;

            // Блок держится, пока зажата ПКМ; в самом начале — окно парирования. Пробитый блок какое-то время не поднять
            SetBlocking(canAct && _inputReader.IsBlockPressed && Time.time >= _blockLockedUntil && Time.time >= _nextAttackTime);

            bool isAttackPressed = _inputReader.IsAttackPressed;

            // Контратаку можно начать прямо из блока
            if (isAttackPressed && !_wasAttackPressed && canAct && (!IsBlocking || IsCounterReady) && Time.time >= _nextAttackTime)
                Attack();

            _wasAttackPressed = isAttackPressed;
        }

        public void Equip(WeaponConfig config)
        {
            _config = config;

            if (_arms != null && config != null)
                _arms.Equip(config);
        }

        // Для урона по игроку: решает, отбит ли удар
        public IncomingHitResult ResolveIncomingHit()
        {
            if (IsInParryWindow)
            {
                OpenCounterWindow();
                Parried?.Invoke();
                return IncomingHitResult.Parried;
            }

            if (IsBlocking)
            {
                OpenCounterWindow();
                Blocked?.Invoke();
                TakeGuardHit();
                return IncomingHitResult.Blocked;
            }

            return IncomingHitResult.Hit;
        }

        private void OpenCounterWindow() => _counterUntil = Time.time + _counterWindow;

        private void TakeGuardHit()
        {
            _lastGuardHitTime = Time.time;
            _guardLeft--;

            if (_guardLeft > 0)
                return;

            _blockLockedUntil = Time.time + _guardBreakDuration;
            _guardLeft = _guardHits;
            SetBlocking(false);
            GuardBroken?.Invoke();
        }

        private void Attack()
        {
            _nextAttackTime = Time.time + Cooldown;
            _isCounterAttack = IsCounterReady;
            _counterUntil = -1f;
            SetBlocking(false);

            if (_arms != null)
            {
                _arms.PlayAttack();
                return;
            }

            if (_model != null)
                StartCoroutine(Swing());

            DealDamage();
        }

        private void DealDamage()
        {
            var hits = Physics.SphereCastAll(_aimTransform.position, Radius, _aimTransform.forward, Range, _hitMask, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    enemy.TakeDamage(_isCounterAttack ? Damage * enemy.CounterDamageMultiplier : Damage);
                    continue;
                }

                SecurityCamera securityCamera = hit.collider.GetComponentInParent<SecurityCamera>();

                if (securityCamera != null)
                    securityCamera.TakeHit();
            }
        }

        private void SetBlocking(bool isBlocking)
        {
            if (IsBlocking == isBlocking)
                return;

            IsBlocking = isBlocking;

            if (isBlocking)
                _blockStartTime = Time.time;

            if (_arms != null)
                _arms.SetBlocking(isBlocking);
        }

        private IEnumerator Swing()
        {
            const float duration = 0.25f;
            // Клинок вдоль локальной оси Y: рубим сверху-справа вниз-влево
            Quaternion swung = _modelRestRotation * Quaternion.Euler(85f, 0f, -35f);

            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = time / duration;
                _model.localRotation = Quaternion.Slerp(_modelRestRotation, swung, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            _model.localRotation = _modelRestRotation;
        }

        private void SetModelVisible(bool visible)
        {
            if (_model != null)
                _model.gameObject.SetActive(visible);

            if (_arms != null)
                _arms.gameObject.SetActive(visible);
        }
    }
}
