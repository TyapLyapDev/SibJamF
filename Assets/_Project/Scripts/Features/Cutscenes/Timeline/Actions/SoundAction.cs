using System;
using DG.Tweening;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Звук без субтитра: шумы, музыка. На таймлайне длиной в клип, начало и конец можно обрезать
    [Serializable, CutsceneAction("Звук", "#59B3FF")]
    public class SoundAction : CutsceneAction
    {
        [CutsceneLabel("Клип")]
        [SerializeField] private AudioClip _clip;
        [CutsceneLabel("Откуда звучит", "Пусто — 2D-источник катсцены")]
        [SerializeField] private ExposedReference<AudioSource> _source;
        [CutsceneLabel("Длина, с", "0 — до конца клипа")]
        [SerializeField, Min(0f)] private float _duration;
        [CutsceneLabel("Начать с, с", "Сколько секунд клипа пропустить в начале")]
        [SerializeField, Min(0f)] private float _offset;

        private Tween _cut;

        public override float MaxDuration => _clip != null ? Mathf.Max(0f, _clip.length - _offset) : 0f;
        public override float Duration => _duration > 0f ? Mathf.Min(_duration, MaxDuration) : MaxDuration;
        public override string Summary => _clip != null ? _clip.name : string.Empty;
        public override AudioClip PreviewClip => _clip;

        public override void Play(CutsceneContext context)
        {
            if (_clip == null || MaxDuration <= 0f)
                return;

            AudioSource source = GetSource(context);

            // Ни своего источника, ни источника катсцены: звук пропускаем, катсцена идёт дальше
            if (source == null)
            {
                Debug.LogWarning($"Катсцена: звуку «{_clip.name}» не указан источник, и у катсцены его нет", context.Bindings as UnityEngine.Object);
                return;
            }

            // С середины клипа PlayOneShot не умеет — ставим клип на источник
            if (_offset > 0f)
            {
                source.clip = _clip;
                source.time = _offset;
                source.Play();
            }
            else
            {
                source.PlayOneShot(_clip);
            }

            if (Duration < MaxDuration)
                _cut = DOVirtual.DelayedCall(Duration, source.Stop);
        }

        public override void Stop(CutsceneContext context)
        {
            _cut?.Kill();
            AudioSource source = GetSource(context);

            if (source != null)
                source.Stop();
        }

        private AudioSource GetSource(CutsceneContext context)
        {
            AudioSource source = context.Resolve(_source);
            return source != null ? source : context.BaseSource;
        }
    }
}
