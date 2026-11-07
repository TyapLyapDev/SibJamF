using UnityEngine;

namespace SibJam.Combat
{
    public readonly struct PlayerHit
    {
        public PlayerHit(float damage, float blockedDamage = -1f, bool canParry = false, Object source = null)
        {
            Damage = damage;
            BlockedDamage = blockedDamage;
            CanParry = canParry;
            Source = source;
        }

        public float Damage { get; }
        public float BlockedDamage { get; }
        public bool CanParry { get; }
        public Object Source { get; }

        public bool CanBlock => BlockedDamage >= 0f;
    }
}