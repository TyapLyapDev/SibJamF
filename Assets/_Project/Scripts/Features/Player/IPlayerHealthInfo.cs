using System;

namespace SibJam.Player
{
    public interface IPlayerHealthInfo
    {
        public event Action Changed;
        public event Action<float> Damaged;
        public event Action Died;

        public float Current { get; }

        public float Max { get; }

        public bool IsDead { get; }
    }
}
