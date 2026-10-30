using System;
using System.Collections.Generic;
using _Project.Scripts.Enemies;
using UnityEngine;

namespace SibJam.Waves
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "SibJam/Wave Config")]
    public class WaveConfig : ScriptableObject
    {
        public List<WaveEnemyEntry> Enemies = new();
    }

    [Serializable]
    public class WaveEnemyEntry
    {
        public EnemyConfig Enemy;
        [Min(1)] public int Count = 1;
    }
}
