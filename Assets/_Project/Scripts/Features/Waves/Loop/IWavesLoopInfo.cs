using System;

namespace SibJam.Waves
{
    public interface IWavesLoopInfo
    {
        public event Action<WavesLoopState> StateChanged;

        public WavesLoopState State { get; }

        public int WaveIndex { get; }

        public int AliveEnemiesCount { get; }

        public float DelayTimeLeft { get; }
    }
}
