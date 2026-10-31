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
        private readonly Anticlicker _anticlicker;

        private bool _isShow;
        private bool _wasEscapePressed;

        public GameButtonsShower(
            IInputReader inputReader,
            IPlayerInputReader playerInputReader,
            ICursorShower cursorShower,
            IPauseSwitcher pauseSwitcher,
            PopUp gameButtons,
            Anticlicker anticlicker)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _playerInputReader = playerInputReader ?? throw new ArgumentNullException(nameof(playerInputReader));
            _cursorShower = cursorShower ?? throw new ArgumentException(nameof(cursorShower));
            _pauseSwitcher = pauseSwitcher ?? throw new ArgumentNullException(nameof(pauseSwitcher));
            _gameButtons = gameButtons != null ? gameButtons : throw new ArgumentNullException(nameof(gameButtons));
            _anticlicker = anticlicker != null ? anticlicker : throw new ArgumentNullException(nameof(anticlicker));
        }

        public void Tick()
        {
            bool isEscapePressed = _inputReader.IsEscapePressed;

            if (isEscapePressed == true && _wasEscapePressed == false)
            {
                _isShow = !_isShow;

                if (_isShow)
                    Show();
                else
                    Hide();
            }

            _wasEscapePressed = isEscapePressed;
        }

        public void Show()
        {
            _isShow = true;
            _gameButtons.Show();
            _cursorShower.Show();
            _playerInputReader.Disable();
            _pauseSwitcher.Pause();
            _anticlicker.Show();
        }

        public void Hide()
        {
            _isShow = false;
            _gameButtons.Hide();
            _cursorShower.Hide();
            _playerInputReader.Enable();
            _pauseSwitcher.Unpause();
            _anticlicker.Hide();
        }
    }
}