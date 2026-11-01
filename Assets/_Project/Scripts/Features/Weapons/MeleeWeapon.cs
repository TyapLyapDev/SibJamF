using System;
using System.Collections;
using _Project.Scripts.Enemies;
using SibJam.Feedback;
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

        [Header("Парирование ударом")]
        [Tooltip("Сколько секунд от начала замаха клинок отбивает снаряды и удары, как блок в тайминг")]
        [SerializeField, Min(0f)] private float _swingParryWindow = 0.12f;

        [Header("Отбивание снарядов ударом")]
        [Tooltip("Минимальное расстояние, с которого удар отбивает снаряд")]
        [SerializeField, Min(0f)] private float _deflectReach = 3.5f;
        [Tooltip("Удар отбивает снаряд, который долетит до игрока не позже чем через столько секунд: быстрые снаряды ловятся издалека")]
        [SerializeField, Min(0f)] private float _deflectTime = 0.4f;
        [Tooltip("Угол от взгляда до снаряда, в котором его можно отбить")]
        [SerializeField, Range(0f, 180f)] private float _deflectAngle = 70f;
        [Tooltip("Сколько секунд после нажатия удар ещё ловит подлетающие снаряды: нажал чуть раньше — всё равно отбил")]
        [SerializeField, Min(0f)] private float _deflectBuffer = 0.2f;
        [Tooltip("Множитель урона снаряда, отбитого ударом из блока")]
        [SerializeField, Min(1f)] private float _megaDeflectMultiplier = 2f;
        [Tooltip("Во сколько раз ускоряется анимация удара, когда он отбил снаряд, чтобы клинок успел к нему")]
        [SerializeField, Min(1f)] private float _deflectAnimationSpeed = 1.8f;

        [Header("Отбрасывание")]
        [SerializeField, Min(0f)] private float _knockbackSpeed = 7f;
        [SerializeField, Min(0f)] private float _knockbackLift = 2f;
        [SerializeField, Min(0f)] private float _knockbackStagger = 0.25f;

        [Header("Удар с прыжка")]
        [SerializeField, Min(1f)] private float _jumpDamageMultiplier = 2f;
        [Tooltip("С какой скоростью удар в прыжке тянет игрока к земле")]
        [SerializeField, Min(0f)] private float _jumpSlamSpeed = 18f;
        [SerializeField, Min(0f)] private float _jumpKnockbackSpeed = 14f;
        [SerializeField, Min(0f)] private float _jumpKnockbackLift = 8f;
        [SerializeField, Min(0f)] private float _jumpKnockbackStagger = 0.6f;

        private IPlayerInputReader _inputReader;
        private IGroundDetector _groundDetector;
        private IGravity _gravity;
        private bool _isJumpAttack;
        private bool _wasAttackPressed;
        private int _guardLeft;
        private float _lastGuardHitTime;
        private float _blockLockedUntil;
        private float _counterUntil = -1f;
        private bool _isCounterAttack;
        private float _nextAttackTime;
        private float _blockStartTime;
        private float _attackStartTime = -1f;
        private bool _isSwingPending;
        private float _deflectUntil = -1f;
        private bool _isMegaDeflect;
        private bool _hasDeflected;
        private Quaternion _modelRestRotation;

        public event Action Blocked;
        public event Action Parried;
        public event Action GuardBroken;
        public event Action Swung;
        // Удар отбил снаряд: true — мега-отражение из блока
        public event Action<bool> Deflected;
        // Попал ли замах хоть по одному врагу: (контратака, убил)
        public event Action<bool, bool> HitLanded;

        public bool IsBlocking { get; private set; }

        public bool IsInParryWindow => (IsBlocking && Time.time - _blockStartTime <= ParryWindow)
            || (_isSwingPending && Time.time - _attackStartTime <= _swingParryWindow);
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
            _groundDetector = GetComponentInParent<IGroundDetector>();
            _gravity = GetComponentInParent<IGravity>();

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
            _isSwingPending = false;

            if (_arms != null)
                _arms.AttackHit -= DealDamage;
        }

        private void Update()
        {
            bool canAct = CursorLocker.IsLocked;

            if (Time.time - _lastGuardHitTime >= _guardRecoverDelay)
                _guardLeft = _guardHits;

            // Страховка: если событие удара из клипа так и не пришло, замах не держит блок дольше перезарядки
            if (_isSwingPending && Time.time >= _nextAttackTime)
                _isSwingPending = false;

            bool isAttackPressed = _inputReader.IsAttackPressed;

            if (Time.time <= _deflectUntil)
                TryDeflectProjectiles();

            // Удар можно начать прямо из блока: руки сразу кроссфейдятся в замах. После блока или парирования он идёт как контратака
            if (isAttackPressed && !_wasAttackPressed && canAct && Time.time >= _nextAttackTime)
                Attack();

            // Блок держится, пока зажата ПКМ; в самом начале — окно парирования. Пробитый блок какое-то время не поднять,
            // а во время замаха — только после того, как клинок нанёс удар, иначе блок съедает атаку
            SetBlocking(canAct && _inputReader.IsBlockPressed && Time.time >= _blockLockedUntil && !_isSwingPending);

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
            // Блок проверяем до SetBlocking(false): удар из блока отбивает снаряды с двойным уроном
            _isMegaDeflect = IsBlocking;
            _hasDeflected = false;
            _deflectUntil = Time.time + _deflectBuffer;

            _nextAttackTime = Time.time + Cooldown;
            _isCounterAttack = IsCounterReady;
            _isJumpAttack = _groundDetector != null && !_groundDetector.HasGround();

            if (_isJumpAttack)
                _gravity?.Slam(_jumpSlamSpeed);
            _counterUntil = -1f;
            _attackStartTime = Time.time;
            _isSwingPending = true;
            SetBlocking(false);
            Swung?.Invoke();

            if (_arms != null)
                _arms.PlayAttack();

            TryDeflectProjectiles();

            if (_arms != null)
                return;

            if (_model != null)
                StartCoroutine(Swing());

            DealDamage();
        }

        private void DealDamage()
        {
            _isSwingPending = false;
            var hits = Physics.SphereCastAll(_aimTransform.position, Radius, _aimTransform.forward, Range, _hitMask, QueryTriggerInteraction.Ignore);
            bool hasHit = false;
            bool hasKill = false;

            foreach (RaycastHit hit in hits)
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    bool wasAlive = !enemy.IsDead;
                    float damage = _isCounterAttack ? Damage * enemy.CounterDamageMultiplier : Damage;

                    if (_isJumpAttack)
                        damage *= _jumpDamageMultiplier;

                    enemy.TakeDamage(damage);

                    if (wasAlive)
                    {
                        Vector3 point = hit.point != Vector3.zero ? hit.point : hit.collider.ClosestPoint(_aimTransform.position);
                        ImpactFx.Hit(point, -_aimTransform.forward, _isJumpAttack || _isCounterAttack);

                        if (_isJumpAttack)
                            ImpactFx.Slam(enemy.transform.position);

                        if (!enemy.IsDead && enemy.TryGetComponent(out EnemyBrain brain))
                            KnockBack(brain);
                    }

                    hasHit |= wasAlive;
                    hasKill |= wasAlive && enemy.IsDead;
                    continue;
                }

                SecurityCamera securityCamera = hit.collider.GetComponentInParent<SecurityCamera>();

                if (securityCamera != null)
                {
                    securityCamera.TakeHit(_isCounterAttack);
                    continue;
                }

                IMeleeHitTarget target = hit.collider.GetComponentInParent<IMeleeHitTarget>();

                // У попаданий, которые начались внутри сферы, точка нулевая — берём ближайшую точку коллайдера
                if (target != null)
                    target.TakeMeleeHit(hit.point != Vector3.zero ? hit.point : hit.collider.ClosestPoint(_aimTransform.position));
            }

            if (hasHit)
                HitLanded?.Invoke(_isCounterAttack, hasKill);
        }

        // Отбивание срабатывает по нажатию, а не по касанию клинка: снаряды впереди в радиусе разворачиваются сразу
        private void TryDeflectProjectiles()
        {
            Vector3 origin = _aimTransform.position;
            Vector3 forward = _aimTransform.forward;
            bool deflectedNow = false;
            var projectiles = Projectile.Active;

            for (int i = projectiles.Count - 1; i >= 0; i--)
            {
                Projectile projectile = projectiles[i];

                if (projectile.IsReflected)
                    continue;

                Vector3 toProjectile = projectile.transform.position - origin;
                float reach = Mathf.Max(_deflectReach, projectile.Speed * _deflectTime);

                // Только то, что рядом, перед игроком и летит в его сторону
                if (toProjectile.sqrMagnitude > reach * reach || Vector3.Angle(forward, toProjectile) > _deflectAngle
                    || Vector3.Dot(projectile.Direction, toProjectile) >= 0f)
                    continue;

                projectile.Reflect(_isMegaDeflect ? _megaDeflectMultiplier : 1f);
                ImpactFx.Hit(projectile.transform.position, -toProjectile.normalized, _isMegaDeflect);
                deflectedNow = true;
            }

            if (!deflectedNow)
                return;

            if (!_hasDeflected && _arms != null)
                _arms.SetAttackSpeed(_deflectAnimationSpeed);

            _hasDeflected = true;
            OpenCounterWindow();
            Parried?.Invoke();
            Deflected?.Invoke(_isMegaDeflect);
        }

        // Отбрасываем от игрока; с прыжка — сильнее и с подбросом
        private void KnockBack(EnemyBrain brain)
        {
            Vector3 direction = brain.transform.position - _aimTransform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                direction = _aimTransform.forward;

            direction.y = 0f;
            direction.Normalize();

            Vector3 velocity = _isJumpAttack
                ? direction * _jumpKnockbackSpeed + Vector3.up * _jumpKnockbackLift
                : direction * _knockbackSpeed + Vector3.up * _knockbackLift;

            brain.Knockback(velocity, _isJumpAttack ? _jumpKnockbackStagger : _knockbackStagger);
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
