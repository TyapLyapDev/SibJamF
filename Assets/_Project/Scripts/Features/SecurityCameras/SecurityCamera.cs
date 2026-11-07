using System;
using SibJam.Feedback;
using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace SibJam.SecurityCameras
{
    public class SecurityCamera : MonoBehaviour, IInjectedFromScene
    {
        // Одинаково для всех камер, поэтому не сериализуется (сцены не могут переопределить)
        private const int _hitsToBreak = 6;

        [Tooltip("Сколько ударов нужно в окне контратаки (после парирования)")]
        [SerializeField, Min(1)] private int _counterHitsToBreak = 1;

        [Tooltip("Тряска камеры, когда камера взрывается")]
        [SerializeField, Range(0f, 1f)] private float _breakTrauma = 0.4f;

        [Header("Звук")]
        [SerializeField] private AudioMixerGroup _sfxOutput;
        [SerializeField] private SoundSet _hitSound;
        [SerializeField] private SoundSet _breakSound;
        [SerializeField] private SoundSet _appearSound;

        private ImpactFx _impactFx;
        private CameraShaker _shaker;
        private int _hitsTaken;
        private WorldHealthBar _healthBar;

        public event Action<SecurityCamera> Broken;
        public event Action<SecurityCamera> Repaired;

        public bool IsBroken { get; private set; }

        public int HitsLeft => _hitsToBreak - _hitsTaken;
        public AudioMixerGroup SfxOutput => _sfxOutput;

        private Vector3 FxPoint => TryGetComponent(out Collider ownCollider) ? ownCollider.bounds.center
            : GetComponentInChildren<Renderer>() is { } renderer ? renderer.bounds.center : transform.position;

        [Inject]
        public void Construct(ImpactFx impactFx, CameraShaker shaker)
        {
            _impactFx = impactFx ?? throw new ArgumentNullException(nameof(impactFx));
            _shaker = shaker;
        }

        private void Awake()
        {
            _healthBar = gameObject.AddComponent<WorldHealthBar>();
        }

        public void TakeHit(bool isCounterAttack = false, Vector3? hitPoint = null, Vector3? hitNormal = null)
        {
            if (IsBroken) return;

            // Контрудар весит столько, чтобы за _counterHitsToBreak таких ударов камера гарантированно сломалась
            _hitsTaken += isCounterAttack ? Mathf.CeilToInt((float)_hitsToBreak / _counterHitsToBreak) : 1;

            if (_hitsTaken >= _hitsToBreak)
            {
                Break();
                return;
            }

            _healthBar.SetValue((float)HitsLeft / _hitsToBreak);
            Vector3 normal = hitNormal.GetValueOrDefault(Vector3.up).normalized;
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.up;

            // Выносим источник искр за поверхность, чтобы корпус камеры не перекрывал вспышку.
            Vector3 point = hitPoint.GetValueOrDefault(FxPoint) + normal * 0.08f;
            _impactFx.Hit(point, normal, isCounterAttack);
            SfxEmitter.PlayAt(_hitSound, point, _sfxOutput);
        }

        private void Break()
        {
            IsBroken = true;
            _impactFx.Explosion(FxPoint);
            SfxEmitter.PlayAt(_breakSound, FxPoint, _sfxOutput);
            if (_shaker != null)
                _shaker.AddTrauma(_breakTrauma);

            Broken?.Invoke(this);
            gameObject.SetActive(false);
        }

        public void Repair()
        {
            if (!IsBroken) return;
            _hitsTaken = 0;
            IsBroken = false;
            _healthBar.Hide();
            gameObject.SetActive(true);
            if (!TryGetComponent(out EnemySpawnHologram hologram))
                hologram = gameObject.AddComponent<EnemySpawnHologram>();
            hologram.Play();
            SfxEmitter.PlayAt(_appearSound, FxPoint, _sfxOutput);
            Repaired?.Invoke(this);
        }
    }
}
