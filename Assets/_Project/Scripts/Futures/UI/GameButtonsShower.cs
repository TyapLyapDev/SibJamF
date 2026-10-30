using System;
using VContainer.Unity;

namespace SibJam
{
    public class GameButtonsShower : ITickable
    {
        private readonly IInputReader _inputReader;
        private readonly IPlayerInputReader _playerInputReader;
        private readonly ICursorShower _cursorShower;
        private readonly IPauseSwitcher _pauseSwitcher;
        private readonly PopUp _gameButtons;

        private bool _isShow;
        private bool _wasEscapePressed;

        public GameButtonsShower(
            IInputReader inputReader,
            IPlayerInputReader playerInputReader,
            ICursorShower cursorShower,
            IPauseSwitcher pauseSwitcher,
            PopUp gameButtons)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _playerInputReader = playerInputReader ?? throw new ArgumentNullException(nameof(playerInputReader));
            _cursorShower = cursorShower ?? throw new ArgumentException(nameof(cursorShower));
            _pauseSwitcher = pauseSwitcher ?? throw new ArgumentNullException(nameof(pauseSwitcher));
            _gameButtons = gameButtons != null ? gameButtons : throw new ArgumentNullException(nameof(gameButtons));
        }

        public void Tick()
        {
            bool isEscapePressed = _inputReader.IsEscapePressed;

            if (isEscapePressed == true && _wasEscapePressed == false)
            {
                _isShow = !_isShow;

                if (_isShow)
                {
                    _gameButtons.Show();
                    _cursorShower.Show();
                    _playerInputReader.Disable();
                    _pauseSwitcher.Pause();
                }
                else
                {
                    _gameButtons.Hide();
                    _cursorShower.Hide();
                    _playerInputReader.Enable();
                    _pauseSwitcher.Unpause();
                }
            }

            _wasEscapePressed = isEscapePressed;
        }
    }
}