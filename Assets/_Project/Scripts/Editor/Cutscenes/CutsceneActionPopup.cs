using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Все поля элемента катсцены во всплывающем окне: в строке дорожки помещается только главное.
    // Подписи — из [CutsceneLabel]. Если выделено несколько элементов того же типа, правка поля идёт всем сразу
    public class CutsceneActionPopup : PopupWindowContent
    {
        private const float Width = 340f;

        private readonly SerializedObject _clip;
        private readonly string _path;
        private readonly string[] _others;
        private readonly Type _type;

        private CutsceneActionPopup(SerializedObject clip, string path, string[] others, Type type)
        {
            _clip = clip;
            _path = path;
            _others = others;
            _type = type;
        }

        // selection — индексы выделенных элементов в actions: из них берутся элементы того же типа, что и этот
        public static void Show(Rect at, SerializedProperty actions, int index, IEnumerable<int> selection)
        {
            SerializedProperty action = actions.GetArrayElementAtIndex(index);
            Type type = action.managedReferenceValue?.GetType();

            if (type == null)
                return;

            string[] others = (selection ?? Enumerable.Empty<int>())
                .Where(i => i != index && i < actions.arraySize)
                .Select(actions.GetArrayElementAtIndex)
                .Where(other => other.managedReferenceValue?.GetType() == type)
                .Select(other => other.propertyPath)
                .ToArray();

            PopupWindow.Show(at, new CutsceneActionPopup(actions.serializedObject, action.propertyPath, others, type));
        }

        public override Vector2 GetWindowSize()
        {
            SerializedProperty action = _clip.targetObject != null ? _clip.FindProperty(_path) : null;
            float height = EditorGUIUtility.singleLineHeight + 12f;

            foreach (SerializedProperty field in Fields(action))
                height += EditorGUI.GetPropertyHeight(field, true) + EditorGUIUtility.standardVerticalSpacing;

            return new Vector2(Width, height + 8f);
        }

        public override void OnGUI(Rect rect)
        {
            if (_clip.targetObject == null)
            {
                editorWindow.Close();
                return;
            }

            _clip.Update();
            SerializedProperty action = _clip.FindProperty(_path);

            if (action == null || action.managedReferenceValue?.GetType() != _type)
            {
                editorWindow.Close();
                return;
            }

            string title = CutsceneTimelineDrawer.TrackName(_type);
            GUILayout.Label(_others.Length > 0 ? $"{title} — элементов: {_others.Length + 1}" : title, EditorStyles.boldLabel);
            List<SerializedProperty> others = _others.Select(_clip.FindProperty).Where(other => other != null).ToList();
            float labelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 110f;

            foreach (SerializedProperty field in Fields(action))
            {
                string relative = field.propertyPath.Substring(action.propertyPath.Length + 1);
                List<SerializedProperty> same = others.Select(other => other.FindPropertyRelative(relative)).Where(other => other != null).ToList();

                // Разные значения у выделенных — прочерк, как в обычном инспекторе
                EditorGUI.showMixedValue = same.Any(other => !SerializedProperty.DataEquals(field, other));
                EditorGUI.BeginChangeCheck();
                // Секунды — до сотых, как в строке элемента
                if (field.propertyType == SerializedPropertyType.Float)
                {
                    float value = EditorGUILayout.DelayedFloatField(Label(field), (float)Math.Round(field.floatValue, 2));

                    if (GUI.changed)
                        field.floatValue = (float)Math.Round(value, 2);
                }
                else
                    EditorGUILayout.PropertyField(field, Label(field), true);

                EditorGUI.showMixedValue = false;

                if (EditorGUI.EndChangeCheck())
                {
                    foreach (SerializedProperty other in same)
                        other.boxedValue = field.boxedValue;
                }
            }

            EditorGUIUtility.labelWidth = labelWidth;

            if (_clip.ApplyModifiedProperties())
                CutsceneWindow.Current?.Repaint();
        }

        private System.Collections.Generic.IEnumerable<SerializedProperty> Fields(SerializedProperty action)
        {
            if (action == null)
                yield break;

            SerializedProperty field = action.Copy();
            SerializedProperty end = action.GetEndProperty();

            if (!field.NextVisible(true))
                yield break;

            while (!SerializedProperty.EqualContents(field, end))
            {
                yield return field.Copy();

                if (!field.NextVisible(false))
                    break;
            }
        }

        private GUIContent Label(SerializedProperty field)
        {
            FieldInfo info = null;

            for (Type type = _type; type != null && info == null; type = type.BaseType)
                info = type.GetField(field.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

            var label = info?.GetCustomAttribute<CutsceneLabelAttribute>();
            return label != null ? new GUIContent(label.Text, label.Tooltip) : new GUIContent(field.displayName, field.tooltip);
        }
    }
}
