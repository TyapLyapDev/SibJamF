using SibJam.Player;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam
{
    // Звуки игрока: шаги по пройденному пути, меч (замах, попадание, блок, парирование), урон, смерть и тревожный сигнал на низком хп
    public class PlayerSfx : MonoBehaviour
    {
        [SerializeField] private SfxEmitter _sfx;
        [SerializeField] private MeleeWeapon _weapon;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private CharacterController _body;

        [Header("Шаги")]
        [SerializeField] private SoundSet _stepSound;
        [SerializeField] private SoundSet _stepBodySound;
        [SerializeField, Min(0.1f)] private float _stepDistance = 2.1f;

        [Header("Меч")]
        [SerializeField] private SoundSet _swingSound;
        [SerializeField] private SoundSet _swingBodySound;
        [SerializeField] private SoundSet _hitSound;
        [SerializeField] private SoundSet _blockSound;
        [SerializeField] private SoundSet _parrySound;

        [Header("Здоровье")]
        [SerializeField] private SoundSet _damageSound;
        [SerializeField] private SoundSet _deathSound;
        [SerializeField] private SoundSet _lowHealthLoop;
        [SerializeField, Range(0f, 1f)] private float _lowHealthThreshold = 0.3f;

        private Vector3 _lastPosition;
        private float _distance;
        private bool _isLowHealth;

        private void Awake()
        {
            if (_weapon == null)
                _weapon = GetComponentInChildren<MeleeWeapon>(true);

            if (_health == null)
                _health = GetComponentInChildren<PlayerHealth>(true);

            if (_body == null)
                _body = GetComponentInChildren<CharacterController>(true);
        }

        private void OnEnable()
        {
            _lastPosition = transform.position;

            if (_weapon != null)
            {
                _weapon.Swung += OnSwung;
                _weapon.HitLanded += OnHitLanded;
                _weapon.Blocked += OnBlocked;
                _weapon.Parried += OnParried;
                _weapon.GuardBroken += OnBlocked;
            }

            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Changed += OnHealthChanged;
                _health.Died += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_weapon != null)
            {
                _weapon.Swung -= OnSwung;
                _weapon.HitLanded -= OnHitLanded;
                _weapon.Blocked -= OnBlocked;
                _weapon.Parried -= OnParried;
                _weapon.GuardBroken -= OnBlocked;
            }

            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Changed -= OnHealthChanged;
                _health.Died -= OnDied;
            }

            SetLowHealth(false);
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            _lastPosition = position;
            delta.y = 0f;

            if (_body != null && !_body.isGrounded)
                return;

            // Телепорт (чекпоинт, переход) не считаем шагами
            if (delta.magnitude > 1f)
                return;

            _distance += delta.magnitude;

            if (_distance < _stepDistance)
                return;

            _distance = 0f;
            _sfx.Play(_stepSound);
            _sfx.Play(_stepBodySound);
        }

        private void OnSwung()
        {
            _sfx.Play(_swingSound);
            _sfx.Play(_swingBodySound);
        }

        private void OnHitLanded(bool isCounter, bool isKill) => _sfx.Play(_hitSound);

        private void OnBlocked() => _sfx.Play(_blockSound);

        private void OnParried() => _sfx.Play(_parrySound);

        private void OnDamaged(float amount) => _sfx.Play(_damageSound);

        private void OnDied()
        {
            SetLowHealth(false);
            _sfx.Play(_deathSound);
        }

        private void OnHealthChanged()
        {
            SetLowHealth(!_health.IsDead && _health.Current / _health.Max <= _lowHealthThreshold);
        }

        private void SetLowHealth(bool isLow)
        {
            if (_isLowHealth == isLow || _sfx == null)
                return;

            _isLowHealth = isLow;

            if (isLow)
                _sfx.PlayLoop(_lowHealthLoop);
            else
                _sfx.StopLoop();
        }
    }
}
