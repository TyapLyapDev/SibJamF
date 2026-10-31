using System;

namespace SibJam.Waves
{
    public class WavesStateMachine : IWavesLoopInfo
    {
        private readonly WaveSequence _waves;
        private readonly DelaySequence _delays;
        private readonly WaveSpawner _waveSpawner;
        private readonly bool _repeatLastWave;

        public WavesStateMachine(WaveSequence waves, DelaySequence delays, WaveSpawner waveSpawner, bool repeatLastWave)
        {
            _waves = waves;
            _delays = delays;
            _waveSpawner = waveSpawner;
            _repeatLastWave = repeatLastWave;
        }

        public event Action Completed;
        public event Action<WavesLoopState> StateChanged;

        public WavesLoopState State { get; private set; } = WavesLoopState.Idle;

        public Wave CurrentWave { get; private set; }

        public float DelayTimeLeft { get; private set; }

        public int WaveIndex => _waves.CurrentIndex;

        public int AliveEnemiesCount => CurrentWave?.AliveCount ?? 0;

        public void Launch()
        {
            if (State is not (WavesLoopState.Idle or WavesLoopState.Completed)) return;

            _waves.Reset();
            _delays.Reset();
            CurrentWave = null;

            if (_waves.IsEmpty)
                EnterCompleted();
            else
                EnterWave();
        }

        public void StopSpawning()
        {
            switch (State)
            {
                case WavesLoopState.Wave:
                    EnterWaitingForEnemiesKilled();
                    break;

                case WavesLoopState.Delay:
                    EnterCompleted();
                    break;
            }
        }

        public void Complete()
        {
            if (State is WavesLoopState.Idle or WavesLoopState.Completed) return;

            EnterCompleted();
        }

        public void Tick(float deltaTime)
        {
            switch (State)
            {
                case WavesLoopState.Wave:
                    if (CurrentWave.IsCompleted)
                        OnWaveCleared();
                    break;

                case WavesLoopState.Delay:
                    DelayTimeLeft -= deltaTime;
                    if (DelayTimeLeft <= 0f)
                        EnterWave();
                    break;

                case WavesLoopState.WaitingForEnemiesKilled:
                    if (CurrentWave.IsCompleted)
                        EnterCompleted();
                    break;
            }
        }

        private void OnWaveCleared()
        {
            if (!_waves.HasNext && !_repeatLastWave)
                EnterCompleted();
            else
                EnterDelay();
        }

        private void EnterWave()
        {
            DelayTimeLeft = 0f;
            CurrentWave = _waveSpawner.Spawn(_waves.Next());
            SetState(WavesLoopState.Wave);
        }

        private void EnterDelay()
        {
            DelayTimeLeft = _delays.Next();
            SetState(WavesLoopState.Delay);
        }

        private void EnterWaitingForEnemiesKilled()
        {
            if (CurrentWave.IsCompleted)
            {
                EnterCompleted();
                return;
            }

            SetState(WavesLoopState.WaitingForEnemiesKilled);
        }

        private void EnterCompleted()
        {
            DelayTimeLeft = 0f;
            SetState(WavesLoopState.Completed);
            Completed?.Invoke();
        }

        private void SetState(WavesLoopState state)
        {
            if (State == state) return;

            State = state;
            StateChanged?.Invoke(state);
        }
    }
}
