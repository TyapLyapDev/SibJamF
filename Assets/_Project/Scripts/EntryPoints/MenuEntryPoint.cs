using System;
using VContainer.Unity;

namespace SibJam
{
    public class MenuEntryPoint : IStartable
    {
        private readonly ICursorShower _cursorShower;
        private readonly IPauseSwitcher _pauseSwitcher;

        public MenuEntryPoint(ICursorShower cursorShower, IPauseSwitcher pauseSwitcher)
        {
            _cursorShower = cursorShower ?? throw new ArgumentNullException(nameof(cursorShower));
            _pauseSwitcher = pauseSwitcher ?? throw new ArgumentNullException(nameof(pauseSwitcher));
        }

        public void Start()
        {
            _cursorShower.Show();
            _pauseSwitcher.Unpause();
        }
    }
}