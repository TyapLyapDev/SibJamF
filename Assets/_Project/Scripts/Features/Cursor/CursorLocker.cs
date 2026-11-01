using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SibJam
{
    // Курсор скрыт и залочен с самого старта; Esc — отпустить, клик — снова залочить
    public class CursorLocker : MonoBehaviour
    {
        private readonly ICursorShower _cursorShower = new CursorShower();

        public static bool IsLocked => Cursor.lockState == CursorLockMode.Locked;

        private void Start() => _cursorShower.Hide();

        private void OnDisable() => _cursorShower.Show();

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                _cursorShower.Show();
            else if (!IsLocked && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUi())
                _cursorShower.Hide();
        }

        // Клик по кнопке (меню паузы, экран поражения) не должен прятать курсор, иначе кнопка не нажмётся
        private static bool IsPointerOverUi() =>
            EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
