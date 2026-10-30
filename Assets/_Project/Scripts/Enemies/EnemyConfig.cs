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
    }
}
