using System;
using VContainer.Unity;

namespace SibJam
{
    public class MenuEntryPoint : IStartable
    {
        private readonly ICursorShower _cursorShower;

        public MenuEntryPoint(ICursorShower cursorShower)
        {
            _cursorShower = cursorShower ?? throw new ArgumentNullException(nameof(cursorShower));
        }

        public void Start()
        {
            _cursorShower.Show();
        }
    }
}