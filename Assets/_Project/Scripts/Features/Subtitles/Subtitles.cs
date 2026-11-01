using System;
using DG.Tweening;
using UnityEngine;

namespace SibJam
{
    // Сервис субтитров: показывает строку на сервисном канвасе (SubtitlesView). Новая строка заменяет старую
    public class Subtitles : IDisposable
    {
        private const float AnimationCheckInterval = 0.1f;

        private readonly SubtitlesView _view;
        private Tween _hideTween;

        public Subtitles(SubtitlesView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsAnimating => _view.IsAnimating;

        public void Show(string text, SubtitleAnimation animation = null) =>
            Show(null, text, 0f, animation);

        // duration > 0 — субтитр сам скроется через столько секунд после показа (но не раньше конца анимации)
        public void Show(string speaker, string text, float duration, SubtitleAnimation animation = null)
        {
            if (string.IsNullOrEmpty(text))
                return;

            _hideTween?.Kill();

            string line = string.IsNullOrEmpty(speaker)
                ? text
                : $"<color=#{ColorUtility.ToHtmlStringRGB(_view.SpeakerColor)}>{speaker}:</color> {text}";

            _view.Show(line, animation);

            if (duration > 0f)
                ScheduleHide(duration);
        }

        public void Hide()
        {
            _hideTween?.Kill();
            _view.Hide();
        }

        public void Dispose() => _hideTween?.Kill();

        private void ScheduleHide(float delay)
        {
            _hideTween = DOVirtual.DelayedCall(delay, () =>
            {
                if (_view.IsAnimating)
                    ScheduleHide(AnimationCheckInterval);
                else
                    _view.Hide();
            }, _view.UseUnscaledTime);
        }
    }
}
