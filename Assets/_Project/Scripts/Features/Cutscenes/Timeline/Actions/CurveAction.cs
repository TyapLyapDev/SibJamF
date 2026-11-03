using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Float-свойство компонента сцены по кривой: затемнение (CanvasGroup.alpha), яркость света (Light.intensity),
    // громкость (AudioSource.volume) и т.п. Длина — по последнему ключу кривой.
    // Пропуск и отмена катсцены оставляют значение конца кривой
    [Serializable, CutsceneAction("Кривая", "#8C8C8C")]
    public class CurveAction : CutsceneAction
    {
        [CutsceneLabel("Свойство", "Объект сцены и float-свойство его компонента")]
        [SerializeField] private CutsceneProperty _property = new CutsceneProperty();
        [CutsceneLabel("Значение"), CutsceneMainField]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

        public override float Duration => _curve != null && _curve.length > 0 ? _curve[_curve.length - 1].time : 0f;

        public override string Summary => _property.Summary;

        public CutsceneProperty Property => _property;

        public float ValueAt(float localTime) => _curve != null && _curve.length > 0 ? _curve.Evaluate(localTime) : 0f;

        public override void Play(CutsceneContext context) => Evaluate(context, 0f);

        public override void Evaluate(CutsceneContext context, float localTime) => _property.Set(context.Bindings, ValueAt(localTime));

        public override void PlaySkipped(CutsceneContext context) => Evaluate(context, Duration);

        public override void Cancel(CutsceneContext context) => Evaluate(context, Duration);
    }
}
