using System.Collections.Generic;
using UnityEngine;

namespace SibJam.Waves
{
    [CreateAssetMenu(fileName = "WavesLoopConfig", menuName = "SibJam/Waves Loop Config")]
    public class WavesLoopConfig : ScriptableObject
    {
        public List<WaveWithDelayConfig> WavesWithDelay = new();
        [Tooltip("После последней волны повторять её бесконечно. Иначе цикл завершается")]
        public bool RepeatLastWave = true;
    }
}
