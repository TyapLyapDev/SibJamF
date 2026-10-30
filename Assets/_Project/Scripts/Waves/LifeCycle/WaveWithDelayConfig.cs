using System;
using UnityEngine;

namespace SibJam.Waves
{
    [Serializable]
    public class WaveWithDelayConfig
    {
        public WaveConfig Wave;
        [Tooltip("Пауза после этой волны, перед следующей")]
        [Min(0f)] public float DelayInSeconds;
    }
}
