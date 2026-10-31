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

        [Header("Рост сложности")]
        [Tooltip("Прибавка к хп врагов за каждую следующую волну: 0.2 — вторая волна x1.2, третья x1.4…")]
        [Min(0f)] public float StatsGrowthPerWave = 0.2f;
        [Tooltip("Сколько врагов добавлять к каждой записи волны за каждый повтор последней волны")]
        [Min(0)] public int ExtraEnemiesPerRepeat = 1;
    }
}
