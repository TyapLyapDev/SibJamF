using System;
using SibJam;
using SibJam.Combat;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Project.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        // Только смерть от урона, без выгрузки/рестарта — для наград за убийство
        public static event Action<Enemy> Killed;

        public event Action<Enemy> Died;
        public event Action<float> Damaged;

        public float Health { get; private set; }
        public float MaxHealth { get; private set; }
        public bool IsDead { get; private set; }

        // Множитель силы от роста сложности волн: хп сейчас, урон — когда появится атака
        public float StatsMultiplier { get; private set; } = 1f;

        public float CounterDamageMultiplier { get; private set; } = 1f;

        private EnemyConfig _config;
        private WorldHealthBar _healthBar;

        public void Init(EnemyConfig config, IEnemyTarget target, float statsMultiplier = 1f)
        {
            _config = config;
            StatsMultiplier = statsMultiplier;
            CounterDamageMultiplier = config.CounterDamageMultiplier;
            MaxHealth = config.MaxHealth * statsMultiplier;
            Health = MaxHealth;
            IsDead = false;

            if (!TryGetComponent(out _healthBar))
                _healthBar = gameObject.AddComponent<WorldHealthBar>();

            if (TryGetComponent(out EnemyBrain brain))
                brain.Init(config, target);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Health = Mathf.Max(0f, Health - amount);
            Damaged?.Invoke(amount);
            if (_healthBar != null && MaxHealth > 0f)
                _healthBar.SetValue(Health / MaxHealth);
            if (Health <= 0f)
                Die();
        }

        public void Die()
        {
            if (IsDead) return;
            IsDead = true;
            TryDropLoot();
            Killed?.Invoke(this);
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
