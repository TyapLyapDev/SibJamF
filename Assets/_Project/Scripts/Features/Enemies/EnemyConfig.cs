using SibJam.Pickups;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "SibJam/Enemy Config")]
    public class EnemyConfig : ScriptableObject
    {
        [field: SerializeField] public Enemy Prefab { get; private set; }

        [field: Header("Stats")]
        [field: SerializeField, Min(1f)] public float MaxHealth { get; private set; } = 100f;
        [field: SerializeField, Min(0f)] public float MoveSpeed { get; private set; } = 3f;

        [field: Header("Attack")]
        [field: SerializeField, Min(0f)] public float Damage { get; private set; } = 10f;
        [field: SerializeField, Min(0f)] public float AttackRange { get; private set; } = 1.5f;
        [field: SerializeField, Min(0f)] public float AttackCooldown { get; private set; } = 1f;

        [field: Header("Defense")]
        [field: Tooltip("Множитель урона от контратаки игрока (удар сразу после блока или парирования)")]
        [field: SerializeField, Min(0f)] public float CounterDamageMultiplier { get; private set; } = 2f;

        [field: Header("Drop")]
        [field: Tooltip("Мини-аптечка, которая может выпасть при смерти")]
        [field: SerializeField] public HealthPickup DropPrefab { get; private set; }
        [field: SerializeField, Range(0f, 1f)] public float DropChance { get; private set; } = 0.08f;
    }
}
