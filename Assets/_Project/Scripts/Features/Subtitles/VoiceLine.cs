using UnityEngine;

namespace SibJam
{
    // Одна реплика: озвучка и субтитр к ней. Проигрывается через VoiceLinePlayer
    [CreateAssetMenu(fileName = nameof(VoiceLine), menuName = "SibJam/Subtitles/Voice Line")]
    public class VoiceLine : ScriptableObject
    {
        private const float DefaultDuration = 3f;

        [field: SerializeField] public AudioClip Clip { get; private set; }

        [field: Tooltip("Имя говорящего. Пусто — без имени")]
        [field: SerializeField] public string Speaker { get; private set; }

        [field: Tooltip("Текст субтитра. Пусто — реплика без субтитра")]
        [field: SerializeField, TextArea(2, 6)] public string Text { get; private set; }

        [Tooltip("Сколько держать субтитр. 0 — длина клипа, а без клипа 3 секунды")]
        [SerializeField, Min(0f)] private float _duration;

        [field: Tooltip("Анимация появления текста. Оставьте пустым — текст появится мгновенно.")]
        [field: SerializeField] public SubtitleAnimation Animation { get; private set; }

        public float Duration
        {
            get
            {
                if (_duration > 0f)
                    return _duration;

                return Clip != null ? Clip.length : DefaultDuration;
            }
        }
    }
}
