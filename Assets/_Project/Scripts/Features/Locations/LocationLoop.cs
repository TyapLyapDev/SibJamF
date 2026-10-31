using System;
using SibJam.SecurityCameras;
using SibJam.Waves;
using UnityEngine;

namespace SibJam.Locations
{
    public class LocationLoop : MonoBehaviour
    {
        [SerializeField] private WavesLoop _wavesLoop;
        [SerializeField] private CameraDestructionGoal _cameraDestructionGoal;
        [SerializeField] private Transform _arrivalPoint;

        public event Action<LocationLoop> Completed;

        public IWavesLoopInfo Waves => _wavesLoop.Info;

        public ICameraDestructionGoalInfo Cameras => _cameraDestructionGoal;

        public Transform ArrivalPoint => _arrivalPoint;

        public LocationLoopState State { get; private set; } = LocationLoopState.Idle;

        private bool CanComplete => _cameraDestructionGoal.IsCompleted && !_wavesLoop.IsRunning;

        private void OnEnable()
        {
            _cameraDestructionGoal.Completed += OnCamerasDestroyed;
            _wavesLoop.Completed += TryComplete;
        }

        private void OnDisable()
        {
            _cameraDestructionGoal.Completed -= OnCamerasDestroyed;
            _wavesLoop.Completed -= TryComplete;
        }

        public void Launch()
        {
            if (State != LocationLoopState.Idle) return;

            State = LocationLoopState.Running;
            _wavesLoop.LaunchWaves();
        }

        private void OnCamerasDestroyed()
        {
            _wavesLoop.StopSpawning();
            TryComplete();
        }

        private void TryComplete()
        {
            if (State == LocationLoopState.Completed || !CanComplete) return;

            State = LocationLoopState.Completed;
            Completed?.Invoke(this);
        }
    }
}
