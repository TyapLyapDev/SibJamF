using System;
using SibJam.TV;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Реплика телевизора: озвучка из его динамика, на экране выбранное лицо, колонки пульсируют.
    // После реплики телик возвращается к лицу простоя. Длина и обрезка — как у обычной реплики
    [Serializable, CutsceneAction("Реплика телика", "#FFB347")]
    public class TVLineAction : CutsceneAction
    {
        [CutsceneLabel("Реплика"), CutsceneMainField]
        [SerializeField] private VoiceLine _line;
        [CutsceneLabel("Телевизор")]
        [SerializeField] private ExposedReference<TVScreen> _tv;
        [CutsceneLabel("Лицо")]
        [SerializeField] private TVFace _face = TVFace.Evil;
        [CutsceneLabel("Длина, с", "0 — вся реплика")]
        [SerializeField, Min(0f)] private float _duration;
        [CutsceneLabel("Начать с, с", "Сколько секунд реплики пропустить в начале")]
        [SerializeField, Min(0f)] private float _offset;

        [NonSerialized] private TVScreen _talking;

        public override float MaxDuration => _line != null ? Mathf.Max(0f, _line.Duration - _offset) : 0f;
        public override float Duration => _duration > 0f ? Mathf.Min(_duration, MaxDuration) : MaxDuration;
        public override AudioClip PreviewClip => _line != null ? _line.Clip : null;

        public override string Summary
        {
            get
            {
                if (_line == null)
                    return string.Empty;

                string text = string.IsNullOrEmpty(_line.Text) ? _line.name : _line.Text;
                return $"[{_face}] {text}";
            }
        }

        public override void Play(CutsceneContext context)
        {
            if (_line == null)
                return;

            _talking = context.Resolve(_tv);

            if (_talking != null)
                _talking.BeginTalk(_face);

            context.VoiceLines.Play(_line, _talking != null ? _talking.Voice : null, _offset);
        }

        public override void Evaluate(CutsceneContext context, float localTime)
        {
            if (_talking == null || localTime < Duration)
                return;

            // Конец обрезан — обрываем голос и субтитр вместе с репликой
            if (Duration < MaxDuration)
                context.VoiceLines.Stop();

            EndTalk();
        }

        public override void Stop(CutsceneContext context)
        {
            if (_talking == null)
                return;

            context.VoiceLines.Stop();
            EndTalk();
        }

        private void EndTalk()
        {
            _talking.EndTalk();
            _talking = null;
        }
    }
}
