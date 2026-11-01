using System;
using SibJam.Feedback;
using UnityEngine;
using UnityEngine.Audio;

namespace SibJam.SecurityCameras
{
    public class SecurityCamera : MonoBehaviour
    {
        // Одинаково для всех камер, поэтому не сериализуется (сцены не могут переопределить)
        private const int _hitsToBreak = 6;

        [Tooltip("Сколько ударов нужно в окне контратаки (после парирования)")]
        [SerializeField, Min(1)] private int _counterHitsToBreak = 1;

        [Header("Звук")]
        [SerializeField] private AudioMixerGroup _sfxOutput;
        [SerializeField] private SoundSet _hitSound;
        [SerializeField] private SoundSet _breakSound;

        private int _hitsTaken;
        private WorldHealthBar _healthBar;

        public event Action<SecurityCamera> Broken;

        public bool IsBroken { get; private set; }

        public int HitsLeft => _hitsToBreak - _hitsTaken;

        private Vector3 FxPoint => TryGetComponent(out Collider ownCollider) ? ownCollider.bounds.center
            : GetComponentInChildren<Renderer>() is { } renderer ? renderer.bounds.center : transform.position;

        private void Awake()
        {
            _healthBar = gameObject.AddComponent<WorldHealthBar>();
        }

        public void TakeHit(bool isCounterAttack = false)
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
            ImpactFx.Hit(FxPoint, Vector3.up, isCounterAttack);
            SfxEmitter.PlayAt(_hitSound, FxPoint, _sfxOutput);
        }

        private void Break()
        {
            IsBroken = true;
            ImpactFx.Explosion(FxPoint);
            SfxEmitter.PlayAt(_breakSound, FxPoint, _sfxOutput);
            CameraShake.AddTrauma(0.4f);
            Broken?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
