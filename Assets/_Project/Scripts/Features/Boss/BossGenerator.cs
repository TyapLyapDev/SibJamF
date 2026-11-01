using System;
using System.Collections;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Boss
{
    // Генератор системы наблюдения: закрыт створками, BossFight открывает его, пока босс оглушён.
    // Пока открыт, получает урон от меча; сняли за одно открытие половину запаса — шлёт HalfLost и BossFight его закрывает.
    // Запас кончился — Broken. Удары по закрытому дают только искры
    public class BossGenerator : MonoBehaviour, IMeleeHitTarget
    {
        [SerializeField] private Transform _shutterLeft;
        [SerializeField] private Transform _shutterRight;
        [Tooltip("На сколько створки разъезжаются в стороны, в их локальных осях X")]
        [SerializeField, Min(0f)] private float _shutterTravel = 0.55f;
        [SerializeField, Min(0.05f)] private float _shutterDuration = 0.4f;
        [SerializeField] private Light _lamp;
        [SerializeField] private Color _closedColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private Color _openColor = new Color(0.2f, 1f, 0.35f);
        [SerializeField] private GameObject _core;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private GameObject _brokenEffect;
        [Tooltip("Запас генератора в ударах мечом. Систему урона подключат позже — тогда сюда пойдёт урон оружия")]
        [SerializeField, Min(2)] private int _maxHealth = 6;

        [Header("Звуки")]
        [SerializeField] private SfxEmitter _sfx;
        [SerializeField] private SoundSet _openSound;
        [SerializeField] private SoundSet _closeSound;
        [SerializeField] private SoundSet _blockedHitSound;
        [SerializeField] private SoundSet _damageSound;
        [SerializeField] private SoundSet _breakSound;
        [SerializeField] private SoundSet _openHumLoop;

        private Vector3 _leftClosed;
        private Vector3 _rightClosed;
        private Coroutine _moving;
        private int _health;
        private int _healthAtOpen;

        public event Action<BossGenerator> HalfLost;
        public event Action<BossGenerator> Broken;

        public bool IsOpen { get; private set; }
        public bool IsBroken { get; private set; }
        public float Health01 => (float)_health / _maxHealth;

        // Створки и ядро необязательны: без них генератор открывается только лампой и звуком
        private bool HasShutters => _shutterLeft != null && _shutterRight != null;

        private void Awake()
        {
            if (HasShutters)
            {
                _leftClosed = _shutterLeft.localPosition;
                _rightClosed = _shutterRight.localPosition;
            }

            ResetGenerator();
        }

        public void ResetGenerator()
        {
            StopAllCoroutines();
            _moving = null;
            IsOpen = false;
            IsBroken = false;
            _health = _maxHealth;

            if (HasShutters)
            {
                _shutterLeft.localPosition = _leftClosed;
                _shutterRight.localPosition = _rightClosed;
            }

            if (_core != null)
                _core.SetActive(true);

            _lamp.enabled = true;
            _lamp.color = _closedColor;

            if (_brokenEffect != null)
                _brokenEffect.SetActive(false);
        }

        public void Open()
        {
            if (IsBroken || IsOpen)
                return;

            IsOpen = true;
            _healthAtOpen = _health;
            _lamp.color = _openColor;
            MoveShutters(true);
            _sfx.Play(_openSound);
            _sfx.PlayLoop(_openHumLoop);
        }

        public void Close()
        {
            if (IsBroken || !IsOpen)
                return;

            IsOpen = false;
            _lamp.color = _closedColor;
            MoveShutters(false);
            _sfx.StopLoop();
            _sfx.Play(_closeSound);
        }

        public void TakeMeleeHit(Vector3 point)
        {
            if (_sparks != null)
            {
                _sparks.transform.position = point;
                _sparks.Play();
            }

            if (IsBroken)
                return;

            if (!IsOpen)
            {
                _sfx.Play(_blockedHitSound);
                return;
            }

            _health--;
            _sfx.Play(_damageSound);

            if (_health <= 0)
            {
                Break();
                return;
            }

            if (_healthAtOpen - _health >= Mathf.CeilToInt(_maxHealth * 0.5f))
                HalfLost?.Invoke(this);
        }

        private void Break()
        {
            IsBroken = true;
            IsOpen = false;
            _health = 0;

            if (_core != null)
                _core.SetActive(false);

            _lamp.enabled = false;
            _sfx.StopLoop();
            _sfx.Play(_breakSound);

            if (_brokenEffect != null)
                _brokenEffect.SetActive(true);

            Broken?.Invoke(this);
        }

        private void MoveShutters(bool open)
        {
            if (!HasShutters)
                return;

            if (_moving != null)
                StopCoroutine(_moving);

            _moving = StartCoroutine(Shutters(open));
        }

        private IEnumerator Shutters(bool open)
        {
            Vector3 leftFrom = _shutterLeft.localPosition, rightFrom = _shutterRight.localPosition;
            Vector3 leftTo = open ? _leftClosed + Vector3.left * _shutterTravel : _leftClosed;
            Vector3 rightTo = open ? _rightClosed + Vector3.right * _shutterTravel : _rightClosed;

            for (float time = 0f; time < _shutterDuration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / _shutterDuration);
                _shutterLeft.localPosition = Vector3.Lerp(leftFrom, leftTo, t);
                _shutterRight.localPosition = Vector3.Lerp(rightFrom, rightTo, t);
                yield return null;
            }

            _shutterLeft.localPosition = leftTo;
            _shutterRight.localPosition = rightTo;
            _moving = null;
        }
    }
}
