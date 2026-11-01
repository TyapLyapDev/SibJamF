using DG.Tweening;
using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(HoverColorSettings), menuName = "SibJam/UI/Hover Color Settings")]
    public class HoverColorSettings : ScriptableObject
    {
        [field: SerializeField] public Color HoverColor { get; private set; } = Color.yellow;
        [field: SerializeField, Min(0f)] public float Duration { get; private set; } = 0.15f;
        [field: SerializeField] public Ease Ease { get; private set; } = Ease.OutQuad;
    }
}