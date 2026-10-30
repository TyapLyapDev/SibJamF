using System;
using System.Collections.Generic;

namespace SibJam.Waves
{
    public class WavesStateMachine
    {
        private readonly List<WaveConfig> _configs;
        private readonly WaveSpawner _waveSpawner;

        public int CurrentIndex { get; private set; } = -1;
        public Wave CurrentWave { get; private set; }

        public bool HasNext => CurrentIndex + 1 < _configs.Count;
        public bool IsCompleted => _onCompleted == null;

        private Action _onCompleted;

        public WavesStateMachine(List<WaveConfig> configs, WaveSpawner waveSpawner)
        {
            _configs = configs;
            _waveSpawner = waveSpawner;
        }

        public void LaunchNextWave(Action onCompleted)
        {
            if (_configs.Count == 0) return;

            if (HasNext)
                CurrentIndex++;
            _onCompleted = onCompleted;
            CurrentWave = _waveSpawner.Spawn(_configs[CurrentIndex]);
        }

        public void Cancel()
        {
            _onCompleted = null;
            CurrentWave = null;
            CurrentIndex = -1;
        }

        public void Tick()
        {
            if (_onCompleted == null || !CurrentWave.IsCompleted) return;

            var onCompleted = _onCompleted;
            _onCompleted = null;
            onCompleted.Invoke();
        }
    }
}
