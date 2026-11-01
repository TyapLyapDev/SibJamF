using System;
using UnityEngine;
using UnityEngine.Events;

namespace SibJam.Cutscenes
{
    // Одно событие катсцены на её таймлайне: реплика и/или звук и/или любой вызов из инспектора
    [Serializable]
    public class CutsceneCue
    {
        [SerializeField, Min(0f)] private float _time;
        [Tooltip("Реплика с озвучкой и субтитром")]
        [SerializeField] private VoiceLine _line;
        [Tooltip("Звук без субтитра: шумы, музыка")]
        [SerializeField] private AudioClip _clip;
        [Tooltip("Откуда звучит. Пусто — голос героя (2D-источник)")]
        [SerializeField] private AudioSource _source;
        [Tooltip("Вызывается и при пропуске катсцены — сюда то, без чего игра не продолжится")]
        [SerializeField] private UnityEvent _event;

        public float Time => _time;
        public VoiceLine Line => _line;
        public AudioClip Clip => _clip;
        public AudioSource Source => _source;
        public UnityEvent Event => _event;
    }
}
