using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Элемент таймлайна: звук, реплика, действие и т.п. Сам знает, когда начинается; длину задаёт тип.
    // Новый тип — новый класс с атрибутом CutsceneAction
    [Serializable]
    public abstract class CutsceneAction
    {
        // Правится перетаскиванием на таймлайне и полем в карточке, поэтому среди полей элемента его нет
        [SerializeField, HideInInspector, Min(0f)] private float _time;

        // Когда элемент запускается, в секундах от начала катсцены
        public float Time => _time;

        public float End => _time + Duration;

        // Сколько элемент длится на таймлайне. 0 — мгновенный
        public virtual float Duration => 0f;

        // Длиннее этого растянуть нельзя (длина клипа). У элементов без клипа — без предела
        public virtual float MaxDuration => float.PositiveInfinity;

        // Короткая подпись для таймлайна в редакторе
        public virtual string Summary => string.Empty;

        // Ничего не задано (нет клипа, текста, вызова) — на таймлайне помечается значком
        public virtual bool IsEmpty => string.IsNullOrEmpty(Summary);

        // Что прослушать в редакторе
        public virtual AudioClip PreviewClip => null;

        public abstract void Play(CutsceneContext context);

        // Элемент пропустили (перемотка или пропуск катсцены): выполнить то, без чего игра не продолжится
        public virtual void PlaySkipped(CutsceneContext context) { }

        // Катсцена закончилась или пропущена: заглушить то, что ещё звучит
        public virtual void Stop(CutsceneContext context) { }
    }
}
