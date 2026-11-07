using System;
using SibJam.Locations;
using SibJam.SecurityCameras;
using SibJam.Waves;
using UnityEngine;

namespace SibJam
{
    public class CurrentLocationInfo : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;

        private LocationLoop _subscribedLocation;
        private IWavesLoopInfo _overrideWaves;
        private ICameraDestructionGoalInfo _overrideCameras;
        private bool _hasOverride;

        public event Action LocationChanged;

        // Локация запущена (игрок вошёл) и ещё не пройдена — только тогда показываем её HUD
        public event Action ActiveChanged;

        public int LocationIndex => _gameLoop.CurrentLocationIndex;

        public bool IsActive => _hasOverride || Location.State == LocationLoopState.Running;

        public IWavesLoopInfo Waves => _hasOverride ? _overrideWaves : Location.Waves;

        public ICameraDestructionGoalInfo Cameras => _hasOverride ? _overrideCameras : Location.Cameras;

        private LocationLoop Location => _gameLoop.CurrentLocationLoop;

        // Зона вне GameLoop (например, тренировка) показывает в HUD свои волны и камеры
        public void ShowOverride(IWavesLoopInfo waves, ICameraDestructionGoalInfo cameras)
        {
            _overrideWaves = waves;
            _overrideCameras = cameras;
            _hasOverride = true;
            NotifyChanged();
        }

        public void ClearOverride()
        {
            _hasOverride = false;
            _overrideWaves = null;
            _overrideCameras = null;
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            LocationChanged?.Invoke();
            ActiveChanged?.Invoke();
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
            NotifyChanged();
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
