using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Субтитр без озвучки: текст прямо на таймлайне, без ассета реплики
    [Serializable, CutsceneAction("Субтитры", "#F2F2F2")]
    public class SubtitleAction : CutsceneAction
    {
        private const float DefaultDuration = 3f;

        [CutsceneLabel("Кто говорит", "Пусто — без имени")]
        [SerializeField] private string _speaker;
        [CutsceneLabel("Текст"), CutsceneMainField]
        [SerializeField, TextArea(1, 4)] private string _text;
        [CutsceneLabel("Держать, с")]
        [SerializeField, Min(0.1f)] private float _duration = DefaultDuration;
        [CutsceneLabel("Анимация", "Пусто — текст появится сразу")]
        [SerializeField] private SubtitleAnimation _animation;

        public string Speaker => _speaker;
        public string Text => _text;

        public override float Duration => _duration;

        public override string Summary
        {
            get
            {
                if (string.IsNullOrEmpty(_text))
                    return string.Empty;

                return string.IsNullOrEmpty(_speaker) ? _text : $"{_speaker}: {_text}";
            }
        }

        public override void Play(CutsceneContext context) =>
            context.Subtitles.Show(_speaker, _text, _duration, _animation);

        public override void Stop(CutsceneContext context) => context.Subtitles.Hide();
    }
}
