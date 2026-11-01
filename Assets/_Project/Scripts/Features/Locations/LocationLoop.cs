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
        [Tooltip("Без камер локация сразу пройдена. Выключить, если конец локации решает что-то другое (бой с боссом)")]
        [SerializeField] private bool _completeWithoutCameras = true;

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

            // Локация без камер — проходная, мобов нет
            if (_cameraDestructionGoal.TotalCount == 0)
            {
                if (_completeWithoutCameras)
                    ForceComplete();
                return;
            }

            _wavesLoop.LaunchWaves();
        }

        private void OnCamerasDestroyed()
        {
            _wavesLoop.StopSpawning();
            // Иначе застрявший где-то враг навсегда держит дверь закрытой
            _wavesLoop.KillRemainingEnemies();
            TryComplete();
        }

        private void TryComplete()
        {
            if (State == LocationLoopState.Completed || !CanComplete) return;

            ForceComplete();
        }

        private void ForceComplete()
        {
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
