using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(SubtitleConfig), menuName = "SibJam/Subtitles/Subtitle Config")]
    public class SubtitleConfig : ScriptableObject
    {
        [field: Tooltip("Имя говорящего. Пусто — без имени")]
        [field: SerializeField] public string Speaker { get; private set; }

        [field: SerializeField, TextArea(2, 6)] public string Text { get; private set; }

        [field: Tooltip("Анимация появления текста. Оставьте пустым — текст появится мгновенно.")]
        [field: SerializeField] public SubtitleAnimation Animation { get; private set; }
    }
}