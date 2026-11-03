using System;
using System.Reflection;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Float-свойство компонента объекта сцены, как свойство в окне Animation: объект и выбор «Компонент.свойство» из списка.
    // Объект — ExposedReference: в файле клипа только ключ, сам объект привязан на компоненте катсцены
    [Serializable]
    public class CutsceneProperty
    {
        [SerializeField] private ExposedReference<GameObject> _target;
        [Tooltip("Имя объекта для подписи на таймлайне")]
        [SerializeField] private string _targetName;
        [SerializeField] private string _component;
        [SerializeField] private string _property;

        public bool IsEmpty => string.IsNullOrEmpty(_property);

        public string Property => _property;

        public string Summary => IsEmpty
            ? string.Empty
            : $"{(string.IsNullOrEmpty(_targetName) ? "?" : _targetName)}.{_property}";

        public Component ResolveComponent(IExposedPropertyTable bindings)
        {
            GameObject target = _target.Resolve(bindings);
            Type type = string.IsNullOrEmpty(_component) ? null : Type.GetType(_component);
            return target != null && type != null ? target.GetComponent(type) : null;
        }

        public void Set(IExposedPropertyTable bindings, float value)
        {
            if (IsEmpty)
                return;

            Component component = ResolveComponent(bindings);
            const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;
            PropertyInfo property = component != null ? component.GetType().GetProperty(_property, Flags) : null;

            if (property != null && property.PropertyType == typeof(float) && property.CanWrite)
            {
                property.SetValue(component, value);
                return;
            }

            FieldInfo field = component != null ? component.GetType().GetField(_property, Flags) : null;

            if (field != null && field.FieldType == typeof(float))
            {
                field.SetValue(component, value);
                return;
            }

            Debug.LogWarning($"Катсцена: не удалось задать {Summary} — нет объекта, компонента или свойства");
        }
    }
}
