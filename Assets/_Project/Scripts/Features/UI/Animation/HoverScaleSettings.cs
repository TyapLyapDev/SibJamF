using DG.Tweening;
using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(HoverScaleSettings), menuName = "SibJam/UI/Hover Scale Settings")]
    public class HoverScaleSettings : ScriptableObject
    {
        [field: SerializeField, Min(1f)] public float HoverMultiplier { get; private set; } = 1.1f;
        [field: SerializeField, Min(0f)] public float Duration { get; private set; } = 0.1f;
        [field: SerializeField] public Ease Ease { get; private set; } = Ease.OutQuad;
    }
}