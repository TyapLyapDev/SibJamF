using System;
using System.Linq;
using UnityEngine;

namespace SibJam.Waves
{
    public class WavesLifeCycle : MonoBehaviour
    {
        [SerializeField] private WavesLifeCycleConfig _wavesLifeCycleConfig;
        [SerializeField] private WaveSpawner _waveSpawner;

        public event Action Completed;

        public bool IsRunning { get; private set; }
        public bool IsCompleted { get; private set; }

        public WavesStateMachine Waves { get; private set; }
        public DelayStateMachine Delays { get; private set; }

        private void Awake()
        {
            var configs = _wavesLifeCycleConfig.WavesWithDelay;
            Waves = new WavesStateMachine(configs.Select(config => config.Wave).ToList(), _waveSpawner);
            Delays = new DelayStateMachine(configs.Select(config => config.DelayInSeconds).ToList());
        }

        private void Update()
        {
            if (!IsRunning) return;

            Waves.Tick();
            Delays.Tick(Time.deltaTime);
        }

        public void LaunchWaves()
        {
            if (IsRunning) return;

            IsRunning = true;
            IsCompleted = false;
            LaunchNextWave();
        }

        public void CompleteWaves()
        {
            if (!IsRunning) return;

            IsRunning = false;
            Waves.Cancel();
            Delays.Cancel();
            
            IsCompleted = true;
            Completed?.Invoke();
        }

        private void LaunchNextWave()
        {
            Waves.LaunchNextWave(onCompleted: OnWaveCompleted);
        }

        private void OnWaveCompleted()
        {
            if (!Waves.HasNext && !_wavesLifeCycleConfig.RepeatLastWave)
            {
                CompleteWaves();
                return;
            }

            Delays.LaunchNextDelay(onCompleted: LaunchNextWave);
        }
    }
}
