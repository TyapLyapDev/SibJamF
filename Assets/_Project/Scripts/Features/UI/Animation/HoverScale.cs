using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SibJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HoverScale : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private HoverScaleSettings _settings;

        [Header("Update")]
        [SerializeField] private bool _useUnscaledTime = true;

        private Tween _scaleTween;
        private Vector3 _baseScale;
        private bool _isHovered;
        private readonly List<RaycastResult> _raycastResults = new();

        private void Awake()
        {
            if (_settings == null)
                throw new InvalidOperationException(
                    $"[HoverScale] HoverScaleSettings не назначен на '{name}'. Назначьте ScriptableObject в инспекторе.");

            _baseScale = transform.localScale;
        }

        private void OnValidate()
        {
            if (_settings == null)
                Debug.LogError(
                    $"[HoverScale] HoverScaleSettings не назначен на '{name}'. Без него компонент не запустится.",
                    this);
        }

        private void Update()
        {
            bool shouldBeHovered = UiPointer.IsOver(transform, _raycastResults);

            if (shouldBeHovered == _isHovered)
                return;

            _isHovered = shouldBeHovered;

            Vector3 target = _isHovered
                ? _baseScale * _settings.HoverMultiplier
                : _baseScale;

            TweenTo(target);
        }

        private void OnDisable()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
            _isHovered = false;

            transform.localScale = _baseScale;
        }

        public void ForceSetHovered(bool hovered)
        {
            if (hovered == _isHovered) return;

            _isHovered = hovered;
            TweenTo(_isHovered ? _baseScale * _settings.HoverMultiplier : _baseScale);
        }

        private void TweenTo(Vector3 target)
        {
            _scaleTween?.Kill();

            _scaleTween = transform
                .DOScale(target, _settings.Duration)
                .SetEase(_settings.Ease)
                .SetUpdate(_useUnscaledTime);
        }
    }
}