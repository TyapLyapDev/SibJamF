using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Инспектор файла клипа: только длина, элементы правятся в окне катсцен
    [CustomEditor(typeof(CutsceneClip))]
    public class CutsceneClipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_length"), new GUIContent("Длина, с", "0 — до конца последнего элемента"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
