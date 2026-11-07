using System;
using SibJam.Locations;
using SibJam.SecurityCameras;
using SibJam.Waves;
using UnityEngine;
using _Project.Scripts.Enemies;

namespace SibJam
{
    public class CurrentLocationInfo : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;

        private LocationLoop _subscribedLocation;
        private TrainingDummyWaveSpawner _training;
        private bool _showTraining;
        private bool _trainingVisible;

        public event Action LocationChanged;

        // Локация запущена (игрок вошёл) и ещё не пройдена — только тогда показываем её HUD
        public event Action ActiveChanged;

        public int LocationIndex => _gameLoop.CurrentLocationIndex;

        public bool IsActive => _showTraining ? _trainingVisible : Location.State == LocationLoopState.Running;

        public IWavesLoopInfo Waves => _showTraining ? _training.Info : Location.Waves;

        public ICameraDestructionGoalInfo Cameras => _showTraining ? _training : Location.Cameras;

        private LocationLoop Location => _gameLoop.CurrentLocationLoop;

        public void SetTraining(TrainingDummyWaveSpawner training)
        {
            _training = training;
            RefreshTraining();
        }

        private void Update() => RefreshTraining();

        private void RefreshTraining()
        {
            bool showTraining = _training != null && LocationIndex == 0 && _training.IsPlayerInside;
            bool visible = showTraining;
            bool sourceChanged = showTraining != _showTraining;
            bool visibilityChanged = visible != _trainingVisible;
            _showTraining = showTraining;
            _trainingVisible = visible;
            if (sourceChanged) LocationChanged?.Invoke();
            if (sourceChanged || visibilityChanged) ActiveChanged?.Invoke();
        }

        private void OnEnable()
        {
            _gameLoop.LocationChanged += OnLocationChanged;
            SubscribeLocation();
        }

        private void OnDisable()
        {
            _gameLoop.LocationChanged -= OnLocationChanged;
            UnsubscribeLocation();
        }

        private void OnLocationChanged(LocationLoop location)
        {
            SubscribeLocation();
            RefreshTraining();
            LocationChanged?.Invoke();
            ActiveChanged?.Invoke();
        }

        private void SubscribeLocation()
        {
            UnsubscribeLocation();
            _subscribedLocation = Location;
            _subscribedLocation.StateChanged += OnLocationStateChanged;
        }

        private void UnsubscribeLocation()
        {
            if (_subscribedLocation != null)
                _subscribedLocation.StateChanged -= OnLocationStateChanged;

            _subscribedLocation = null;
        }

        private void OnLocationStateChanged(LocationLoopState state) => ActiveChanged?.Invoke();
    }
}
