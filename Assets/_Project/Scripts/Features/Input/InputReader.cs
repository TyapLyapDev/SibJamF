using System;
using UnityEngine.InputSystem;

namespace SibJam
{
    public class InputReader : IInputReader, IDisposable
    {
        private const string UiActionMapName = "UI";
        private const string EscapeActionName = "Escape";

        private readonly InputActionAsset _inputActions;

        private InputAction _escapeAction;

        public InputReader(InputActionAsset inputActions)
        {
            _inputActions = inputActions != null ? inputActions : throw new ArgumentNullException(nameof(inputActions));
            CreateMap();
            Enable();
        }

        public bool IsEscapePressed { get; private set; }

        public void Dispose() =>
            Disable();

        private void CreateMap()
        {
            InputActionMap uiMap = _inputActions.FindActionMap(UiActionMapName, throwIfNotFound: true);
            _escapeAction = uiMap.FindAction(EscapeActionName, throwIfNotFound: true);
        }

        private void Enable()
        {
            _escapeAction.Enable();

            _escapeAction.performed += OnEscape;
            _escapeAction.canceled += OnEscape;
        }

        private void Disable()
        {
            _escapeAction.performed -= OnEscape;
            _escapeAction.canceled -= OnEscape;

            _escapeAction.Disable();
        }

        private void OnEscape(InputAction.CallbackContext context) =>
            IsEscapePressed = context.ReadValueAsButton();
    }
}