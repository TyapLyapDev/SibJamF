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

        [Inject]
        public void Construct(IPlayerHealthInfo health) =>
            _info = health ?? throw new ArgumentNullException(nameof(health));

        private void OnEnable()
        {
            _info.Changed += Refresh;
            _info.Damaged += OnDamaged;
            Refresh();
            SetFlashAlpha(0f);
        }

        private void OnDisable()
        {
            _info.Changed -= Refresh;
            _info.Damaged -= OnDamaged;
        }

        private void Update()
        {
            if (_damageFlash == null || _damageFlash.color.a <= 0f) return;

            SetFlashAlpha(Mathf.MoveTowards(_damageFlash.color.a, 0f, _flashFadeSpeed * Time.deltaTime));
        }

        private void Refresh()
        {
            if (_fill != null)
                _fill.fillAmount = _info.Current / _info.Max;

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
