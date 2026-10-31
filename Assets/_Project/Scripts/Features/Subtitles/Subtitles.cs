using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SibJam
{
    public class Subtitles : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] private TMP_Text _text;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField, Min(0f)] private float _fadeDuration = 0.2f;

        [Header("Update")]
        [Tooltip("Игнорировать Time.timeScale (например, если субтитры должны быть видны в паузе)")]
        [SerializeField] private bool _useUnscaledTime = false;

        private Tween _fadeTween;
        private Coroutine _animationCoroutine;

        public bool IsAnimating => _animationCoroutine != null;

        private void Awake()
        {
            if (_text == null)
                throw new InvalidOperationException(
                    $"[Subtitles] TMP_Text не назначен на '{name}'. Назначьте ссылку в инспекторе.");

            if (_canvasGroup == null)
                throw new InvalidOperationException(
                    $"[Subtitles] CanvasGroup не назначен на '{name}'. Назначьте ссылку в инспекторе.");

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void OnValidate()
        {
            if (_text == null)
                Debug.LogError($"[Subtitles] TMP_Text не назначен на '{name}'.", this);

            if (_canvasGroup == null)
                Debug.LogError($"[Subtitles] CanvasGroup не назначен на '{name}'.", this);
        }

        private void OnDisable() => StopAnimation();

        public void Show(SubtitleConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Show(config.Text, config.Animation);
        }

        public void Show(string text, SubtitleAnimation animation = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            StopAnimation();

            _text.maxVisibleCharacters = int.MaxValue;

            if (animation != null)
                _animationCoroutine = StartCoroutine(PlayAnimation(animation, text));
            else
                _text.text = text;

            FadeTo(1f);
        }

        public void Hide()
        {
            StopAnimation();
            FadeTo(0f);
        }

        private IEnumerator PlayAnimation(SubtitleAnimation animation, string text)
        {
            yield return animation.Play(_text, text);
            _animationCoroutine = null;
        }

        private void StopAnimation()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }
        }

        private void FadeTo(float targetAlpha)
        {
            _fadeTween?.Kill();
            _fadeTween = _canvasGroup
                .DOFade(targetAlpha, _fadeDuration)
                .SetUpdate(_useUnscaledTime);
        }
    }
}