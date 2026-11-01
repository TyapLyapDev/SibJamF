using System;
using SibJam.SecurityCameras;
using SibJam.Waves;
using UnityEngine;
using UnityEngine.Serialization;

namespace SibJam.Locations
{
    public class LocationLoop : MonoBehaviour
    {
        [SerializeField] private WavesLoop _wavesLoop;
        [SerializeField] private CameraDestructionGoal _cameraDestructionGoal;
        [Tooltip("Где появляется игрок при старте с чекпоинта этой локации")]
        [FormerlySerializedAs("_arrivalPoint")]
        [SerializeField] private Transform _respawnPoint;
        [Tooltip("Аптечка в сейф-зоне: выключена в сцене, включается при завершении локации")]
        [SerializeField] private GameObject _completionMedkit;

        public event Action<LocationLoop> Completed;
        public event Action<LocationLoopState> StateChanged;

        public IWavesLoopInfo Waves => _wavesLoop.Info;

        public ICameraDestructionGoalInfo Cameras => _cameraDestructionGoal;

        public Transform RespawnPoint => _respawnPoint;

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

            SetState(LocationLoopState.Running);
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

            SetState(LocationLoopState.Completed);
            if (_completionMedkit != null)
                _completionMedkit.SetActive(true);
            Completed?.Invoke(this);
        }

        private void SetState(LocationLoopState state)
        {
            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
