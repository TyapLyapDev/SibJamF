using System;
using _Project.Scripts.Enemies;
using UnityEngine;

namespace SibJam.Player
{
    public class PlayerHealth : MonoBehaviour, IPlayerHealthInfo
    {
        [SerializeField, Min(1f)] private float _max = 100f;

        [Header("Regen")]
        [SerializeField, Min(0f)] private float _regenPerSecond = 2f;
        [SerializeField, Min(0f)] private float _regenDelay = 3f;
        [SerializeField, Min(0f)] private float _healOnKill = 5f;

        public event Action Changed;
        public event Action<float> Damaged;
        public event Action Died;

        // Игрок выключен до конца катсцены, поэтому не полагаемся на Awake: до первого изменения хп полное
        private float? _current;

        public float Current
        {
            get => _current ?? _max;
            private set => _current = value;
        }

        public float Max => _max;
        public bool IsDead => Current <= 0f;

        private float _lastDamageTime = float.NegativeInfinity;

        private void OnEnable() => Enemy.Killed += OnEnemyKilled;
        private void OnDisable() => Enemy.Killed -= OnEnemyKilled;

        private void OnEnemyKilled(Enemy _) => Heal(_healOnKill);

        private void Update()
        {
            if (IsDead || Current >= _max || _regenPerSecond <= 0f) return;
            if (Time.time - _lastDamageTime < _regenDelay) return;

            Heal(_regenPerSecond * Time.deltaTime);
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

            _lastDamageTime = Time.time;
            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);
            Changed?.Invoke();

            if (IsDead)
                Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;

            Current = Mathf.Min(_max, Current + amount);
            Changed?.Invoke();
        }

        public void Restore()
        {
            Current = _max;
            Changed?.Invoke();
        }
    }
}
