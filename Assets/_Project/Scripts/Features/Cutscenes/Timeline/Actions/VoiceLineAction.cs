using System;
using DG.Tweening;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Реплика: озвучка и субтитр через VoiceLinePlayer. На таймлайне длиной в субтитр, начало и конец можно обрезать
    [Serializable, CutsceneAction("Реплика", "#FF4D4D")]
    public class VoiceLineAction : CutsceneAction
    {
        [CutsceneLabel("Реплика")]
        [SerializeField] private VoiceLine _line;
        [CutsceneLabel("Откуда звучит", "Пусто — голос героя")]
        [SerializeField] private ExposedReference<AudioSource> _source;
        [CutsceneLabel("Длина, с", "0 — вся реплика")]
        [SerializeField, Min(0f)] private float _duration;
        [CutsceneLabel("Начать с, с", "Сколько секунд реплики пропустить в начале")]
        [SerializeField, Min(0f)] private float _offset;

        private Tween _cut;

        public VoiceLine Line => _line;

        public override float MaxDuration => _line != null ? Mathf.Max(0f, _line.Duration - _offset) : 0f;
        public override float Duration => _duration > 0f ? Mathf.Min(_duration, MaxDuration) : MaxDuration;
        public override AudioClip PreviewClip => _line != null ? _line.Clip : null;

        public override string Summary
        {
            get
            {
                if (_line == null)
                    return string.Empty;

                if (string.IsNullOrEmpty(_line.Text))
                    return _line.name;

                return string.IsNullOrEmpty(_line.Speaker) ? _line.Text : $"{_line.Speaker}: {_line.Text}";
            }
        }

        public override void Play(CutsceneContext context)
        {
            if (_line == null)
                return;

            context.VoiceLines.Play(_line, context.Resolve(_source), _offset);

            // Конец обрезан — обрываем голос и субтитр раньше
            if (Duration < MaxDuration)
                _cut = DOVirtual.DelayedCall(Duration, context.VoiceLines.Stop);
        }

        public override void Stop(CutsceneContext context)
        {
            _cut?.Kill();
            context.VoiceLines.Stop();
        }
    }
}
