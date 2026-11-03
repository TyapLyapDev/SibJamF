using SibJam.TV;
using UnityEditor;
using UnityEngine;

namespace SibJam.Editor
{
    // Редактор телика одним экраном: базовое лицо, зона комнаты и реакции «когда → фразы с лицами»
    [CustomEditor(typeof(TVReactions))]
    public class TVReactionsEditor : UnityEditor.Editor
    {
        private static readonly string[] TriggerNames =
        {
            "Игрок вошёл в зону", "Игрок вышел из зоны", "Сломали камеру", "Сломали генератор", "Начался бой с боссом", "Победа над боссом",
        };

        private static readonly string[] OrderNames = { "По очереди", "Случайно" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var tv = (TVReactions)target;
            SerializedProperty screen = serializedObject.FindProperty("_screen");

            EditorGUILayout.PropertyField(screen, new GUIContent("Экран"));

            if (screen.objectReferenceValue != null)
            {
                var screenObject = new SerializedObject(screen.objectReferenceValue);
                screenObject.Update();
                EditorGUILayout.PropertyField(screenObject.FindProperty("_idleFace"), new GUIContent("Базовое лицо", "Пока телик молчит"));
                screenObject.ApplyModifiedProperties();
            }

            DrawZone(tv);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossFight"), new GUIContent("Бой с боссом", "Нужен для реакций на начало боя и победу"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Реакции", EditorStyles.boldLabel);
            SerializedProperty reactions = serializedObject.FindProperty("_reactions");

            for (int i = 0; i < reactions.arraySize; i++)
            {
                if (DrawReaction(reactions.GetArrayElementAtIndex(i), i))
                {
                    reactions.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            if (GUILayout.Button("+ Реакция"))
            {
                reactions.InsertArrayElementAtIndex(reactions.arraySize);
                SerializedProperty added = reactions.GetArrayElementAtIndex(reactions.arraySize - 1);
                added.FindPropertyRelative("Name").stringValue = "Новая реакция";
                added.FindPropertyRelative("Phrases").arraySize = 1;
                added.isExpanded = true;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawZone(TVReactions tv)
        {
            TVZone zone = tv.GetComponentInChildren<TVZone>();

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Зона комнаты", "Коллайдер-триггер для «вошёл / вышел»"));

                if (zone != null)
                {
                    if (GUILayout.Button("Выделить зону"))
                        Selection.activeGameObject = zone.gameObject;
                }
                else if (GUILayout.Button("Создать зону"))
                {
                    var go = new GameObject("Zone", typeof(BoxCollider), typeof(TVZone));
                    Undo.RegisterCreatedObjectUndo(go, "Зона телика");
                    go.transform.SetParent(tv.transform, false);
                    go.transform.localScale = new Vector3(8f, 4f, 8f);
                    Selection.activeGameObject = go;
                }
            }
        }

        // true — реакцию удалили
        private static bool DrawReaction(SerializedProperty reaction, int index)
        {
            SerializedProperty name = reaction.FindPropertyRelative("Name");
            SerializedProperty when = reaction.FindPropertyRelative("When");

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string title = $"{name.stringValue}  —  {TriggerNames[when.enumValueIndex]}";
                    reaction.isExpanded = EditorGUILayout.Foldout(reaction.isExpanded, title, true);

                    if (GUILayout.Button("✕", GUILayout.Width(22)))
                        return true;
                }

                if (!reaction.isExpanded)
                    return false;

                EditorGUILayout.PropertyField(name, new GUIContent("Название"));
                when.enumValueIndex = EditorGUILayout.Popup("Когда", when.enumValueIndex, TriggerNames);

                var trigger = (TVReactions.Trigger)when.enumValueIndex;

                if (trigger == TVReactions.Trigger.CameraBroken)
                    EditorGUILayout.PropertyField(reaction.FindPropertyRelative("Cameras"), new GUIContent("Камеры"));
                else if (trigger == TVReactions.Trigger.GeneratorBroken)
                    EditorGUILayout.PropertyField(reaction.FindPropertyRelative("Generators"), new GUIContent("Генераторы"));

                SerializedProperty order = reaction.FindPropertyRelative("Order");
                order.enumValueIndex = EditorGUILayout.Popup("Какую фразу", order.enumValueIndex, OrderNames);
                EditorGUILayout.PropertyField(reaction.FindPropertyRelative("Loop"), new GUIContent("По кругу", "Фразы кончились — начать заново. Иначе реакция замолкает"));

                EditorGUILayout.LabelField("Фразы", EditorStyles.miniBoldLabel);
                SerializedProperty phrases = reaction.FindPropertyRelative("Phrases");

                for (int i = 0; i < phrases.arraySize; i++)
                {
                    SerializedProperty phrase = phrases.GetArrayElementAtIndex(i);

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField((i + 1).ToString(), GUILayout.Width(18));
                        EditorGUILayout.PropertyField(phrase.FindPropertyRelative("Line"), GUIContent.none);
                        EditorGUILayout.PropertyField(phrase.FindPropertyRelative("Face"), GUIContent.none, GUILayout.Width(80));

                        if (GUILayout.Button("✕", GUILayout.Width(22)))
                        {
                            phrases.DeleteArrayElementAtIndex(i);
                            break;
                        }
                    }
                }

                if (GUILayout.Button("+ Фраза"))
                    phrases.InsertArrayElementAtIndex(phrases.arraySize);
            }

            return false;
        }
    }
}
