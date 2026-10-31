using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(ShakeSettings), menuName = "SibJam/UI/Shake Settings")]
    public class ShakeSettings : ScriptableObject
    {
        [field: SerializeField, Min(0.01f)] public float DurationPerLoop { get; private set; } = 0.1f;
        [field: SerializeField, Min(0f)] public float Strength { get; private set; } = 3f;
        [field: SerializeField, Min(1)] public int Vibrato { get; private set; } = 30;
        [field: SerializeField, Range(0f, 180f)] public float Randomness { get; private set; } = 180f;
    }
}