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

        public event Action LocationChanged;

        public int LocationIndex => _gameLoop.CurrentLocationIndex;

        public IWavesLoopInfo Waves => Location.Waves;

        public ICameraDestructionGoalInfo Cameras => Location.Cameras;

        private LocationLoop Location => _gameLoop.CurrentLocationLoop;

        private void OnEnable() => _gameLoop.LocationChanged += OnLocationChanged;

        private void OnDisable() => _gameLoop.LocationChanged -= OnLocationChanged;

        private void OnLocationChanged(LocationLoop location) => LocationChanged?.Invoke();
    }
}
