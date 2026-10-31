using System.Collections.Generic;

namespace SibJam.Waves
{
    public class WaveSequence
    {
        private readonly List<WaveConfig> _configs;

        public WaveSequence(List<WaveConfig> configs)
        {
            _configs = configs;
        }

        public int CurrentIndex { get; private set; } = -1;

        public bool IsEmpty => _configs.Count == 0;

        public bool HasNext => CurrentIndex + 1 < _configs.Count;

        public WaveConfig Next()
        {
            if (HasNext)
                CurrentIndex++;

            return _configs[CurrentIndex];
        }

        public void Reset() => CurrentIndex = -1;
    }
}
