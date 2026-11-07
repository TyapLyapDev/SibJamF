using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam.Player.UI
{
    // Полоска хп и красная вспышка по краям экрана при получении урона
    public class PlayerHealthView : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Image _damageFlash;
        [SerializeField, Range(0f, 1f)] private float _flashAlpha = 0.35f;
        [SerializeField, Min(0.01f)] private float _flashFadeSpeed = 1.5f;

        private IPlayerHealthInfo _info;
        private bool _subscribed;

        [Inject]
        public void Construct(IPlayerHealthInfo health)
        {
            Unsubscribe();
            _info = health ?? throw new ArgumentNullException(nameof(health));
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
            SetFlashAlpha(0f);
        }

        private void Subscribe()
        {
            if (_info == null || _subscribed) return;
            _info.Changed += Refresh;
            _info.Damaged += OnDamaged;
            _subscribed = true;
            Refresh();
        }

        private void OnDisable() => Unsubscribe();

        private void Unsubscribe()
        {
            if (!_subscribed) return;
            _info.Changed -= Refresh;
            _info.Damaged -= OnDamaged;
            _subscribed = false;
        }

        private void Update()
        {
            if (_damageFlash == null || _damageFlash.color.a <= 0f) return;

            SetFlashAlpha(Mathf.MoveTowards(_damageFlash.color.a, 0f, _flashFadeSpeed * Time.deltaTime));
        }

        private void Refresh()
        {
            if (_fill != null)
                _fill.fillAmount = _info.Max > 0f ? Mathf.Clamp01(_info.Current / _info.Max) : 0f;

            if (_text != null)
                _text.text = $"{Mathf.CeilToInt(_info.Current)}";
        }

        private void OnDamaged(float amount) => SetFlashAlpha(_flashAlpha);

        private void SetFlashAlpha(float alpha)
        {
            if (_damageFlash == null) return;

            var color = _damageFlash.color;
            color.a = alpha;
            _damageFlash.color = color;
        }
    }
}
