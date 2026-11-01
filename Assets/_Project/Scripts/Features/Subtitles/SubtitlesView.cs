using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace SibJam
{
    // Плашка субтитров на сервисном канвасе: только вывод текста и фейд. Показывать через сервис Subtitles
    public class SubtitlesView : MonoBehaviour
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

        public bool IsAnimating => _animationCoroutine != null;
        public bool UseUnscaledTime => _useUnscaledTime;
        public Color SpeakerColor => _speakerColor;

        private void Awake()
        {
            if (_text == null)
                throw new InvalidOperationException(
                    $"[SubtitlesView] TMP_Text не назначен на '{name}'. Назначьте ссылку в инспекторе.");

            if (_canvasGroup == null)
                throw new InvalidOperationException(
                    $"[SubtitlesView] CanvasGroup не назначен на '{name}'. Назначьте ссылку в инспекторе.");

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        private void OnValidate()
        {
            if (_text == null)
                Debug.LogError($"[SubtitlesView] TMP_Text не назначен на '{name}'.", this);

            if (_canvasGroup == null)
                Debug.LogError($"[SubtitlesView] CanvasGroup не назначен на '{name}'.", this);
        }

        private void OnDisable() => StopAnimation();

        private void OnDestroy() => _fadeTween?.Kill();

        public void Show(string text, SubtitleAnimation animation)
        {
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
