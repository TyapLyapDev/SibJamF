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
        [Tooltip("Цвет имени говорящего перед репликой")]
        [SerializeField] private Color _speakerColor = new Color(1f, 0.75f, 0.35f);

        [Header("Update")]
        [Tooltip("Игнорировать Time.timeScale (например, если субтитры должны быть видны в паузе)")]
        [SerializeField] private bool _useUnscaledTime = false;

        private Tween _fadeTween;
        private Coroutine _animationCoroutine;
        private Coroutine _hideCoroutine;

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

        private void OnDisable() => StopRoutines();

        public void Show(SubtitleConfig config, float duration = 0f)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            Show(config.Speaker, config.Text, duration, config.Animation);
        }

        public void Show(string text, SubtitleAnimation animation = null) =>
            Show(null, text, 0f, animation);

        // duration > 0 — субтитр сам скроется через столько секунд после показа (но не раньше конца анимации)
        public void Show(string speaker, string text, float duration, SubtitleAnimation animation = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            StopRoutines();

            string line = string.IsNullOrEmpty(speaker)
                ? text
                : $"<color=#{ColorUtility.ToHtmlStringRGB(_speakerColor)}>{speaker}:</color> {text}";

            _text.maxVisibleCharacters = int.MaxValue;

            if (animation != null)
                _animationCoroutine = StartCoroutine(PlayAnimation(animation, line));
            else
                _text.text = line;

            if (duration > 0f)
                _hideCoroutine = StartCoroutine(HideAfter(duration));

            FadeTo(1f);
        }

        public void Hide()
        {
            StopRoutines();
            FadeTo(0f);
        }

        private IEnumerator PlayAnimation(SubtitleAnimation animation, string text)
        {
            yield return animation.Play(_text, text);
            _animationCoroutine = null;
        }

        private IEnumerator HideAfter(float duration)
        {
            if (_useUnscaledTime)
                yield return new WaitForSecondsRealtime(duration);
            else
                yield return new WaitForSeconds(duration);

            while (IsAnimating)
                yield return null;

            _hideCoroutine = null;
            FadeTo(0f);
        }

        private void StopRoutines()
        {
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
            }

            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
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
