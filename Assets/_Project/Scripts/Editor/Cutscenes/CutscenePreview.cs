using System;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Превью кадра катсцены в окне редактора на выбранной секунде. Своё для каждого типа катсцены:
    // наследник с атрибутом [CutscenePreviewFor(typeof(МояКатсцена))]. Без него — ClipCutscenePreview по элементам клипа
    public abstract class CutscenePreview : IDisposable
    {
        protected Cutscene Target { get; private set; }

        // Нарисовать кадр шириной width и вернуть, где он: поверх окно нарисует субтитр
        public abstract Rect Draw(float width, float time);

        // Время сменилось — перерисовать кадр
        public virtual void OnTimeChanged(float time) { }

        // Превью остановлено или окно закрыто — вернуть сцену как была
        public virtual void Stop() { }

        public virtual void Dispose() => Stop();

        public static CutscenePreview Create(Cutscene target)
        {
            if (target == null)
                return null;

            Type previewType = TypeCache.GetTypesWithAttribute<CutscenePreviewForAttribute>()
                .Where(type => typeof(CutscenePreview).IsAssignableFrom(type) && !type.IsAbstract)
                .FirstOrDefault(type => type.GetCustomAttribute<CutscenePreviewForAttribute>().Type == target.GetType());

            var preview = previewType != null
                ? (CutscenePreview)Activator.CreateInstance(previewType)
                : new ClipCutscenePreview();
            preview.Target = target;
            preview.OnCreated();
            return preview;
        }

        protected virtual void OnCreated() { }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CutscenePreviewForAttribute : Attribute
    {
        public CutscenePreviewForAttribute(Type type) => Type = type;

        public Type Type { get; }
    }
}
