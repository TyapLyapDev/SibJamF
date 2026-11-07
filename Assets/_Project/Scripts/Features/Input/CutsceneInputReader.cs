using System;
using UnityEngine.InputSystem;

namespace SibJam
{
    public class CutsceneInputReader : IDisposable
    {
        private readonly InputAction _skipAction;
        private int _activeCount;

        public CutsceneInputReader(InputActionAsset inputActions)
        {
            if (inputActions == null)
                throw new ArgumentNullException(nameof(inputActions));

            _skipAction = inputActions.FindAction("Cutscene/Skip", throwIfNotFound: true);
        }

        public bool IsSkipPressed => _skipAction.enabled && _skipAction.WasPressedThisFrame();

        public void Enable()
        {
            if (_activeCount++ == 0)
                _skipAction.Enable();
        }

        public void Disable()
        {
            if (_activeCount > 0 && --_activeCount == 0)
                _skipAction.Disable();
        }

        public void Dispose() => _skipAction.Disable();
    }
}
