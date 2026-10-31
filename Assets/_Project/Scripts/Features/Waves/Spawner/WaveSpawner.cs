using _Project.Scripts.EnemySpawner;
using UnityEngine;

namespace SibJam.Waves
{
    public class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] _spawnPoints;

        private readonly EnemySpawner _enemySpawner = new();
        private int _lastSpawnPointIndex = -1;

        public Wave Spawn(WaveConfig config) => Spawn(config, WaveDifficulty.Default);

        public Wave Spawn(WaveConfig config, WaveDifficulty difficulty)
        {
            var wave = new Wave();
            if (config == null) return wave;

            foreach (var entry in config.Enemies)
            {
                for (var i = 0; i < entry.Count + difficulty.ExtraEnemiesPerEntry; i++)
                {
                    var spawnPoint = GetSpawnPoint();
                    var enemy = _enemySpawner.Spawn(entry.Enemy, spawnPoint.position, spawnPoint.rotation, difficulty.StatsMultiplier);
                    if (enemy != null)
                        wave.BindEnemy(enemy);
                }
            }

            return wave;
        }

        private Transform GetSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return transform;

            if (_spawnPoints.Length == 1)
                return _spawnPoints[0];

            int index;
            do
            {
                index = Random.Range(0, _spawnPoints.Length);
            } while (index == _lastSpawnPointIndex);

            _lastSpawnPointIndex = index;
            return _spawnPoints[index];
        }
    }
}
