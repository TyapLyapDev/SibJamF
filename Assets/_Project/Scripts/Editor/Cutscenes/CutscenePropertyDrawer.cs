using System;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SibJam.EditorTools
{
    // Свойство в одну строку, как в окне Animation: объект сцены и список float-свойств его компонентов
    [CustomPropertyDrawer(typeof(CutsceneProperty))]
    public class CutscenePropertyDrawer : PropertyDrawer
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (label != GUIContent.none && !string.IsNullOrEmpty(label.text))
                position = EditorGUI.PrefixLabel(position, label);

            SerializedProperty target = property.FindPropertyRelative("_target");
            SerializedProperty targetName = property.FindPropertyRelative("_targetName");
            SerializedProperty component = property.FindPropertyRelative("_component");
            SerializedProperty member = property.FindPropertyRelative("_property");

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            Rect targetRect = new Rect(position.x, position.y, position.width * 0.5f - 2f, position.height);
            Rect memberRect = new Rect(targetRect.xMax + 4f, position.y, position.xMax - targetRect.xMax - 4f, position.height);

            // Штатное поле ExposedReference: объект запоминает катсцена-контекст
            EditorGUI.PropertyField(targetRect, target, GUIContent.none);
            GameObject gameObject = Resolve(property, target);

            if (gameObject != null && targetName.stringValue != gameObject.name)
                targetName.stringValue = gameObject.name;

            string current = string.IsNullOrEmpty(member.stringValue)
                ? "Свойство…"
                : $"{ShortName(component.stringValue)}.{member.stringValue}";

            using (new EditorGUI.DisabledScope(gameObject == null))
            {
                if (EditorGUI.DropdownButton(memberRect, new GUIContent(current, "Float-свойство компонента"), FocusType.Keyboard))
                    ShowProperties(property, gameObject, component.stringValue, member.stringValue);
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

        // Меню «Компонент/свойство», как выбор свойства в окне Animation
        private static void ShowProperties(SerializedProperty property, GameObject gameObject, string currentComponent, string currentMember)
        {
            var menu = new GenericMenu();
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;

            menu.AddItem(new GUIContent("Нет"), string.IsNullOrEmpty(currentMember), () => Set(serializedObject, path, null, null));
            menu.AddSeparator(string.Empty);

            foreach (Component behaviour in gameObject.GetComponents<Component>().Where(item => item != null))
            {
                Type type = behaviour.GetType();
                string typeName = $"{type.FullName}, {type.Assembly.GetName().Name}";

                foreach (string name in FloatMembers(type))
                {
                    bool on = typeName == currentComponent && name == currentMember;
                    menu.AddItem(new GUIContent($"{type.Name}/{name}"), on, () => Set(serializedObject, path, typeName, name));
                }
            }

            menu.ShowAsContext();
        }

        // Публичные float-свойства с записью и float-поля, без устаревших
        private static string[] FloatMembers(Type type)
        {
            var properties = type.GetProperties(Flags)
                .Where(info => info.PropertyType == typeof(float) && info.CanRead && info.CanWrite
                    && info.GetIndexParameters().Length == 0 && info.GetSetMethod() != null)
                .Where(info => !info.IsDefined(typeof(ObsoleteAttribute), true))
                .Select(info => info.Name);

            var fields = type.GetFields(Flags)
                .Where(info => info.FieldType == typeof(float) && !info.IsInitOnly)
                .Where(info => !info.IsDefined(typeof(ObsoleteAttribute), true))
                .Select(info => info.Name);

            return properties.Concat(fields).Distinct().OrderBy(name => name).ToArray();
        }

        private static void Set(SerializedObject serializedObject, string path, string component, string member)
        {
            serializedObject.Update();
            SerializedProperty property = serializedObject.FindProperty(path);
            property.FindPropertyRelative("_component").stringValue = component;
            property.FindPropertyRelative("_property").stringValue = member;
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
