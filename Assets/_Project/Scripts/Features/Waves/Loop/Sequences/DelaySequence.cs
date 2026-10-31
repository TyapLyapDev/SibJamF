using System.Collections.Generic;

namespace SibJam.Waves
{
    public class DelaySequence
    {
        private readonly List<float> _delays;

        public DelaySequence(List<float> delays)
        {
            _delays = delays;
        }

        public int CurrentIndex { get; private set; } = -1;

        public bool HasNext => CurrentIndex + 1 < _delays.Count;

        public float Next()
        {
            if (HasNext)
                CurrentIndex++;

            return _delays[CurrentIndex];
        }

        public void Reset() => CurrentIndex = -1;
    }
}
