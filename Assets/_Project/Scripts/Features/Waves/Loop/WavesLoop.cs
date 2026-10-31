using System;
using System.Linq;
using UnityEngine;

namespace SibJam.Waves
{
    public class WavesLoop : MonoBehaviour
    {
        [SerializeField] private WavesLoopConfig _wavesLoopConfig;
        [SerializeField] private WaveSpawner _waveSpawner;

        private WavesStateMachine _stateMachine;

        public event Action Completed;

        public IWavesLoopInfo Info => _stateMachine;

        public bool IsRunning => 
            Info.State is WavesLoopState.Wave or WavesLoopState.Delay or WavesLoopState.WaitingForEnemiesKilled;
        
        public bool IsCompleted => 
            Info.State == WavesLoopState.Completed;

        private void Awake()
        {
            var configs = _wavesLoopConfig.WavesWithDelay;

            _stateMachine = new WavesStateMachine(
                new WaveSequence(configs.Select(config => config.Wave).ToList()),
                new DelaySequence(configs.Select(config => config.DelayInSeconds).ToList()),
                _waveSpawner,
                _wavesLoopConfig.RepeatLastWave);
        }

        private void OnEnable() => _stateMachine.Completed += OnCompleted;

        private void OnDisable() => _stateMachine.Completed -= OnCompleted;

        private void Update() => _stateMachine.Tick(Time.deltaTime);

        public void LaunchWaves() => _stateMachine.Launch();

        public void StopSpawning() => _stateMachine.StopSpawning();

        public void CompleteWaves() => _stateMachine.Complete();

        private void OnCompleted() => Completed?.Invoke();
    }
}
