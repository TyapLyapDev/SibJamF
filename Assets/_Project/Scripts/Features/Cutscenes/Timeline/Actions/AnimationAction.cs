using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace SibJam.Cutscenes
{
    // Анимация объекта сцены: кадр клипа берётся из времени катсцены, поэтому перемотка и пропуск ставят верную позу.
    // На таймлайне длиной в клип, начало и конец можно обрезать
    [Serializable, CutsceneAction("Анимация", "#B98CFF")]
    public class AnimationAction : CutsceneAction
    {
        [CutsceneLabel("Клип"), CutsceneMainField]
        [SerializeField] private AnimationClip _clip;
        [CutsceneLabel("Риг", "Animator, который играет клип. Включается на старте элемента")]
        [SerializeField] private ExposedReference<Animator> _rig;
        [CutsceneLabel("Спрятать в конце", "Выключить риг, когда катсцена закончится или не запустится")]
        [SerializeField] private bool _hideOnFinish = true;
        [CutsceneLabel("Длина, с", "0 — до конца клипа")]
        [SerializeField, Min(0f)] private float _duration;
        [CutsceneLabel("Начать с, с", "Сколько секунд клипа пропустить в начале")]
        [SerializeField, Min(0f)] private float _offset;

        private PlayableGraph _graph;
        private AnimationClipPlayable _playable;

        public AnimationClip Clip => _clip;

        public override float MaxDuration => _clip != null ? Mathf.Max(0f, _clip.length - _offset) : 0f;
        public override float Duration => _duration > 0f ? Mathf.Min(_duration, MaxDuration) : MaxDuration;
        public override string Summary => _clip != null ? _clip.name : string.Empty;

        public Animator ResolveRig(IExposedPropertyTable bindings) => _rig.Resolve(bindings);

        // Время клипа на секунде элемента
        public float ClipTime(float localTime) => _offset + Mathf.Clamp(localTime, 0f, Duration);

        public override void Play(CutsceneContext context)
        {
            Animator rig = context.Resolve(_rig);

            if (_clip == null || rig == null)
            {
                Debug.LogWarning($"Катсцена: анимации «{Summary}» не указан клип или риг", context.Bindings as UnityEngine.Object);
                return;
            }

            rig.gameObject.SetActive(true);
            rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // Скорость 0: время клипа ставим сами каждый кадр, Animator только применяет позу
            _graph = PlayableGraph.Create($"Cutscene {_clip.name}");
            _playable = AnimationClipPlayable.Create(_graph, _clip);
            _playable.SetSpeed(0d);
            AnimationPlayableOutput.Create(_graph, "Animation", rig).SetSourcePlayable(_playable);
            _graph.Play();
        }

        public override void Evaluate(CutsceneContext context, float localTime)
        {
            if (!_graph.IsValid())
                return;

            _playable.SetTime(ClipTime(localTime));
            // Позу ставим сразу, а не в проходе анимации: при пропуске её читают в этом же кадре
            _graph.Evaluate();
        }

        // Пропущена целиком: тело всё равно должно встать в позу последнего кадра
        public override void PlaySkipped(CutsceneContext context)
        {
            Play(context);
            Evaluate(context, Duration);
        }

        public override void Stop(CutsceneContext context)
        {
            if (_graph.IsValid())
                _graph.Destroy();

            HideRig(context);
        }

        public override void Cancel(CutsceneContext context) => HideRig(context);

        private void HideRig(CutsceneContext context)
        {
            Animator rig = context.Resolve(_rig);

            if (_hideOnFinish && rig != null)
                rig.gameObject.SetActive(false);
        }
    }
}
