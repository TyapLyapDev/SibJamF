using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam.Cutscenes
{
    // Сервис пропуска катсцены: первое нажатие пробела показывает подсказку на сервисном канвасе (SkipHintView),
    // второе, пока подсказка не погасла, просит пропустить
    public class SkipHint
    {
        private const float FadeSpeed = 0.5f;

        private readonly SkipHintView _view;

        public SkipHint(SkipHintView view)
        {
            _view = view ?? throw new ArgumentNullException(nameof(view));
        }

        public bool IsVisible => _view.Alpha > 0f;

        public void Hide() => _view.Alpha = 0f;

        // Вызывать каждый кадр, пока идёт катсцена. true — игрок подтвердил пропуск
        public bool WantsSkip(float deltaTime)
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
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
