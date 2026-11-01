using System;
using SibJam.Combat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public event Action<Enemy> Died;

        public float Health { get; private set; }
        public bool IsDead { get; private set; }

        // Множитель силы от роста сложности волн: хп сейчас, урон — когда появится атака
        public float StatsMultiplier { get; private set; } = 1f;

        public float CounterDamageMultiplier { get; private set; } = 1f;

        private EnemyConfig _config;

        public void Init(EnemyConfig config, IEnemyTarget target, float statsMultiplier = 1f)
        {
            _config = config;
            StatsMultiplier = statsMultiplier;
            CounterDamageMultiplier = config.CounterDamageMultiplier;
            Health = config.MaxHealth * statsMultiplier;
            IsDead = false;

            if (TryGetComponent(out EnemyBrain brain))
                brain.Init(config, target);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            if (Health <= 0f)
                Die();
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            TryDropLoot();
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        // Только при смерти от урона: уничтожение при выгрузке/рестарте ничего не роняет
        private void TryDropLoot()
        {
            if (_config == null || _config.DropPrefab == null || Random.value >= _config.DropChance) return;

            Instantiate(_config.DropPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
        }

        private void OnDestroy()
        {
            if (IsDead) return;
            IsDead = true;
            Died?.Invoke(this);
        }
    }
}
