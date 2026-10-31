using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SibJam
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HoverShake : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private ShakeSettings _settings;

        [Header("Update")]
        [SerializeField] private bool _useUnscaledTime = true;

        private RectTransform _rect;
        private Tween _shakeTween;
        private Vector2 _baseAnchoredPosition;
        private readonly List<RaycastResult> _raycastResults = new();

        private void Awake()
        {
            if (_settings == null)
                throw new InvalidOperationException(
                    $"[HoverShake] ShakeSettings не назначен на '{name}'. Назначьте ScriptableObject в инспекторе.");

            _rect = GetComponent<RectTransform>();
            _baseAnchoredPosition = _rect.anchoredPosition;
        }

        private void OnValidate()
        {
            if (_settings == null)
                Debug.LogError(
                    $"[HoverShake] ShakeSettings не назначен на '{name}'. Без него компонент не запустится.",
                    this);
        }

        private void Update()
        {
            if (UiPointer.IsOver(transform, _raycastResults))
            {
                if (!IsShaking())
                    StartShake();
            }
            else if (IsShaking())
            {
                StopShake();
            }
        }

        private void OnDisable() => StopShake();

        private void OnDestroy() => StopShake();

        public void ForceStartShake() => StartShake();
        public void ForceStopShake() => StopShake();

        private bool IsShaking() =>
            _shakeTween != null && _shakeTween.IsActive() && _shakeTween.IsPlaying();

        private void StartShake()
        {
            if (_shakeTween != null)
            {
                _shakeTween.Kill();
                _shakeTween = null;
            }

            _baseAnchoredPosition = _rect.anchoredPosition;

            _shakeTween = _rect
                .DOShakeAnchorPos(
                    _settings.DurationPerLoop,
                    _settings.Strength,
                    _settings.Vibrato,
                    _settings.Randomness,
                    snapping: false,
                    fadeOut: false)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(_useUnscaledTime)
                .OnKill(() =>
                {
                    if (_rect != null)
                        _rect.anchoredPosition = _baseAnchoredPosition;
                });
        }

        private void StopShake()
        {
            if (_shakeTween == null)
                return;

            _shakeTween.Kill();
            _shakeTween = null;
        }
    }
}