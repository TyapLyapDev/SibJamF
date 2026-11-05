using SibJam.TV;
using UnityEditor;
using UnityEngine;

namespace SibJam.Editor
{
    // Редактор комнаты с теликами одним экраном: базовое лицо, телики комнаты, зона и реакции «когда → фразы с лицами»
    [CustomEditor(typeof(TVRoom))]
    public class TVRoomEditor : UnityEditor.Editor
    {
        private const string TVPrefabPath = "Assets/_Project/Art/Models/TV/TV.prefab";

        private static readonly string[] TriggerNames =
        {
            "Игрок вошёл в зону", "Игрок вышел из зоны", "Сломали камеру", "Сломали генератор", "Начался бой с боссом", "Победа над боссом",
            "Босс атакует", "Босс оглушён", "Босс встал после оглушения",
        };

        private static readonly string[] OrderNames = { "По очереди", "Случайно" };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var room = (TVRoom)target;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_idleFace"), new GUIContent("Базовое лицо", "Пока телики молчат"));

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();

                foreach (TVScreen screen in room.GetComponentsInChildren<TVScreen>(true))
                {
                    Undo.RecordObject(screen, "Базовое лицо");
                    screen.IdleFace = room.IdleFace;
                    EditorUtility.SetDirty(screen);
                }
            }

            DrawScreens(room);
            DrawZone(room);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossFight"), new GUIContent("Бой с боссом", "Нужен для реакций на начало боя и победу"));
            SerializedProperty boss = serializedObject.FindProperty("_boss");
            EditorGUILayout.PropertyField(boss, new GUIContent("Босс", "Нужен для реакций и лиц на атаку, оглушение и подъём босса"));

            if (boss.objectReferenceValue != null)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossFightFace"), new GUIContent("Лицо в бою"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossAttackFace"), new GUIContent("Лицо: атака"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossStaggeredFace"), new GUIContent("Лицо: оглушён"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossRecoveredFace"), new GUIContent("Лицо: встал"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("_bossDeadFace"), new GUIContent("Лицо: победа"));
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Реакции", EditorStyles.boldLabel);
            SerializedProperty reactions = serializedObject.FindProperty("_reactions");

            for (int i = 0; i < reactions.arraySize; i++)
            {
                if (DrawReaction(reactions.GetArrayElementAtIndex(i)))
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

        private static void DrawScreens(TVRoom room)
        {
            TVScreen[] screens = room.GetComponentsInChildren<TVScreen>(true);
            EditorGUILayout.LabelField($"Телики в комнате: {screens.Length}", EditorStyles.boldLabel);

            for (int i = 0; i < screens.Length; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    string role = i == 0 ? "  (голос отсюда)" : string.Empty;
                    EditorGUILayout.LabelField(screens[i].name + role);

                    if (GUILayout.Button("Выделить", GUILayout.Width(80)))
                        Selection.activeGameObject = screens[i].gameObject;
                }
            }

            if (GUILayout.Button("+ Телик в комнату"))
                AddScreen(room, screens);

            EditorGUILayout.HelpBox("Телик можно и просто перетащить внутрь этой комнаты в иерархии — он сразу начнёт говорить вместе с остальными.", MessageType.None);
        }

        private static void AddScreen(TVRoom room, TVScreen[] screens)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TVPrefabPath);
            var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, room.transform);
            Undo.RegisterCreatedObjectUndo(copy, "Телик в комнату");
            copy.name = $"TV_{screens.Length + 1}";

            Transform near = screens.Length > 0 ? screens[screens.Length - 1].transform : room.transform;
            copy.transform.SetPositionAndRotation(near.position + near.forward * 1.5f, near.rotation);
            copy.transform.localScale = screens.Length > 0 ? near.localScale : Vector3.one;
            copy.GetComponent<TVScreen>().IdleFace = room.IdleFace;
            Selection.activeGameObject = copy;
        }

        private static void DrawZone(TVRoom room)
        {
            TVZone zone = room.GetComponentInChildren<TVZone>(true);

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
                    Undo.RegisterCreatedObjectUndo(go, "Зона комнаты");
                    go.transform.SetParent(room.transform, false);
                    go.GetComponent<BoxCollider>().size = new Vector3(8f, 4f, 8f);
                    Selection.activeGameObject = go;
                }
            }
        }

        // Меню «все камеры локации»: камеры берутся из CameraDestructionGoal каждой локации
        private static void DrawLocationCamerasButton(SerializedProperty cameras)
        {
            if (!GUILayout.Button("Взять камеры локации…"))
                return;

            var menu = new GenericMenu();

            foreach (SecurityCameras.CameraDestructionGoal goal in Object.FindObjectsByType<SecurityCameras.CameraDestructionGoal>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var location = goal.GetComponentInParent<Locations.LocationLoop>(true);
                SerializedProperty source = new SerializedObject(goal).FindProperty("_securityCameras");

                if (source == null || source.arraySize == 0)
                    continue;

                string label = $"{(location != null ? location.name : goal.name)} ({source.arraySize})";
                SerializedProperty target = cameras;
                menu.AddItem(new GUIContent(label), false, () =>
                {
                    target.serializedObject.Update();
                    target.arraySize = source.arraySize;

                    for (int i = 0; i < source.arraySize; i++)
                        target.GetArrayElementAtIndex(i).objectReferenceValue = source.GetArrayElementAtIndex(i).objectReferenceValue;

                    target.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }

        // true — реакцию удалили
        private static bool DrawReaction(SerializedProperty reaction)
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

                var trigger = (TVRoom.Trigger)when.enumValueIndex;

                if (trigger == TVRoom.Trigger.CameraBroken)
                {
                    SerializedProperty cameras = reaction.FindPropertyRelative("Cameras");
                    EditorGUILayout.PropertyField(cameras, new GUIContent("Камеры"));

                    if (cameras.arraySize == 0)
                        EditorGUILayout.HelpBox("Камеры не выбраны — реакция не сработает. Перетащи камеры или возьми все камеры локации кнопкой ниже.", MessageType.Warning);

                    DrawLocationCamerasButton(cameras);
                }
                else if (trigger == TVRoom.Trigger.GeneratorBroken)
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
