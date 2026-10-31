using System;
using VContainer.Unity;

namespace SibJam
{
    public class GameEntryPoint : IStartable
    {
        private readonly ICursorShower _cursorShower;
        private readonly Music _music;

        public GameEntryPoint(ICursorShower cursorShower, Music music)
        {
            _cursorShower = cursorShower ?? throw new ArgumentNullException(nameof(cursorShower));
            _music = music != null ? music : throw new ArgumentNullException(nameof(music));
        }

        public void Start()
        {
            _cursorShower.Hide();
            _music.PlayGame();
        }
    }
}