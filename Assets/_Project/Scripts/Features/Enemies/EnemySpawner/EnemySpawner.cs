using _Project.Scripts.Enemies;
using UnityEngine;

namespace _Project.Scripts.EnemySpawner
{
    public class EnemySpawner
    {
        public Enemy Spawn(EnemyConfig enemyConfig, Vector3 spawnPosition)
        {
            return Spawn(enemyConfig, spawnPosition, Quaternion.identity);
        }

        public Enemy Spawn(EnemyConfig enemyConfig, Vector3 spawnPosition, Quaternion rotation, float statsMultiplier = 1f)
        {
            if (enemyConfig == null || enemyConfig.Prefab == null)
            {
                Debug.LogError($"[EnemySpawner] Enemy config or prefab is missing: {enemyConfig}");
                return null;
            }

            var enemy = Object.Instantiate(enemyConfig.Prefab, spawnPosition, rotation);
            enemy.Init(enemyConfig, statsMultiplier);
            return enemy;
        }
    }
}
