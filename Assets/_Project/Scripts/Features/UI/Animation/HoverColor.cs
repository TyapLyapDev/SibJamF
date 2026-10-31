using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SibJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HoverColor : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private HoverColorSettings _settings;
        [SerializeField] private TMP_Text _text;

        [Header("Update")]
        [SerializeField] private bool _useUnscaledTime = true;

        private Tween _colorTween;
        private Color _baseColor;
        private bool _isHovered;
        private readonly List<RaycastResult> _raycastResults = new();

        private void Awake()
        {
            if (_settings == null)
                throw new InvalidOperationException(
                    $"[HoverColor] HoverColorSettings не назначен на '{name}'. Назначьте ScriptableObject в инспекторе.");

            if (_text == null)
                throw new InvalidOperationException(
                    $"[HoverColor] TMP_Text не назначен на '{name}'. Назначьте ссылку в инспекторе.");

            _baseColor = _text.color;
        }

        private void OnValidate()
        {
            if (_settings == null)
                Debug.LogError(
                    $"[HoverColor] HoverColorSettings не назначен на '{name}'. Без него компонент не запустится.",
                    this);

            if (_text == null)
                Debug.LogError(
                    $"[HoverColor] TMP_Text не назначен на '{name}'. Без него компонент не запустится.",
                    this);
        }

        private void Update()
        {
            bool shouldBeHovered = UiPointer.IsOver(transform, _raycastResults);

            if (shouldBeHovered == _isHovered)
                return;

            _isHovered = shouldBeHovered;
            TweenTo(_isHovered ? _settings.HoverColor : _baseColor);
        }

        private void OnDisable()
        {
            _colorTween?.Kill();
            _colorTween = null;
            _isHovered = false;

            if (_text != null)
                _text.color = _baseColor;
        }

        public void ForceSetHovered(bool hovered)
        {
            if (hovered == _isHovered) return;

            _isHovered = hovered;
            TweenTo(_isHovered ? _settings.HoverColor : _baseColor);
        }

        private void TweenTo(Color target)
        {
            _colorTween?.Kill();

            _colorTween = _text
                .DOColor(target, _settings.Duration)
                .SetEase(_settings.Ease)
                .SetUpdate(_useUnscaledTime);
        }
    }
}