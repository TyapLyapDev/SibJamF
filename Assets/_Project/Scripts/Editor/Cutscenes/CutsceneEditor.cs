using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Инспектор катсцены: сводка по клипу и кнопка окна катсцен (там таймлайн), ниже — поля компонента
    [CustomEditor(typeof(Cutscene), true)]
    public class CutsceneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var cutscene = (Cutscene)target;
            CutsceneClip clip = cutscene.Clip;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label(clip != null
                        ? $"{clip.name}  ·  {cutscene.Length:0.0} с  ·  элементов: {clip.Actions.Count}"
                        : "Клип не назначен",
                    EditorStyles.boldLabel);

                if (GUILayout.Button("Открыть в окне катсцен", GUILayout.Height(26)))
                    CutsceneWindow.Open(cutscene);
            }

            EditorGUILayout.Space(4f);
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script");
            serializedObject.ApplyModifiedProperties();
        }
    }

    [CustomEditor(typeof(CutsceneClip))]
    public class CutsceneClipEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var clip = (CutsceneClip)target;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label($"{clip.Length:0.0} с  ·  элементов: {clip.Actions.Count}", EditorStyles.boldLabel);

                if (GUILayout.Button("Открыть в окне катсцен", GUILayout.Height(26)))
                    CutsceneWindow.Open(clip);
            }

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_length"), new GUIContent("Длина, с", "0 — до конца последнего элемента"));
            serializedObject.ApplyModifiedProperties();
        }
    }
}
