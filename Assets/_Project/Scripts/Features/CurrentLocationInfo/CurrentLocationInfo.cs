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

        public event Action LocationChanged;

        // Локация запущена (игрок вошёл) и ещё не пройдена — только тогда показываем её HUD
        public event Action ActiveChanged;

        public int LocationIndex => _gameLoop.CurrentLocationIndex;

        public bool IsActive => Location.State == LocationLoopState.Running;

        public IWavesLoopInfo Waves => Location.Waves;

        public ICameraDestructionGoalInfo Cameras => Location.Cameras;

        private LocationLoop Location => _gameLoop.CurrentLocationLoop;

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
