using System;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SibJam.Cutscenes
{
    // Вызов метода у объекта сцены, как функция у UnityEvent: метод самого GameObject (SetActive) или любого его компонента,
    // свой или юнитевский, без параметров или с одним — bool, int, float, string или ассет. Свойство (enabled) — через сеттер.
    // Объект — ExposedReference: в файле клипа только ключ, сам объект привязан на компоненте катсцены
    [Serializable]
    public class CutsceneCall
    {
        [SerializeField] private ExposedReference<GameObject> _target;
        [Tooltip("Имя объекта для подписи на таймлайне")]
        [SerializeField] private string _targetName;
        [SerializeField] private string _component;
        [SerializeField] private string _method;
        [Tooltip("Тип единственного параметра. Пусто — метод без параметров")]
        [SerializeField] private string _parameterType;
        [SerializeField] private bool _boolArgument;
        [SerializeField] private int _intArgument;
        [SerializeField] private float _floatArgument;
        [SerializeField] private string _stringArgument;
        [Tooltip("Ассет: клип файлом и не может ссылаться на объект сцены")]
        [SerializeField] private Object _objectArgument;

        public bool IsEmpty => string.IsNullOrEmpty(_method);

        public string Summary => IsEmpty
            ? string.Empty
            : $"{(string.IsNullOrEmpty(_targetName) ? "?" : _targetName)}.{DisplayName(_method)}{ArgumentSummary}";

        private string ArgumentSummary
        {
            get
            {
                Type type = ParameterType;

                if (type == null)
                    return string.Empty;

                object argument = Argument(type);

                if (argument is Object asset)
                    return asset != null ? $"({asset.name})" : "(—)";

                return argument is string text ? $"(\"{text}\")" : $"({argument})";
            }
        }

        private Type ParameterType => string.IsNullOrEmpty(_parameterType) ? null : Type.GetType(_parameterType);

        // Сеттер свойства показываем именем свойства: set_enabled → enabled
        public static string DisplayName(string method) =>
            method != null && method.StartsWith("set_") ? method.Substring(4) : method;

        public void Invoke(CutsceneContext context)
        {
            if (IsEmpty)
                return;

            GameObject target = context.Resolve(_target);
            Type type = string.IsNullOrEmpty(_component) ? null : Type.GetType(_component);
            object receiver = null;

            if (target != null && type != null)
                receiver = type == typeof(GameObject) ? target : target.GetComponent(type);

            Type parameterType = ParameterType;
            Type[] signature = parameterType != null ? new[] { parameterType } : Type.EmptyTypes;
            MethodInfo method = receiver != null && (parameterType != null || string.IsNullOrEmpty(_parameterType))
                ? receiver.GetType().GetMethod(_method, BindingFlags.Instance | BindingFlags.Public, null, signature, null)
                : null;

            if (method == null)
            {
                Debug.LogWarning($"Катсцена: не удалось вызвать {Summary} — нет объекта, компонента или метода");
                return;
            }

            method.Invoke(receiver, parameterType != null ? new[] { Argument(parameterType) } : null);
        }

        private object Argument(Type type)
        {
            if (type == typeof(bool))
                return _boolArgument;

            if (type == typeof(int))
                return _intArgument;

            if (type == typeof(float))
                return _floatArgument;

            if (type == typeof(string))
                return _stringArgument;

            return _objectArgument;
        }
    }
}
