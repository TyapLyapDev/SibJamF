using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SibJam.EditorTools
{
    // Вызов в одну строку, как функция у UnityEvent: объект сцены, метод GameObject или любого его компонента
    // и значение параметра, если он есть
    [CustomPropertyDrawer(typeof(CutsceneCall))]
    public class CutsceneCallDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (label != GUIContent.none && !string.IsNullOrEmpty(label.text))
                position = EditorGUI.PrefixLabel(position, label);

            SerializedProperty target = property.FindPropertyRelative("_target");
            SerializedProperty targetName = property.FindPropertyRelative("_targetName");
            SerializedProperty component = property.FindPropertyRelative("_component");
            SerializedProperty method = property.FindPropertyRelative("_method");
            SerializedProperty parameterType = property.FindPropertyRelative("_parameterType");
            Type parameter = string.IsNullOrEmpty(parameterType.stringValue) ? null : Type.GetType(parameterType.stringValue);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            // С параметром строка делится на три: объект, метод, значение
            float argumentWidth = parameter != null ? position.width * 0.25f : 0f;
            Rect targetRect = new Rect(position.x, position.y, (position.width - argumentWidth) * 0.5f - 2f, position.height);
            Rect methodRect = new Rect(targetRect.xMax + 4f, position.y, position.xMax - argumentWidth - targetRect.xMax - 4f, position.height);
            Rect argumentRect = new Rect(methodRect.xMax + 4f, position.y, argumentWidth - 4f, position.height);

            // Штатное поле ExposedReference: объект запоминает катсцена-контекст
            EditorGUI.PropertyField(targetRect, target, GUIContent.none);
            GameObject gameObject = Resolve(property, target);

            if (gameObject != null && targetName.stringValue != gameObject.name)
                targetName.stringValue = gameObject.name;

            string current = string.IsNullOrEmpty(method.stringValue)
                ? "Метод…"
                : $"{ShortName(component.stringValue)}.{CutsceneCall.DisplayName(method.stringValue)}";

            using (new EditorGUI.DisabledScope(gameObject == null))
            {
                if (EditorGUI.DropdownButton(methodRect, new GUIContent(current, "Метод GameObject или компонента"), FocusType.Keyboard))
                    ShowMethods(property, gameObject, component.stringValue, method.stringValue, parameterType.stringValue);
            }

            if (parameter != null)
                DrawArgument(argumentRect, property, parameter);

            EditorGUI.indentLevel = indent;
        }

        private static GameObject Resolve(SerializedProperty property, SerializedProperty target)
        {
            string exposedName = target.FindPropertyRelative("exposedName").stringValue;
            Object fallback = target.FindPropertyRelative("defaultValue").objectReferenceValue;

            if (property.serializedObject.context is IExposedPropertyTable table && !string.IsNullOrEmpty(exposedName))
            {
                Object value = table.GetReferenceValue(new PropertyName(exposedName), out bool valid);

                if (valid)
                    return value as GameObject;
            }

            return fallback as GameObject;
        }

        private static void DrawArgument(Rect rect, SerializedProperty property, Type parameter)
        {
            if (parameter == typeof(bool))
            {
                SerializedProperty value = property.FindPropertyRelative("_boolArgument");
                value.boolValue = EditorGUI.Toggle(rect, value.boolValue);
            }
            else if (parameter == typeof(int))
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("_intArgument"), GUIContent.none);
            else if (parameter == typeof(float))
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("_floatArgument"), GUIContent.none);
            else if (parameter == typeof(string))
                EditorGUI.PropertyField(rect, property.FindPropertyRelative("_stringArgument"), GUIContent.none);
            else
            {
                SerializedProperty value = property.FindPropertyRelative("_objectArgument");
                value.objectReferenceValue = EditorGUI.ObjectField(rect, value.objectReferenceValue, parameter, false);
            }
        }

        // Меню «Компонент/Метод», как выбор функции у UnityEvent: сам GameObject и все его компоненты
        private static void ShowMethods(SerializedProperty property, GameObject gameObject, string currentComponent, string currentMethod,
            string currentParameter)
        {
            var menu = new GenericMenu();
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;

            menu.AddItem(new GUIContent("Нет"), string.IsNullOrEmpty(currentMethod), () => Set(serializedObject, path, null, null, null));
            menu.AddSeparator(string.Empty);

            var receivers = new List<Type> { typeof(GameObject) };
            receivers.AddRange(gameObject.GetComponents<Component>().Where(item => item != null).Select(item => item.GetType()).Distinct());

            foreach (Type type in receivers)
            {
                string typeName = $"{type.FullName}, {type.Assembly.GetName().Name}";

                foreach (MethodInfo info in CallableMethods(type))
                {
                    string methodName = info.Name;
                    Type parameter = info.GetParameters().Select(item => item.ParameterType).FirstOrDefault();
                    string parameterName = parameter != null ? $"{parameter.FullName}, {parameter.Assembly.GetName().Name}" : null;
                    bool on = typeName == currentComponent && methodName == currentMethod
                        && (parameterName ?? string.Empty) == (currentParameter ?? string.Empty);
                    string label = parameter != null
                        ? $"{type.Name}/{CutsceneCall.DisplayName(methodName)} ({parameter.Name})"
                        : $"{type.Name}/{methodName} ()";
                    menu.AddItem(new GUIContent(label), on, () => Set(serializedObject, path, typeName, methodName, parameterName));
                }
            }

            menu.ShowAsContext();
        }

        // Как у UnityEvent: публичные void-методы и сеттеры свойств без параметров или с одним простым параметром.
        // Служебное object/Object/Component/MonoBehaviour не показываем
        private static IEnumerable<MethodInfo> CallableMethods(Type type) => type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(info => info.ReturnType == typeof(void) && !info.IsGenericMethod && !info.IsDefined(typeof(ObsoleteAttribute)))
            .Where(info => !info.IsSpecialName || info.Name.StartsWith("set_"))
            .Where(info => !HiddenDeclaringTypes.Contains(info.DeclaringType))
            .Where(info =>
            {
                ParameterInfo[] parameters = info.GetParameters();
                return parameters.Length == 0 || parameters.Length == 1 && IsSupported(parameters[0].ParameterType);
            })
            .OrderBy(info => info.IsSpecialName ? 0 : 1)
            .ThenBy(info => info.Name);

        private static readonly Type[] HiddenDeclaringTypes = { typeof(object), typeof(Object), typeof(Component), typeof(MonoBehaviour) };

        private static bool IsSupported(Type type) =>
            type == typeof(bool) || type == typeof(int) || type == typeof(float) || type == typeof(string)
            || typeof(Object).IsAssignableFrom(type);

        private static void Set(SerializedObject serializedObject, string path, string component, string method, string parameter)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(path);
            property.FindPropertyRelative("_component").stringValue = component;
            property.FindPropertyRelative("_method").stringValue = method;
            property.FindPropertyRelative("_parameterType").stringValue = parameter;
            serializedObject.ApplyModifiedProperties();
        }

        private static string ShortName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
                return "?";

            string fullName = typeName.Split(',')[0];
            return fullName.Substring(fullName.LastIndexOf('.') + 1);
        }
    }
}
