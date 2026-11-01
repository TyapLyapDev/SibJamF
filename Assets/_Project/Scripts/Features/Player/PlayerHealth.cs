using System;
using UnityEngine;

namespace SibJam.Player
{
    public class PlayerHealth : MonoBehaviour, IPlayerHealthInfo
    {
        [SerializeField, Min(1f)] private float _max = 100f;

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

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;

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
