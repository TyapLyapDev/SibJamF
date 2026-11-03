using System;
using System.Reflection;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Вызов метода без параметров у компонента объекта сцены, как функция у Animation Event.
    // Объект — ExposedReference: в файле клипа только ключ, сам объект привязан на компоненте катсцены
    [Serializable]
    public class CutsceneCall
    {
        [SerializeField] private ExposedReference<GameObject> _target;
        [Tooltip("Имя объекта для подписи на таймлайне")]
        [SerializeField] private string _targetName;
        [SerializeField] private string _component;
        [SerializeField] private string _method;

        public bool IsEmpty => string.IsNullOrEmpty(_method);

        public string Summary => IsEmpty
            ? string.Empty
            : $"{(string.IsNullOrEmpty(_targetName) ? "?" : _targetName)}.{_method}";

        public void Invoke(CutsceneContext context)
        {
            if (IsEmpty)
                return;

            GameObject target = context.Resolve(_target);
            Type type = string.IsNullOrEmpty(_component) ? null : Type.GetType(_component);
            Component component = target != null && type != null ? target.GetComponent(type) : null;
            MethodInfo method = component != null
                ? component.GetType().GetMethod(_method, BindingFlags.Instance | BindingFlags.Public, null, Type.EmptyTypes, null)
                : null;

            if (method == null)
            {
                Debug.LogWarning($"Катсцена: не удалось вызвать {Summary} — нет объекта, компонента или метода");
                return;
            }

            method.Invoke(component, null);
        }
    }
}
