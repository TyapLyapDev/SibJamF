using System;
using System.Collections.Generic;

namespace SibJam.Waves
{
    public class DelayStateMachine
    {
        private readonly List<float> _configs;
        private Action _onCompleted;

        public int CurrentIndex { get; private set; } = -1;
        public float TimeLeft { get; private set; }

        public bool HasNext => CurrentIndex + 1 < _configs.Count;
        public bool IsCompleted => _onCompleted == null;

        public DelayStateMachine(List<float> configs)
        {
            _configs = configs;
        }

        public void LaunchNextDelay(Action onCompleted)
        {
            if (_configs.Count == 0) return;

            if (HasNext)
                CurrentIndex++;
            TimeLeft = _configs[CurrentIndex];
            _onCompleted = onCompleted;
        }

        public void Cancel()
        {
            _onCompleted = null;
            TimeLeft = 0f;
            CurrentIndex = -1;
        }

        public void Tick(float deltaTime)
        {
            if (_onCompleted == null) return;

            TimeLeft -= deltaTime;
            if (TimeLeft > 0f) return;

            TimeLeft = 0f;
            var onCompleted = _onCompleted;
            _onCompleted = null;
            onCompleted.Invoke();
        }
    }
}
