using System;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SibJam.EditorTools
{
    // Вызов в одну строку, как функция у Animation Event: объект сцены и список методов его компонентов
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

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect targetRect = new Rect(position.x, position.y, position.width * 0.5f - 2f, position.height);
            Rect methodRect = new Rect(targetRect.xMax + 4f, position.y, position.xMax - targetRect.xMax - 4f, position.height);

            // Штатное поле ExposedReference: объект запоминает катсцена-контекст
            EditorGUI.PropertyField(targetRect, target, GUIContent.none);
            GameObject gameObject = Resolve(property, target);

            if (gameObject != null && targetName.stringValue != gameObject.name)
                targetName.stringValue = gameObject.name;

            string current = string.IsNullOrEmpty(method.stringValue)
                ? "Метод…"
                : $"{ShortName(component.stringValue)}.{method.stringValue}";

            using (new EditorGUI.DisabledScope(gameObject == null))
            {
                if (EditorGUI.DropdownButton(methodRect, new GUIContent(current, "Метод компонента без параметров"), FocusType.Keyboard))
                    ShowMethods(property, gameObject, component.stringValue, method.stringValue);
            }

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

        // Меню «Компонент/Метод», как выбор функции у UnityEvent
        private static void ShowMethods(SerializedProperty property, GameObject gameObject, string currentComponent, string currentMethod)
        {
            var menu = new GenericMenu();
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;

            menu.AddItem(new GUIContent("Нет"), string.IsNullOrEmpty(currentMethod), () => Set(serializedObject, path, null, null));
            menu.AddSeparator(string.Empty);

            foreach (Component behaviour in gameObject.GetComponents<Component>().Where(item => item != null))
            {
                Type type = behaviour.GetType();
                string typeName = $"{type.FullName}, {type.Assembly.GetName().Name}";

                foreach (MethodInfo info in CallableMethods(type))
                {
                    string methodName = info.Name;
                    bool on = typeName == currentComponent && methodName == currentMethod;
                    menu.AddItem(new GUIContent($"{type.Name}/{methodName}"), on, () => Set(serializedObject, path, typeName, methodName));
                }
            }

            menu.ShowAsContext();
        }

        // Свои публичные методы без параметров, без служебных методов Unity
        private static MethodInfo[] CallableMethods(Type type) => type
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(info => !info.IsSpecialName && !info.IsGenericMethod && info.GetParameters().Length == 0)
            .Where(info => info.DeclaringType != null && info.DeclaringType.Namespace?.StartsWith("UnityEngine") != true
                && info.DeclaringType != typeof(object))
            .OrderBy(info => info.Name)
            .ToArray();

        private static void Set(SerializedObject serializedObject, string path, string component, string method)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(path);
            property.FindPropertyRelative("_component").stringValue = component;
            property.FindPropertyRelative("_method").stringValue = method;
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
