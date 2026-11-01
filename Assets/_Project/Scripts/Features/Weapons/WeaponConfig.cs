using UnityEngine;

namespace SibJam.Weapons
{
    // Всё, что отличает одно оружие от другого: модель в руке, урон, дальность, тайминги и свои анимации
    [CreateAssetMenu(menuName = "SibJam/Weapon Config", fileName = "WeaponConfig")]
    public class WeaponConfig : ScriptableObject
    {
        [field: Header("Модель")]
        [field: SerializeField] public GameObject ModelPrefab { get; private set; }
        [field: SerializeField] public Vector3 ModelPosition { get; private set; }
        [field: SerializeField] public Vector3 ModelRotation { get; private set; }
        [field: SerializeField] public Vector3 ModelScale { get; private set; } = Vector3.one;

        [field: Header("Анимации (пусто — анимации рук по умолчанию)")]
        [field: SerializeField] public AnimatorOverrideController AnimatorOverride { get; private set; }

        [field: Header("Атака")]
        [field: SerializeField, Min(0f)] public float Damage { get; private set; } = 50f;
        [field: SerializeField, Min(0f)] public float Range { get; private set; } = 2.5f;
        [field: SerializeField, Min(0f)] public float Radius { get; private set; } = 0.5f;
        [field: SerializeField, Min(0f)] public float Cooldown { get; private set; } = 0.6f;

        [field: Header("Блок")]
        [field: SerializeField, Min(0f)] public float ParryWindow { get; private set; } = 0.2f;
    }
}
