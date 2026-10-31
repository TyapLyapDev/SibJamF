using System;
using VContainer.Unity;

namespace SibJam
{
    public class MenuEntryPoint : IStartable
    {
        private readonly ICursorShower _cursorShower;
        private readonly IPauseSwitcher _pauseSwitcher;
        private readonly Music _music;

        public MenuEntryPoint(
            ICursorShower cursorShower, 
            IPauseSwitcher pauseSwitcher,
            Music music)
        {
            _cursorShower = cursorShower ?? throw new ArgumentNullException(nameof(cursorShower));
            _pauseSwitcher = pauseSwitcher ?? throw new ArgumentNullException(nameof(pauseSwitcher));
            _music = music != null ? music : throw new ArgumentNullException(nameof(music));
        }

        public void Start()
        {
            _cursorShower.Show();
            _pauseSwitcher.Unpause();
            _music.PlayMenu();
        }
    }
}