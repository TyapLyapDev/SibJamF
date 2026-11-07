using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Сервис пропуска катсцены: первое нажатие пробела показывает подсказку на сервисном канвасе (SkipHintView),
    // второе, пока подсказка не погасла, просит пропустить
    public class SkipHint
    {
        private const float FadeSpeed = 0.5f;

        private readonly SkipHintView _view;
        private readonly CutsceneInputReader _input;

        public SkipHint(SkipHintView view, CutsceneInputReader input)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public bool IsVisible => _view.Alpha > 0f;

        public void Hide() => _view.Alpha = 0f;

        // Вызывать каждый кадр, пока идёт катсцена. true — игрок подтвердил пропуск
        public bool WantsSkip(float deltaTime)
        {
            if (!_input.IsSkipPressed)
            {
                _view.Alpha = Mathf.MoveTowards(_view.Alpha, 0f, deltaTime * FadeSpeed);
                return false;
            }

            if (IsVisible)
                return true;

            _view.Alpha = 1f;
            return false;
        }
    }
}
