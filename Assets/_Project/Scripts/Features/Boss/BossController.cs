using System;
using System.Collections;
using SibJam.Combat;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Boss
{
    // Босс: идёт к игроку и по дистанции выбирает одну из трёх атак — удар, кольцо зарядов, прыжок на полосу арены.
    // Удары мечом копят оглушение: набралось — босс падает на колени (Staggered), BossFight открывает генераторы и потом зовёт Recover.
    // Фазу (0..2) задаёт BossFight по числу сломанных генераторов
    public class BossController : MonoBehaviour, IMeleeHitTarget
    {
        private const float CrossFade = 0.15f;

        [SerializeField] private Animator _animator;
        [SerializeField] private BossAnimationEvents _events;
        [SerializeField] private Collider _body;
        [SerializeField] private ParticleSystem _shieldSparks;
        [SerializeField] private string _playerTag = "Player";

        [Header("Арена")]
        [SerializeField] private Transform _arenaCenter;
        [SerializeField, Min(1f)] private float _arenaRadius = 11f;
        [SerializeField] private BossStrip[] _strips;

        [Header("Движение")]
        [SerializeField, Min(1f)] private float _turnSpeed = 140f;
        [SerializeField, Min(0.5f)] private float _maxChaseTime = 2.5f;

        [Header("Удар")]
        [SerializeField, Min(0.5f)] private float _meleeRange = 5.5f;
        [SerializeField, Range(0f, 180f)] private float _meleeAngle = 70f;
        [SerializeField, Min(0f)] private float _meleeDamage = 30f;
        [SerializeField, Min(0f)] private float _meleeBlockedDamage = 10f;
        [SerializeField, Min(0f)] private float _parryStun = 1.5f;
        [Tooltip("Время в клипе Attacking (Mutant Swiping), когда рука доходит до цели — там же событие OnMeleeHit")]
        [SerializeField, Min(0.1f)] private float _meleeHitTime = 1.3f;
        [SerializeField, Min(0.1f)] private float _meleeClipLength = 2.67f;
        [SerializeField] private Transform _meleeTelegraph;
        [SerializeField] private TrailRenderer[] _handTrails;
        [SerializeField] private Light _handLight;
        [SerializeField, Min(0f)] private float _handLightIntensity = 6f;
        [SerializeField] private ParticleSystem _meleeImpactFx;

        [Header("Заряды")]
        [SerializeField] private BossCharge _chargePrefab;
        [SerializeField, Min(0f)] private float _chargeHeight = 1.6f;
        [SerializeField, Min(0f)] private float _chargeDamage = 15f;
        [SerializeField, Min(0f)] private float _chargeBlockedDamage = 7.5f;
        [SerializeField] private int[] _chargesPerPhase = { 8, 12, 12 };
        [SerializeField] private int[] _ringsPerPhase = { 1, 2, 2 };
        [SerializeField, Min(0f)] private float _maxShootDistance = 18f;

        [Header("Прыжок на полосу")]
        [SerializeField, Min(0f)] private float _stripLandDamage = 40f;
        [SerializeField, Min(0f)] private float _stripTickDamage = 5f;
        [SerializeField, Min(0f)] private float _stripDuration = 3f;
        [SerializeField] private int[] _attacksBetweenStripsPerPhase = { 3, 2, 2 };
        [Tooltip("Время в клипе JumpAttack, когда ноги отрываются от пола и когда касаются")]
        [SerializeField, Min(0f)] private float _jumpTakeoff = 0.5f;
        [SerializeField, Min(0f)] private float _jumpLand = 1.6f;
        [SerializeField, Min(0f)] private float _jumpClipLength = 3.7f;
        [SerializeField] private Transform _landingMarker;
        [SerializeField] private ParticleSystem _landFx;

        [Header("Оглушение")]
        [Tooltip("Сколько ударов мечом нужно, чтобы босс упал на колени, по фазам")]
        [SerializeField] private int[] _staggerHitsPerPhase = { 5, 6, 7 };
        [Tooltip("Пока босс не атакует, накопленные удары понемногу сходят на нет: ударов в секунду")]
        [SerializeField, Min(0f)] private float _staggerDecay = 0.25f;
        [SerializeField] private ParticleSystem _stunFx;
        [Tooltip("Рык после оглушения: скорость клипа Roar и сколько он длится в секундах клипа")]
        [SerializeField, Min(0.1f)] private float _roarSpeed = 1.5f;
        [SerializeField, Min(0f)] private float _roarClipLength = 5.4f;

        [Header("Паузы")]
        [SerializeField] private float[] _pausePerPhase = { 2f, 1.2f, 1.2f };

        [Header("Звуки")]
        [SerializeField] private SfxEmitter _sfx;
        [SerializeField] private SoundSet _stepSound;
        [Tooltip("Раз в столько секунд повторяется звук шагов, пока босс идёт (длина цикла Walking)")]
        [SerializeField, Min(0.1f)] private float _stepInterval = 1.43f;
        [SerializeField] private SoundSet _meleeWindupSound;
        [SerializeField] private SoundSet _meleeWhooshSound;
        [SerializeField] private SoundSet _meleeHitSound;
        [SerializeField] private SoundSet _shootSound;
        [SerializeField] private SoundSet _jumpSound;
        [SerializeField] private SoundSet _landSound;
        [SerializeField] private SoundSet _shieldHitSound;
        [SerializeField] private SoundSet _staggerSound;
        [SerializeField] private SoundSet _stunLoopSound;
        [SerializeField] private SoundSet _roarSound;
        [SerializeField] private SoundSet _deathSound;

        private Transform _player;
        private IPlayerHitReceiver _receiver;
        private Coroutine _brain;
        private Pose _startPose;
        private int _phase;
        private int _attacksSinceStrip;
        private bool _forceStrip;
        private bool _jumping;
        private float _stagger;
        private BossStrip _currentStrip;

        public event Action AttackStarted;
        public event Action AttackFinished;
        public event Action Staggered;
        public event Action Recovered;
        public event Action Died;
        // 0..1 — заполненность шкалы оглушения, для HUD
        public event Action<float> StaggerChanged;

        public bool IsActive => _brain != null;
        public bool IsDead { get; private set; }
        public bool IsStaggered { get; private set; }

        private void Awake()
        {
            _startPose = new Pose(transform.position, transform.rotation);
            HideFx();
        }

        private void OnEnable()
        {
            _events.MeleeHit += OnMeleeHit;
            _events.Shoot += OnShoot;
            _events.JumpLanded += OnJumpLanded;
            _events.Moved += OnAnimatorMoved;
        }

        private void OnDisable()
        {
            _events.MeleeHit -= OnMeleeHit;
            _events.Shoot -= OnShoot;
            _events.JumpLanded -= OnJumpLanded;
            _events.Moved -= OnAnimatorMoved;
        }

        private void Update()
        {
            if (IsActive && !IsStaggered && _stagger > 0f)
                SetStagger(Mathf.Max(0f, _stagger - _staggerDecay * Time.deltaTime));
        }

        public void Activate()
        {
            GameObject player = GameObject.FindWithTag(_playerTag);
            _player = player != null ? player.transform : null;
            _receiver = player != null ? player.GetComponent<IPlayerHitReceiver>() : null;

            if (_receiver == null)
                Debug.LogWarning("[Boss] На игроке нет IPlayerHitReceiver — удары босса никуда не попадут", this);

            if (_brain == null && !IsDead)
                _brain = StartCoroutine(Brain());
        }

        // Для перезапуска боя после смерти игрока
        public void ResetBoss()
        {
            StopBrain();
            StopStrips();
            HideFx();
            _sfx.StopAll();
            IsDead = false;
            IsStaggered = false;
            _phase = 0;
            _attacksSinceStrip = 0;
            _forceStrip = false;
            _jumping = false;
            SetStagger(0f);
            _animator.speed = 1f;
            _body.enabled = true;
            transform.SetPositionAndRotation(_startPose.position, _startPose.rotation);
            Play("Idle", 0f);
        }

        public void SetPhase(int phase) => _phase = Mathf.Max(0, phase);

        public void RequestStrip() => _forceStrip = true;

        // Короткое оглушение, например после парирования
        public void Stun(float duration)
        {
            if (IsDead || IsStaggered)
                return;

            StopBrain();
            HideFx();
            _brain = StartCoroutine(Stunned(duration));
        }

        // Встаёт с колен и продолжает бой. Зовёт BossFight, когда генераторы закрылись
        public void Recover()
        {
            if (IsDead || !IsStaggered)
                return;

            StopBrain();
            _brain = StartCoroutine(Recovering());
        }

        public void Die()
        {
            if (IsDead)
                return;

            IsDead = true;
            IsStaggered = false;
            StopBrain();
            StopStrips();
            HideFx();
            _animator.speed = 1f;
            _body.enabled = false;
            Play("Death");
            _sfx.StopAll();
            _sfx.Play(_deathSound);
            Died?.Invoke();
        }

        public void TakeMeleeHit(Vector3 point)
        {
            if (_shieldSparks != null)
            {
                _shieldSparks.transform.position = point;
                _shieldSparks.Play();
            }

            if (IsDead)
                return;

            _sfx.Play(_shieldHitSound);

            if (IsStaggered || !IsActive)
                return;

            SetStagger(_stagger + 1f);

            if (_stagger >= ForPhase(_staggerHitsPerPhase))
                Stagger();
        }

        private void Stagger()
        {
            StopBrain();
            StopStrips();
            HideFx();
            IsStaggered = true;
            _sfx.Play(_staggerSound);
            _brain = StartCoroutine(Kneeling());
            Staggered?.Invoke();
        }

        private IEnumerator Brain()
        {
            Play("Idle");
            yield return new WaitForSeconds(1f);

            for (;;)
            {
                yield return Chase();

                float distance = DistanceToPlayer();
                AttackStarted?.Invoke();

                if (_forceStrip || _attacksSinceStrip >= ForPhase(_attacksBetweenStripsPerPhase))
                    yield return StripAttack();
                else if (distance <= _meleeRange)
                    yield return MeleeAttack();
                else if (distance <= _maxShootDistance)
                    yield return ShootAttack();
                else
                    yield return StripAttack();

                AttackFinished?.Invoke();

                Play("Idle");
                yield return TurnWhile(ForPhase(_pausePerPhase));
            }
        }

        private IEnumerator Chase()
        {
            if (DistanceToPlayer() <= _meleeRange)
                yield break;

            Play("Walking");
            float nextStep = 0f;

            // Идёт к игроку, пока не подойдёт на удар; если игрок держит дистанцию — атакует издалека
            for (float time = 0f; time < _maxChaseTime && DistanceToPlayer() > _meleeRange * 0.8f; time += Time.deltaTime)
            {
                if (time >= nextStep)
                {
                    nextStep += _stepInterval;
                    _sfx.Play(_stepSound);
                }

                TurnTowardsPlayer();
                yield return null;
            }
        }

        // Пока идёт замах из клипа, на полу растёт сектор поражения и разгорается рука
        private IEnumerator MeleeAttack()
        {
            _attacksSinceStrip++;
            Play("Attacking", 0.1f);
            _sfx.Play(_meleeWindupSound);

            if (_meleeTelegraph != null)
                _meleeTelegraph.gameObject.SetActive(true);

            float windup = Mathf.Max(0.1f, _meleeHitTime - 0.3f);

            for (float time = 0f; time < windup; time += Time.deltaTime)
            {
                float t = time / windup;

                // В последнюю треть замаха босс уже не доворачивает — можно уйти вбок
                if (t < 0.65f)
                    TurnTowardsPlayer();

                if (_meleeTelegraph != null)
                    _meleeTelegraph.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, Mathf.Sqrt(t));

                if (_handLight != null)
                {
                    _handLight.enabled = true;
                    _handLight.intensity = _handLightIntensity * t;
                }

                yield return null;
            }

            foreach (TrailRenderer trail in _handTrails)
            {
                trail.Clear();
                trail.emitting = true;
            }

            _sfx.Play(_meleeWhooshSound);

            // Удар наносит событие OnMeleeHit из клипа, здесь только ждём, пока рука пройдёт
            yield return new WaitForSeconds(0.35f);

            if (_meleeTelegraph != null)
                _meleeTelegraph.gameObject.SetActive(false);

            if (_handLight != null)
                _handLight.enabled = false;

            yield return new WaitForSeconds(0.25f);
            SetTrails(false);

            yield return new WaitForSeconds(Mathf.Max(0f, _meleeClipLength - _meleeHitTime - 0.6f - 0.3f));
        }

        private IEnumerator ShootAttack()
        {
            Play("Shooting");
            _sfx.Play(_shootSound);
            _attacksSinceStrip++;
            yield return TurnWhile(1.2f);
            yield return new WaitForSeconds(1.3f);
        }

        // Прыжок ведётся кодом: от взлёта до касания босс летит ровно в точку на полосе, на ней же метка приземления
        private IEnumerator StripAttack()
        {
            _forceStrip = false;
            _attacksSinceStrip = 0;
            _currentStrip = StripForJump();

            Vector3 from = transform.position;
            Vector3 target = ClampToArena(_currentStrip != null ? _currentStrip.LandingPoint(_player.position) : _player.position);
            target.y = from.y;

            if (_currentStrip != null)
                _currentStrip.Telegraph();

            if (_landingMarker != null)
            {
                _landingMarker.position = target + Vector3.up * 0.05f;
                _landingMarker.gameObject.SetActive(true);
            }

            _jumping = true;
            Play("JumpAttack", 0.1f);
            _sfx.Play(_jumpSound);

            for (float time = 0f; time < _jumpTakeoff; time += Time.deltaTime)
            {
                TurnTowards(target);
                yield return null;
            }

            float flight = Mathf.Max(0.05f, _jumpLand - _jumpTakeoff);

            for (float time = 0f; time < flight; time += Time.deltaTime)
            {
                transform.position = Vector3.Lerp(from, target, time / flight);
                yield return null;
            }

            transform.position = target;

            if (_landingMarker != null)
                _landingMarker.gameObject.SetActive(false);

            yield return new WaitForSeconds(Mathf.Max(0f, _jumpClipLength - _jumpLand - 0.3f));
            _jumping = false;
        }

        private IEnumerator Stunned(float duration)
        {
            Play("Idle");
            _animator.speed = 0.35f;

            if (_shieldSparks != null)
            {
                _shieldSparks.transform.position = transform.position + Vector3.up * 3f;
                _shieldSparks.Play();
            }

            yield return new WaitForSeconds(duration);
            _animator.speed = 1f;
            _brain = StartCoroutine(Brain());
        }

        // Шатается на месте (Dizzy Idle по кругу), пока BossFight не позовёт Recover
        private IEnumerator Kneeling()
        {
            _animator.speed = 1f;
            Play("Stun", 0.3f);

            if (_stunFx != null)
                _stunFx.Play();

            _sfx.PlayLoop(_stunLoopSound);
            yield break;
        }

        // Рык: окно оглушения кончилось, сейчас снова пойдёт в атаку
        private IEnumerator Recovering()
        {
            if (_stunFx != null)
                _stunFx.Stop();

            _sfx.StopLoop();
            _animator.speed = _roarSpeed;
            Play("Roar", 0.3f);
            _sfx.Play(_roarSound);
            yield return new WaitForSeconds(_roarClipLength / _roarSpeed);
            _animator.speed = 1f;
            IsStaggered = false;
            SetStagger(0f);
            Recovered?.Invoke();
            _brain = StartCoroutine(Brain());
        }

        private void OnMeleeHit()
        {
            _sfx.Play(_meleeHitSound);

            if (_meleeImpactFx != null)
            {
                _meleeImpactFx.transform.position = Ground(transform.position + transform.forward * (_meleeRange * 0.55f));
                _meleeImpactFx.Play();
            }

            if (_player == null || _receiver == null || DistanceToPlayer() > _meleeRange)
                return;

            Vector3 toPlayer = Flat(_player.position - transform.position);

            if (Vector3.Angle(transform.forward, toPlayer) > _meleeAngle)
                return;

            IncomingHitResult result = _receiver.ReceiveHit(new PlayerHit(_meleeDamage, _meleeBlockedDamage, canParry: true, source: this));

            if (result == IncomingHitResult.Parried)
                Stun(_parryStun);
        }

        private void OnShoot() => StartCoroutine(FireRings());

        private IEnumerator FireRings()
        {
            int count = ForPhase(_chargesPerPhase);
            int rings = ForPhase(_ringsPerPhase);

            for (int ring = 0; ring < rings; ring++)
            {
                // Второе кольцо сдвинуто на полшага: щели первого закрываются, надо успеть сместиться
                float offset = ring * 180f / count + transform.eulerAngles.y;

                for (int i = 0; i < count; i++)
                {
                    Vector3 direction = Quaternion.Euler(0f, offset + i * 360f / count, 0f) * Vector3.forward;
                    BossCharge charge = Instantiate(_chargePrefab, transform.position + Vector3.up * _chargeHeight + direction * 1.5f, Quaternion.identity);
                    charge.Launch(direction, _player, _receiver, transform, _chargeDamage, _chargeBlockedDamage);
                }

                yield return new WaitForSeconds(0.5f);
            }
        }

        private void OnJumpLanded()
        {
            _sfx.Play(_landSound);

            if (_landFx != null)
            {
                _landFx.transform.position = Ground(transform.position);
                _landFx.Play();
            }

            if (_currentStrip != null)
                _currentStrip.Activate(_stripDuration, _stripLandDamage, _stripTickDamage, _player, _receiver, this);
        }

        private void OnAnimatorMoved(Vector3 delta)
        {
            // В прыжке позицию ведёт StripAttack, на коленях босс стоит на месте
            if (IsDead || _jumping || IsStaggered)
                return;

            transform.position = ClampToArena(transform.position + Flat(delta));
        }

        // Не даём уйти за пределы арены (на помост, на площадки с генераторами, в стены)
        private Vector3 ClampToArena(Vector3 position)
        {
            if (_arenaCenter == null)
                return position;

            Vector3 fromCenter = Flat(position - _arenaCenter.position);

            if (fromCenter.magnitude <= _arenaRadius)
                return position;

            Vector3 clamped = _arenaCenter.position + fromCenter.normalized * _arenaRadius;
            clamped.y = position.y;
            return clamped;
        }

        private IEnumerator TurnWhile(float duration)
        {
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                TurnTowardsPlayer();
                yield return null;
            }
        }

        private void TurnTowardsPlayer()
        {
            if (_player != null)
                TurnTowards(_player.position);
        }

        private void TurnTowards(Vector3 point)
        {
            Vector3 direction = Flat(point - transform.position);

            if (direction.sqrMagnitude < 0.01f)
                return;

            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, _turnSpeed * Time.deltaTime);
        }

        // Полоса, на которой стоит игрок; если он между полосами или на помосте — ближайшая к нему
        private BossStrip StripForJump()
        {
            if (_player == null || _strips.Length == 0)
                return null;

            BossStrip nearest = null;
            float best = float.MaxValue;

            foreach (BossStrip strip in _strips)
            {
                if (strip.IsInside(_player.position + Vector3.up))
                    return strip;

                float distance = Flat(strip.transform.position - _player.position).sqrMagnitude;

                if (distance < best)
                {
                    best = distance;
                    nearest = strip;
                }
            }

            return nearest;
        }

        private Vector3 Ground(Vector3 position)
        {
            position.y = _arenaCenter != null ? _arenaCenter.position.y : transform.position.y;
            return position + Vector3.up * 0.05f;
        }

        private void SetStagger(float value)
        {
            _stagger = value;
            StaggerChanged?.Invoke(Mathf.Clamp01(_stagger / ForPhase(_staggerHitsPerPhase)));
        }

        private void HideFx()
        {
            _animator.speed = 1f;

            if (_meleeTelegraph != null)
                _meleeTelegraph.gameObject.SetActive(false);

            if (_landingMarker != null)
                _landingMarker.gameObject.SetActive(false);

            if (_handLight != null)
                _handLight.enabled = false;

            SetTrails(false);

            if (_stunFx != null)
                _stunFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _jumping = false;
        }

        private void SetTrails(bool emitting)
        {
            foreach (TrailRenderer trail in _handTrails)
                trail.emitting = emitting;
        }

        private float DistanceToPlayer() => _player != null ? Flat(_player.position - transform.position).magnitude : float.MaxValue;

        private T ForPhase<T>(T[] values) => values[Mathf.Clamp(_phase, 0, values.Length - 1)];

        private void Play(string state, float fade = CrossFade) => _animator.CrossFadeInFixedTime(state, fade);

        private void StopBrain()
        {
            if (_brain != null)
                StopCoroutine(_brain);

            _brain = null;
            StopAllCoroutines();
        }

        private void StopStrips()
        {
            foreach (BossStrip strip in _strips)
                strip.Stop();
        }

        private static Vector3 Flat(Vector3 vector) => new Vector3(vector.x, 0f, vector.z);
    }
}
