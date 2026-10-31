using System;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    public class Enemy : MonoBehaviour
    {
        public event Action<Enemy> Died;

        public float Health { get; private set; }
        public bool IsDead { get; private set; }

        // Множитель силы от роста сложности волн: хп сейчас, урон — когда появится атака
        public float StatsMultiplier { get; private set; } = 1f;

        public void Init(EnemyConfig config, float statsMultiplier = 1f)
        {
            StatsMultiplier = statsMultiplier;
            Health = config.MaxHealth * statsMultiplier;
            IsDead = false;
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
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (IsDead) return;
            IsDead = true;
            Died?.Invoke(this);
        }
    }
}
