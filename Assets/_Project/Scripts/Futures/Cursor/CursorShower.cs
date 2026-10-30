using UnityEngine;

namespace SibJam
{
    public class CursorShower : ICursorShower
    {
        public bool IsShow { get; private set; } = true;

        public void Show()
        {
            IsShow = true;
            UpdateState();
        }

        public void Hide()
        {
            IsShow = false;
            UpdateState();
        }

        private void UpdateState()
        {
            Cursor.visible = IsShow;
            Cursor.lockState = IsShow ? CursorLockMode.None : CursorLockMode.Locked;
        }
    }
}