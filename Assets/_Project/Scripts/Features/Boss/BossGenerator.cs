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
        [Tooltip("Анимация открытия створок из модели. Если задана, створки двигает она (вперёд — открыть, назад — закрыть)")]
        [SerializeField] private AnimationClip _openClip;
        [SerializeField] private GameObject _openClipRoot;

        [Header("Свечение модели")]
        [SerializeField] private Renderer _glowRenderer;
        [Tooltip("Индексы материалов лампочек: красные, пока закрыт, зелёные, пока открыт")]
        [SerializeField] private int[] _lampMaterials = { 2, 3 };
        [Tooltip("Индексы материалов светящихся полос внутри: горят ярче, пока открыт")]
        [SerializeField] private int[] _stripMaterials = { 1 };
        [SerializeField, ColorUsage(false, true)] private Color _stripClosedColor = new Color(0.1f, 0.6f, 1.5f);
        [SerializeField, ColorUsage(false, true)] private Color _stripOpenColor = new Color(0.4f, 2.5f, 6f);
        [SerializeField, Min(0f)] private float _lampIntensity = 1.2f;
        [SerializeField] private Light _lamp;
        [Tooltip("Свет у второй и следующих лампочек: цвет и выключение — как у _lamp")]
        [SerializeField] private Light[] _extraLamps;
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
        private float _openProgress;
        private Material[] _glowMaterials;
        private int _health;
        private int _healthAtOpen;

        public event Action<BossGenerator> HalfLost;
        public event Action<BossGenerator> Broken;

        public bool IsOpen { get; private set; }
        public bool IsBroken { get; private set; }
        public float Health01 => (float)_health / _maxHealth;

        // Створки и ядро необязательны: без них генератор открывается только лампой и звуком
        private bool HasShutters => _shutterLeft != null && _shutterRight != null;
        private bool HasOpenClip => _openClip != null && _openClipRoot != null;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private void Awake()
        {
            if (HasShutters)
            {
                _leftClosed = _shutterLeft.localPosition;
                _rightClosed = _shutterRight.localPosition;
            }

            if (_glowRenderer != null)
            {
                _glowMaterials = _glowRenderer.materials;

                foreach (Material material in _glowMaterials)
                    material.EnableKeyword("_EMISSION");
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

            SampleOpen(0f);

            if (_core != null)
                _core.SetActive(true);

            SetLampLights(true, _closedColor);
            ApplyGlow();

            if (_brokenEffect != null)
                _brokenEffect.SetActive(false);
        }

        public void Open()
        {
            if (IsBroken || IsOpen)
                return;

            IsOpen = true;
            _healthAtOpen = _health;
            SetLampLights(true, _openColor);
            ApplyGlow();
            MoveShutters(true);
            _sfx.Play(_openSound);
            _sfx.PlayLoop(_openHumLoop);
        }

        public void Close()
        {
            if (IsBroken || !IsOpen)
                return;

            IsOpen = false;
            SetLampLights(true, _closedColor);
            ApplyGlow();
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

            SetLampLights(false, _closedColor);
            ApplyGlow();
            _sfx.StopLoop();
            _sfx.Play(_breakSound);

            if (_brokenEffect != null)
                _brokenEffect.SetActive(true);

            Broken?.Invoke(this);
        }

        private void MoveShutters(bool open)
        {
            if (!HasShutters && !HasOpenClip)
                return;

            if (_moving != null)
                StopCoroutine(_moving);

            _moving = StartCoroutine(Shutters(open));
        }

        private IEnumerator Shutters(bool open)
        {
            if (HasOpenClip)
            {
                float target = open ? 1f : 0f;

                while (!Mathf.Approximately(_openProgress, target))
                {
                    SampleOpen(Mathf.MoveTowards(_openProgress, target, Time.deltaTime / _openClip.length));
                    yield return null;
                }

                _moving = null;
                yield break;
            }

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

        private void SetLampLights(bool enabled, Color color)
        {
            _lamp.enabled = enabled;
            _lamp.color = color;

            foreach (Light lamp in _extraLamps)
            {
                if (lamp == null)
                    continue;

                lamp.enabled = enabled;
                lamp.color = color;
            }
        }

        private void SampleOpen(float progress)
        {
            _openProgress = progress;

            if (HasOpenClip)
                _openClip.SampleAnimation(_openClipRoot, progress * _openClip.length);
        }

        // Лампочки: красные — закрыт, зелёные — открыт, погасли — сломан. Полосы ярче, пока открыт
        private void ApplyGlow()
        {
            if (_glowMaterials == null)
                return;

            Color lamp = IsBroken ? Color.black : (IsOpen ? _openColor : _closedColor) * _lampIntensity;
            Color strip = IsBroken ? Color.black : IsOpen ? _stripOpenColor : _stripClosedColor;

            SetEmission(_lampMaterials, lamp);
            SetEmission(_stripMaterials, strip);
        }

        private void SetEmission(int[] indices, Color color)
        {
            foreach (int index in indices)
            {
                if (index >= 0 && index < _glowMaterials.Length)
                    _glowMaterials[index].SetColor(EmissionColorId, color);
            }
        }
    }
}
