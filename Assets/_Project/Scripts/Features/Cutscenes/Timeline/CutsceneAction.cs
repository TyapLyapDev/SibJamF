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

        // Каждый кадр, пока элемент идёт, и последний раз в его конце: localTime от 0 до Duration. Из Update
        public virtual void Evaluate(CutsceneContext context, float localTime) { }

        // То же из LateUpdate, когда анимация на кадре уже применена: для камеры, которая едет за костью
        public virtual void LateEvaluate(CutsceneContext context, float localTime) { }

        // Автозапуск катсцены отменён (старт с чекпоинта): убрать с экрана то, что сохранено в сцене для её начала.
        // Вызовы и звуки тут не нужны — игра продолжается не с конца катсцены
        public virtual void Cancel(CutsceneContext context) { }

        // Элемент пропустили (перемотка или пропуск катсцены): выполнить то, без чего игра не продолжится
        public virtual void PlaySkipped(CutsceneContext context) { }

        // Катсцена закончилась или пропущена: заглушить то, что ещё звучит
        public virtual void Stop(CutsceneContext context) { }
    }
}
