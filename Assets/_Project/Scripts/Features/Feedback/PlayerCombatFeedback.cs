using SibJam.Player;
using SibJam.Weapons;
using UnityEngine;
using VContainer;

namespace SibJam.Feedback
{
    // Отдача боя от первого лица: тряска, толчки камеры и стоп-кадр на удары, блоки, парирования и полученный урон
    public class PlayerCombatFeedback : MonoBehaviour
    {
        [SerializeField] private MeleeWeapon _weapon;
        [SerializeField] private PlayerHealth _health;

        [Header("Замах")]
        [SerializeField] private Vector3 _swingKick = new Vector3(1.2f, -0.8f, 1.5f);

        [Header("Попадание")]
        [SerializeField, Min(0f)] private float _hitTrauma = 0.3f;
        [SerializeField, Min(0f)] private float _hitStop = 0.06f;
        [SerializeField, Min(0f)] private float _counterHitStop = 0.12f;
        [SerializeField, Min(0f)] private float _killHitStop = 0.1f;
        [SerializeField] private Vector3 _hitKick = new Vector3(-1.5f, 1f, -2f);

        [Header("Защита")]
        [SerializeField, Min(0f)] private float _blockTrauma = 0.25f;
        [SerializeField] private Vector3 _blockKick = new Vector3(-2f, 0f, 0f);
        [SerializeField, Min(0f)] private float _parryHitStop = 0.15f;
        [SerializeField, Min(0f)] private float _parryTrauma = 0.35f;
        [SerializeField, Min(0f)] private float _guardBreakTrauma = 0.6f;

        [Header("Урон по игроку")]
        [SerializeField, Min(0f)] private float _damageTrauma = 0.5f;
        [SerializeField] private Vector3 _damageKick = new Vector3(-4f, 2f, 3f);

        private CameraShaker _shaker;

        [Inject]
        public void Construct(CameraShaker shaker) => _shaker = shaker;

        private void Awake()
        {
            if (_weapon == null)
                _weapon = GetComponentInChildren<MeleeWeapon>(true);

            if (_health == null)
                _health = GetComponentInChildren<PlayerHealth>(true);
        }

        private void OnEnable()
        {
            if (_weapon != null)
            {
                _weapon.Swung += OnSwung;
                _weapon.HitLanded += OnHitLanded;
                _weapon.Blocked += OnBlocked;
                _weapon.Parried += OnParried;
                _weapon.GuardBroken += OnGuardBroken;
            }

            if (_health != null)
                _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (_weapon != null)
            {
                _weapon.Swung -= OnSwung;
                _weapon.HitLanded -= OnHitLanded;
                _weapon.Blocked -= OnBlocked;
                _weapon.Parried -= OnParried;
                _weapon.GuardBroken -= OnGuardBroken;
            }

            if (_health != null)
                _health.Damaged -= OnDamaged;
        }

        private void OnSwung() => _shaker.Kick(_swingKick);

        private void OnHitLanded(bool isCounter, bool isKill)
        {
            float trauma = _hitTrauma * (isCounter || isKill ? 1.6f : 1f);
            _shaker.AddTrauma(trauma);
            _shaker.Kick(_hitKick);
            HitStop.Play(isCounter ? _counterHitStop : isKill ? _killHitStop : _hitStop);
        }

        private void OnBlocked()
        {
            _shaker.AddTrauma(_blockTrauma);
            _shaker.Kick(_blockKick);
        }

        private void OnParried()
        {
            _shaker.AddTrauma(_parryTrauma);
            _shaker.Kick(_blockKick * 0.5f);
            HitStop.Play(_parryHitStop);
        }

        private void OnGuardBroken()
        {
            _shaker.AddTrauma(_guardBreakTrauma);
            _shaker.Kick(_blockKick * 2f);
        }

        private void OnDamaged(float amount)
        {
            _shaker.AddTrauma(_damageTrauma);
            _shaker.Kick(new Vector3(_damageKick.x, Random.Range(-_damageKick.y, _damageKick.y), Random.Range(-_damageKick.z, _damageKick.z)));
        }
    }
}
