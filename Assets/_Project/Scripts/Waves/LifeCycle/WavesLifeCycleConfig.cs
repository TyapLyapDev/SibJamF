using System.Collections.Generic;
using UnityEngine;

namespace SibJam.Waves
{
    [CreateAssetMenu(fileName = "WavesLifeCycleConfig", menuName = "SibJam/Waves LifeCycle Config")]
    public class WavesLifeCycleConfig : ScriptableObject
    {
        public List<WaveWithDelayConfig> WavesWithDelay = new();
        [Tooltip("После последней волны повторять её бесконечно. Иначе цикл завершается")]
        public bool RepeatLastWave = true;
    }
}
