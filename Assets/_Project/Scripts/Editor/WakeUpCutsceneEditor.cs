using System;
using System.Collections.Generic;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SibJam.EditorTools
{
    // Инспектор катсцены: превью глазами героя на любой секунде, шкала времени с репликами и правка таймингов без запуска игры
    [CustomEditor(typeof(WakeUpCutscene))]
    public class WakeUpCutsceneEditor : UnityEditor.Editor
    {
        private const int PreviewWidth = 640;
        private const int PreviewHeight = 360;
        private const float TimelineHeight = 54f;

        private static readonly Color SoundColor = new Color(0.35f, 0.7f, 1f);
        private static readonly Color TextColor = new Color(1f, 0.7f, 0.25f);
        private static readonly Color EventColor = new Color(0.45f, 0.9f, 0.45f);
        private static readonly Color PlayheadColor = new Color(1f, 0.25f, 0.25f);

        private SerializedProperty _rig;
        private SerializedProperty _clip;
        private SerializedProperty _eye;
        private SerializedProperty _hiddenBones;
        private SerializedProperty _cues;
        private SerializedProperty _editorStartTime;

        private float _time;
        private bool _isPlayingPreview;
        private double _lastTickTime;
        private RenderTexture _previewTexture;
        private Camera _previewCamera;
        private bool _showSettings;

        private AnimationClip Clip => _clip.objectReferenceValue as AnimationClip;

        private void OnEnable()
        {
            _rig = serializedObject.FindProperty("_rig");
            _clip = serializedObject.FindProperty("_clip");
            _eye = serializedObject.FindProperty("_eye");
            _hiddenBones = serializedObject.FindProperty("_hiddenBones");
            _cues = serializedObject.FindProperty("_cues");
            _editorStartTime = serializedObject.FindProperty("_editorStartTime");

            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            StopPreview();

            if (_previewTexture != null)
                DestroyImmediate(_previewTexture);

            if (_previewCamera != null)
                DestroyImmediate(_previewCamera.gameObject);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Идёт игра. Тайминги можно менять и сейчас, но превью работает только вне Play.", MessageType.Info);
                DrawCues();
                DrawSettings();
                serializedObject.ApplyModifiedProperties();
                return;
            }

            DrawLaunchButtons();

            if (Clip == null || _rig.objectReferenceValue == null || _eye.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Для превью нужны Rig, Clip и Eye в настройках ниже.", MessageType.Warning);
            }
            else
            {
                DrawPlaybackControls();
                DrawTimeline();
                DrawPreview();
            }

            DrawCues();
            DrawSettings();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLaunchButtons()
        {
            EditorGUILayout.LabelField("Запуск", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Play с начала — как увидит игрок. Play с ползунка — сразу с выбранной секунды (уже пройденные действия, например старт робота, выполнятся, звуки и субтитры — нет).",
                MessageType.None);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("▶ Play с начала", GUILayout.Height(26)))
                    EnterPlayMode(0f);

                if (GUILayout.Button($"▶ Play с {_time:0.0} с", GUILayout.Height(26)))
                    EnterPlayMode(_time);
            }

            EditorGUILayout.Space();
        }

        private void DrawPlaybackControls()
        {
            EditorGUILayout.LabelField("Превью без запуска игры", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(_isPlayingPreview ? "❚❚" : "▶", GUILayout.Width(36), GUILayout.Height(20)))
                    TogglePreviewPlayback();

                if (GUILayout.Button("■", GUILayout.Width(36), GUILayout.Height(20)))
                {
                    StopPlayback();
                    SetTime(0f);
                }

                EditorGUI.BeginChangeCheck();
                float time = EditorGUILayout.Slider(_time, 0f, Clip.length);

                if (EditorGUI.EndChangeCheck())
                    SetTime(time);
            }
        }

        private void DrawTimeline()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, TimelineHeight, GUILayout.ExpandWidth(true));
            float length = Clip.length;

            EditorGUI.DrawRect(rect, new Color(0.13f, 0.13f, 0.13f));

            // Секунды: риска каждую секунду, подпись каждые 5
            for (int second = 0; second <= Mathf.FloorToInt(length); second++)
            {
                float x = TimeToX(rect, second, length);
                bool isMajor = second % 5 == 0;
                EditorGUI.DrawRect(new Rect(x, rect.yMax - (isMajor ? 10f : 5f), 1f, isMajor ? 10f : 5f), new Color(1f, 1f, 1f, 0.3f));

                if (isMajor)
                    GUI.Label(new Rect(x + 2f, rect.yMax - 22f, 30f, 14f), second.ToString(), EditorStyles.miniLabel);
            }

            for (int i = 0; i < _cues.arraySize; i++)
            {
                SerializedProperty cue = _cues.GetArrayElementAtIndex(i);
                float start = cue.FindPropertyRelative("_time").floatValue;
                float x = TimeToX(rect, start, length);
                VoiceLine line = GetLine(cue);
                bool hasText = line != null && !string.IsNullOrEmpty(line.Text);
                bool hasSound = cue.FindPropertyRelative("_clip").objectReferenceValue != null || (line != null && line.Clip != null);
                bool hasEvent = HasEvent(cue);

                if (hasText)
                {
                    float end = TimeToX(rect, Mathf.Min(start + line.Duration, length), length);
                    EditorGUI.DrawRect(new Rect(x, rect.y + 16f, Mathf.Max(2f, end - x), 8f), new Color(TextColor.r, TextColor.g, TextColor.b, 0.6f));
                }

                Color color = hasText ? TextColor : hasSound ? SoundColor : hasEvent ? EventColor : Color.gray;
                EditorGUI.DrawRect(new Rect(x - 1f, rect.y + 2f, 2f, rect.height - 14f), color);
                GUI.Label(new Rect(x + 2f, rect.y + 1f, 24f, 14f), (i + 1).ToString(), EditorStyles.miniBoldLabel);
            }

            EditorGUI.DrawRect(new Rect(TimeToX(rect, _time, length) - 1f, rect.y, 2f, rect.height), PlayheadColor);

            Event current = Event.current;

            if ((current.type == EventType.MouseDown || current.type == EventType.MouseDrag) && rect.Contains(current.mousePosition))
            {
                StopPlayback();
                SetTime(Mathf.Clamp01((current.mousePosition.x - rect.x) / rect.width) * length);
                current.Use();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawLegend(TextColor, "субтитр");
                DrawLegend(SoundColor, "только звук");
                DrawLegend(EventColor, "только действие");
            }
        }

        private static void DrawLegend(Color color, string label)
        {
            Rect rect = GUILayoutUtility.GetRect(10f, 14f, GUILayout.Width(10f));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y + 3f, 8f, 8f), color);
            GUILayout.Label(label, EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
        }

        private void DrawPreview()
        {
            if (_previewTexture == null)
                RenderPreview();

            float width = EditorGUIUtility.currentViewWidth - 30f;
            Rect rect = GUILayoutUtility.GetRect(width, width * PreviewHeight / PreviewWidth, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit);

            string subtitle = GetSubtitleAt(_time);

            if (subtitle == null)
                return;

            var style = new GUIStyle(EditorStyles.whiteLargeLabel) { alignment = TextAnchor.MiddleCenter, wordWrap = true, richText = true };
            Rect subtitleRect = new Rect(rect.x + 20f, rect.yMax - 46f, rect.width - 40f, 36f);
            EditorGUI.DrawRect(subtitleRect, new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(subtitleRect, subtitle, style);
        }

        private void DrawCues()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Реплики, звуки и действия по времени", EditorStyles.boldLabel);

            int removeIndex = -1;

            for (int i = 0; i < _cues.arraySize; i++)
            {
                SerializedProperty cue = _cues.GetArrayElementAtIndex(i);
                SerializedProperty time = cue.FindPropertyRelative("_time");

                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        cue.isExpanded = EditorGUILayout.Foldout(cue.isExpanded, $"{i + 1}. {Describe(cue)}", true);

                        time.floatValue = Mathf.Max(0f, EditorGUILayout.FloatField(time.floatValue, GUILayout.Width(50)));
                        GUILayout.Label("с", GUILayout.Width(12));

                        if (!Application.isPlaying && GUILayout.Button(new GUIContent("◎", "Поставить на текущую секунду ползунка"), GUILayout.Width(24)))
                            time.floatValue = _time;

                        if (!Application.isPlaying && GUILayout.Button(new GUIContent("→", "Перемотать превью к этой реплике"), GUILayout.Width(24)))
                        {
                            StopPlayback();
                            SetTime(time.floatValue);
                        }

                        VoiceLine cueLine = GetLine(cue);
                        AudioClip clip = cueLine != null && cueLine.Clip != null
                            ? cueLine.Clip
                            : cue.FindPropertyRelative("_clip").objectReferenceValue as AudioClip;

                        using (new EditorGUI.DisabledScope(clip == null))
                        {
                            if (GUILayout.Button(new GUIContent("♪", "Прослушать звук"), GUILayout.Width(24)))
                                PlayClip(clip);
                        }

                        if (GUILayout.Button(new GUIContent("✕", "Удалить"), GUILayout.Width(24)))
                            removeIndex = i;
                    }

                    if (!cue.isExpanded)
                        continue;

                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("_line"), new GUIContent("Реплика", "Озвучка и субтитр"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("_clip"), new GUIContent("Звук", "Без субтитра: шумы, музыка"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("_source"), new GUIContent("Откуда звучит", "Пусто — голос героя"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("_event"), new GUIContent("Действие"));
                }
            }

            if (removeIndex >= 0)
                _cues.DeleteArrayElementAtIndex(removeIndex);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button($"+ Добавить на {_time:0.0} с"))
                    AddCue(_time);

                if (GUILayout.Button("Упорядочить по времени"))
                    SortCues();
            }
        }

        private void DrawSettings()
        {
            EditorGUILayout.Space();
            _showSettings = EditorGUILayout.Foldout(_showSettings, "Остальные настройки", true);

            if (_showSettings)
                DrawPropertiesExcluding(serializedObject, "m_Script", "_cues");
        }

        private void AddCue(float time)
        {
            int index = _cues.arraySize;
            _cues.arraySize++;
            SerializedProperty cue = _cues.GetArrayElementAtIndex(index);

            // Новый элемент копирует предыдущий — очищаем
            cue.FindPropertyRelative("_time").floatValue = time;
            cue.FindPropertyRelative("_clip").objectReferenceValue = null;
            cue.FindPropertyRelative("_source").objectReferenceValue = null;
            cue.FindPropertyRelative("_line").objectReferenceValue = null;
            cue.FindPropertyRelative("_event").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize = 0;
            cue.isExpanded = true;
        }

        private void SortCues()
        {
            // Пузырёк через MoveArrayElement: так сохраняются все поля, включая настроенные действия
            for (int i = 0; i < _cues.arraySize; i++)
            {
                for (int j = 0; j < _cues.arraySize - 1 - i; j++)
                {
                    float current = _cues.GetArrayElementAtIndex(j).FindPropertyRelative("_time").floatValue;
                    float next = _cues.GetArrayElementAtIndex(j + 1).FindPropertyRelative("_time").floatValue;

                    if (current > next)
                        _cues.MoveArrayElement(j + 1, j);
                }
            }
        }

        private static string Describe(SerializedProperty cue)
        {
            VoiceLine line = GetLine(cue);
            var parts = new List<string>();

            if (line != null)
                parts.Add(string.IsNullOrEmpty(line.Text) ? line.name
                    : string.IsNullOrEmpty(line.Speaker) ? line.Text : $"{line.Speaker}: {line.Text}");

            if (cue.FindPropertyRelative("_clip").objectReferenceValue != null)
                parts.Add("♪");

            if (HasEvent(cue))
                parts.Add("действие");

            string description = parts.Count > 0 ? string.Join("  ", parts) : "(пусто)";
            return description.Length > 48 ? description.Substring(0, 45) + "..." : description;
        }

        private static bool HasEvent(SerializedProperty cue) =>
            cue.FindPropertyRelative("_event").FindPropertyRelative("m_PersistentCalls.m_Calls").arraySize > 0;

        private static VoiceLine GetLine(SerializedProperty cue) =>
            cue.FindPropertyRelative("_line").objectReferenceValue as VoiceLine;

        // Последний субтитр, который на этой секунде висит на экране (как Subtitles: новый заменяет старый)
        private string GetSubtitleAt(float time)
        {
            string result = null;
            float latestStart = float.MinValue;

            for (int i = 0; i < _cues.arraySize; i++)
            {
                SerializedProperty cue = _cues.GetArrayElementAtIndex(i);
                VoiceLine line = GetLine(cue);
                float start = cue.FindPropertyRelative("_time").floatValue;

                if (line == null || string.IsNullOrEmpty(line.Text) || time < start || start < latestStart)
                    continue;

                latestStart = start;
                result = time <= start + line.Duration
                    ? (string.IsNullOrEmpty(line.Speaker) ? line.Text : $"<color=#FFBF59>{line.Speaker}:</color> {line.Text}")
                    : null;
            }

            return result;
        }

        private static float TimeToX(Rect rect, float time, float length) =>
            rect.x + rect.width * Mathf.Clamp01(time / length);

        private void SetTime(float time)
        {
            _time = time;
            RenderPreview();
            Repaint();
        }

        private void TogglePreviewPlayback()
        {
            if (_isPlayingPreview)
            {
                StopPlayback();
                return;
            }

            if (_time >= Clip.length)
                _time = 0f;

            _isPlayingPreview = true;
            _lastTickTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void Tick()
        {
            if (target == null || Clip == null)
            {
                StopPlayback();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float previous = _time;
            _time = Mathf.Min(_time + (float)(now - _lastTickTime), Clip.length);
            _lastTickTime = now;

            serializedObject.Update();

            for (int i = 0; i < _cues.arraySize; i++)
            {
                SerializedProperty cue = _cues.GetArrayElementAtIndex(i);
                float start = cue.FindPropertyRelative("_time").floatValue;

                if (start >= previous && start < _time)
                    PlayClip(cue.FindPropertyRelative("_clip").objectReferenceValue as AudioClip);
            }

            RenderPreview();
            Repaint();

            if (_time >= Clip.length)
                StopPlayback();
        }

        private void StopPlayback()
        {
            if (!_isPlayingPreview)
                return;

            _isPlayingPreview = false;
            EditorApplication.update -= Tick;
            StopClips();
        }

        private void StopPreview()
        {
            StopPlayback();

            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                StopPreview();
        }

        private void EnterPlayMode(float startTime)
        {
            StopPreview();
            _editorStartTime.floatValue = startTime;
            serializedObject.ApplyModifiedProperties();
            EditorApplication.isPlaying = true;
        }

        // Ставит тело в позу через AnimationMode (сцена не меняется) и рендерит кадр глазами героя
        private void RenderPreview()
        {
            var rig = (Animator)_rig.objectReferenceValue;
            var eye = (Transform)_eye.objectReferenceValue;

            if (rig == null || eye == null || Clip == null)
                return;

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(rig.gameObject, Clip, _time);
            AnimationMode.EndSampling();

            EnsurePreviewCamera();
            _previewCamera.transform.SetPositionAndRotation(eye.position + eye.rotation * serializedObject.FindProperty("_eyeOffset").vector3Value, eye.rotation);

            var hiddenBones = new List<Transform>();

            for (int i = 0; i < _hiddenBones.arraySize; i++)
            {
                if (_hiddenBones.GetArrayElementAtIndex(i).objectReferenceValue is Transform bone)
                    hiddenBones.Add(bone);
            }

            // В игре катсцена выключает игрока, в превью просто не рисуем его
            var playerRenderers = new List<Renderer>();
            GameObject player = GameObject.FindWithTag(serializedObject.FindProperty("_playerTag").stringValue);

            if (player != null)
                playerRenderers.AddRange(player.GetComponentsInChildren<Renderer>());

            foreach (Renderer playerRenderer in playerRenderers)
                playerRenderer.forceRenderingOff = true;

            // Скин в редакторе сам не обновляется до перерисовки — запекаем меши в позе и рендерим их
            var skinned = rig.GetComponentsInChildren<SkinnedMeshRenderer>();
            var baked = new List<GameObject>();
            var scales = new Vector3[hiddenBones.Count];

            for (int i = 0; i < hiddenBones.Count; i++)
            {
                scales[i] = hiddenBones[i].localScale;
                hiddenBones[i].localScale = Vector3.zero;
            }

            try
            {
                foreach (SkinnedMeshRenderer renderer in skinned)
                {
                    var mesh = new Mesh();
                    renderer.BakeMesh(mesh, true);
                    var bakedObject = new GameObject("CutscenePreviewBake") { hideFlags = HideFlags.HideAndDontSave };
                    bakedObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    // Запечённый меш в единицах объекта: у рига из Blender масштаб может быть 100, его надо вернуть
                    bakedObject.transform.localScale = renderer.transform.lossyScale;
                    bakedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var bakedRenderer = bakedObject.AddComponent<MeshRenderer>();
                    bakedRenderer.sharedMaterials = renderer.sharedMaterials;
                    bakedRenderer.renderingLayerMask = renderer.renderingLayerMask;
                    renderer.forceRenderingOff = true;
                    baked.Add(bakedObject);
                }

                _previewCamera.Render();
            }
            finally
            {
                foreach (GameObject bakedObject in baked)
                {
                    DestroyImmediate(bakedObject.GetComponent<MeshFilter>().sharedMesh);
                    DestroyImmediate(bakedObject);
                }

                foreach (SkinnedMeshRenderer renderer in skinned)
                    renderer.forceRenderingOff = false;

                foreach (Renderer playerRenderer in playerRenderers)
                    playerRenderer.forceRenderingOff = false;

                for (int i = 0; i < hiddenBones.Count; i++)
                    hiddenBones[i].localScale = scales[i];
            }
        }

        private void EnsurePreviewCamera()
        {
            if (_previewTexture == null)
                _previewTexture = new RenderTexture(PreviewWidth, PreviewHeight, 24) { hideFlags = HideFlags.HideAndDontSave };

            if (_previewCamera != null)
                return;

            var cameraObject = new GameObject("CutscenePreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
            _previewCamera = cameraObject.AddComponent<Camera>();
            _previewCamera.enabled = false;
            _previewCamera.fieldOfView = 60f;
            _previewCamera.nearClipPlane = 0.03f;
            _previewCamera.targetTexture = _previewTexture;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        }

        // Прослушка в редакторе через внутренний AudioUtil (публичного API для этого нет)
        private static void PlayClip(AudioClip clip)
        {
            if (clip == null)
                return;

            MethodInfo method = AudioUtilType?.GetMethod("PlayPreviewClip", BindingFlags.Static | BindingFlags.Public,
                null, new[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
            method?.Invoke(null, new object[] { clip, 0, false });
        }

        private static void StopClips()
        {
            MethodInfo method = AudioUtilType?.GetMethod("StopAllPreviewClips", BindingFlags.Static | BindingFlags.Public);
            method?.Invoke(null, null);
        }

        private static Type AudioUtilType => typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
    }
}
