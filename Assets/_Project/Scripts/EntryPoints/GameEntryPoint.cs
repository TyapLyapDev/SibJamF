using System;
using VContainer.Unity;

namespace SibJam
{
    public class GameEntryPoint : IStartable
    {
        private readonly ICursorShower _cursorShower;

        public GameEntryPoint(ICursorShower cursorShower)
        {
            _cursorShower = cursorShower ?? throw new ArgumentNullException(nameof(cursorShower));
        }

        public void Start()
        {
            _cursorShower.Hide();
        }
    }
}