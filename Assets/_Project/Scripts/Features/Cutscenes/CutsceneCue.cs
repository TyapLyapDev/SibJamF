using System;
using UnityEngine;
using UnityEngine.Events;

namespace SibJam.Cutscenes
{
    // Одно событие катсцены на её таймлайне: звук и/или реплика с субтитром и/или любой вызов из инспектора
    [Serializable]
    public class CutsceneCue
    {
        [SerializeField, Min(0f)] private float _time;
        [SerializeField] private AudioClip _clip;
        [Tooltip("Откуда звучит. Пусто — голос героя (2D-источник катсцены)")]
        [SerializeField] private AudioSource _source;
        [SerializeField] private string _speaker;
        [SerializeField, TextArea(1, 3)] private string _text;
        [Tooltip("Сколько держать субтитр. 0 — длина клипа, а без клипа 3 секунды")]
        [SerializeField, Min(0f)] private float _subtitleDuration;
        [Tooltip("Вызывается и при пропуске катсцены — сюда то, без чего игра не продолжится")]
        [SerializeField] private UnityEvent _event;

        public float Time => _time;
        public AudioClip Clip => _clip;
        public AudioSource Source => _source;
        public string Speaker => _speaker;
        public string Text => _text;
        public UnityEvent Event => _event;

        public float SubtitleDuration
        {
            get
            {
                if (_subtitleDuration > 0f)
                    return _subtitleDuration;

                return _clip != null ? _clip.length : 3f;
            }
        }
    }
}
