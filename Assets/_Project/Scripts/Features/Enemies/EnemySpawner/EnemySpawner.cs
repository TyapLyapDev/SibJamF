using _Project.Scripts.Enemies;
using SibJam.Combat;
using UnityEngine;

namespace _Project.Scripts.EnemySpawner
{
    public class EnemySpawner
    {
        public Enemy Spawn(EnemyConfig enemyConfig, Vector3 spawnPosition, IEnemyTarget target)
        {
            return Spawn(enemyConfig, spawnPosition, Quaternion.identity, target);
        }

        public Enemy Spawn(EnemyConfig enemyConfig, Vector3 spawnPosition, Quaternion rotation, IEnemyTarget target, float statsMultiplier = 1f)
        {
            if (enemyConfig == null || enemyConfig.Prefab == null)
            {
                Debug.LogError($"[EnemySpawner] Enemy config or prefab is missing: {enemyConfig}");
                return null;
            }

            var enemy = Object.Instantiate(enemyConfig.Prefab, spawnPosition, rotation);
            enemy.Init(enemyConfig, target, statsMultiplier);
            return enemy;
        }
    }
}
