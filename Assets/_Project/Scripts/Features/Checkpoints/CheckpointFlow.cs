using System;
using SibJam.Cutscenes;
using SibJam.Locations;
using SibJam.Player;
using UnityEngine;
using VContainer.Unity;

namespace SibJam.Checkpoints
{
    // Чекпоинт — сейвзона перед дверью локации: пишется, как только предыдущая зачищена.
    // При «продолжить» игрок появляется там в обход катсцены, локация запускается, когда он сам войдёт
    public class CheckpointFlow : IInitializable, IStartable, IDisposable
    {
        private readonly ICheckpoint _checkpoint;
        private readonly GameLoop _gameLoop;
        private readonly Cutscene _cutscene;
        private readonly PlayerHealth _player;

        private bool _isRestoring;

        public CheckpointFlow(ICheckpoint checkpoint, GameLoop gameLoop, Cutscene cutscene, PlayerHealth player)
        {
            _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
            _gameLoop = gameLoop != null ? gameLoop : throw new ArgumentNullException(nameof(gameLoop));
            _cutscene = cutscene != null ? cutscene : throw new ArgumentNullException(nameof(cutscene));
            _player = player != null ? player : throw new ArgumentNullException(nameof(player));
        }

        // Вызывается сразу после сборки контейнера — до Start катсцены, поэтому успеваем отменить её автозапуск
        public void Initialize()
        {
            _isRestoring = _checkpoint.IsContinueRequested && _checkpoint.HasSaved;
            _checkpoint.ConsumeRequest();

            if (_isRestoring)
                _cutscene.CancelAutoPlay();

            _gameLoop.LocationChanged += OnLocationChanged;
            _gameLoop.LocationLaunched += OnLocationLaunched;
            _gameLoop.Completed += OnGameCompleted;
        }

        public void Start()
        {
            if (_isRestoring)
                Restore(_checkpoint.LocationIndex);
        }

        public void Dispose()
        {
            _gameLoop.LocationChanged -= OnLocationChanged;
            _gameLoop.LocationLaunched -= OnLocationLaunched;
            _gameLoop.Completed -= OnGameCompleted;
        }

        private void Restore(int locationIndex)
        {
            LocationLoop location = _gameLoop.StartFrom(locationIndex);

            if (location.RespawnPoint == null)
                Debug.LogWarning($"[CheckpointFlow] {location.name} has no RespawnPoint, player stays at scene position");

            _player.GetComponent<PlayerCutsceneHandOver>().AppearAt(location.RespawnPoint);
        }

        private void OnLocationChanged(LocationLoop location) => _checkpoint.Save(_gameLoop.CurrentLocationIndex);

        private void OnLocationLaunched(LocationLoop location) => _checkpoint.Save(_gameLoop.CurrentLocationIndex);

        private void OnGameCompleted() => _checkpoint.Clear();
    }
}
