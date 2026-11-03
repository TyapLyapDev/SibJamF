using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Вызов метода у объекта сцены, как Animation Event. Выполняется и при пропуске катсцены —
    // сюда то, без чего игра не продолжится
    [Serializable, CutsceneAction("Действие", "#73E673")]
    public class EventAction : CutsceneAction
    {
        [CutsceneLabel("Вызов", "Объект сцены и метод его компонента"), CutsceneMainField]
        [SerializeField] private CutsceneCall _call = new CutsceneCall();

        public override string Summary => _call.Summary;

        public override void Play(CutsceneContext context) => _call.Invoke(context);

        public override void PlaySkipped(CutsceneContext context) => _call.Invoke(context);
    }
}
