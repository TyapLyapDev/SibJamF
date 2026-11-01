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

    // Меч: по ЛКМ — рубящий удар сферой перед прицелом, урон врагам и удары по камерам.
    // По клику ПКМ — парирование: руки поднимают блок, держат и опускают; в начале окно парирования, дальше просто блок.
    // Пока парирование не доиграло, новый клик не срабатывает; зажатие ничего не продлевает.
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

        [Header("Парирование")]
        [Tooltip("Сколько секунд блок поднят после клика (подъём 0.25 с + удержание)")]
        [SerializeField, Min(0.05f)] private float _parryRaisedTime = 0.4f;
        [Tooltip("Сколько секунд руки опускаются, прежде чем можно кликнуть снова")]
        [SerializeField, Min(0f)] private float _parryRecoveryTime = 0.25f;

        private IPlayerInputReader _inputReader;
        private Coroutine _parry;
        private bool _wasAttackPressed;
        private bool _wasBlockPressed;
        private float _nextAttackTime;
        private float _blockStartTime;
        private Quaternion _modelRestRotation;

        public event Action Blocked;
        public event Action Parried;

        public bool IsBlocking { get; private set; }

        public bool IsInParryWindow => IsBlocking && Time.time - _blockStartTime <= ParryWindow;

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

            if (_parry != null)
                StopCoroutine(_parry);

            _parry = null;
            SetBlocking(false);

            if (_arms != null)
                _arms.AttackHit -= DealDamage;
        }

        private void Update()
        {
            bool canAct = CursorLocker.IsLocked;

            bool isBlockPressed = _inputReader.IsBlockPressed;

            if (isBlockPressed && !_wasBlockPressed && canAct && _parry == null)
                _parry = StartCoroutine(Parry());

            _wasBlockPressed = isBlockPressed;

            bool isAttackPressed = _inputReader.IsAttackPressed;

            if (isAttackPressed && !_wasAttackPressed && canAct && _parry == null && Time.time >= _nextAttackTime)
                Attack();

            _wasAttackPressed = isAttackPressed;
        }

        private IEnumerator Parry()
        {
            SetBlocking(true);
            yield return new WaitForSeconds(_parryRaisedTime);
            SetBlocking(false);
            yield return new WaitForSeconds(_parryRecoveryTime);
            _parry = null;
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
                Parried?.Invoke();
                return IncomingHitResult.Parried;
            }

            if (IsBlocking)
            {
                Blocked?.Invoke();
                return IncomingHitResult.Blocked;
            }

            return IncomingHitResult.Hit;
        }

        private void Attack()
        {
            _nextAttackTime = Time.time + Cooldown;

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
                    enemy.TakeDamage(Damage);
                    continue;
                }

                SecurityCamera securityCamera = hit.collider.GetComponentInParent<SecurityCamera>();

                if (securityCamera != null)
                {
                    securityCamera.TakeHit();
                    continue;
                }

                IMeleeHitTarget target = hit.collider.GetComponentInParent<IMeleeHitTarget>();

                // У попаданий, которые начались внутри сферы, точка нулевая — берём ближайшую точку коллайдера
                if (target != null)
                    target.TakeMeleeHit(hit.point != Vector3.zero ? hit.point : hit.collider.ClosestPoint(_aimTransform.position));
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
