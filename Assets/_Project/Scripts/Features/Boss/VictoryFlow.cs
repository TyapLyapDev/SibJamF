using System;
using SibJam.Checkpoints;
using VContainer.Unity;

namespace SibJam.Boss
{
    // Победа над боссом — конец игры: чекпоинт стирается (Продолжить больше нечего),
    // управление и меню паузы выключаются до смены сцены, дальше играют титры
    public class VictoryFlow : IInitializable, IDisposable
    {
        private readonly BossFight _bossFight;
        private readonly ICheckpoint _checkpoint;
        private readonly IPlayerInputReader _playerInput;
        private readonly GameButtonsShower _gameButtonsShower;

        public VictoryFlow(
            BossFight bossFight,
            ICheckpoint checkpoint,
            IPlayerInputReader playerInput,
            GameButtonsShower gameButtonsShower)
        {
            _bossFight = bossFight != null ? bossFight : throw new ArgumentNullException(nameof(bossFight));
            _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
            _gameButtonsShower = gameButtonsShower ?? throw new ArgumentNullException(nameof(gameButtonsShower));
        }

        public void Initialize() => _bossFight.Won += OnWon;

        public void Dispose() => _bossFight.Won -= OnWon;

        private void OnWon()
        {
            _checkpoint.Clear();
            _gameButtonsShower.Hide();
            _gameButtonsShower.Lock();
            _playerInput.Disable();
        }
    }
}
