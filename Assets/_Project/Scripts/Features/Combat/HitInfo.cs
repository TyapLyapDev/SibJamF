using UnityEngine;

namespace SibJam.Combat
{
    public readonly struct HitInfo
    {
        public HitInfo(float damage, GameObject source, Vector3 direction, bool parryable)
        {
            Damage = damage;
            Source = source;
            Direction = direction;
            Parryable = parryable;
        }

        public float Damage { get; }
        public GameObject Source { get; }
        public Vector3 Direction { get; }
        public bool Parryable { get; }
    }

    public enum HitResult
    {
        Hit,
        Parried,
        Dodged
    }
}
