using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Окно катсцен, по аналогии с окном Animation: правит клип (CutsceneClip) выделенной катсцены или сам выделенный клип.
    // Слева транспорт, клип и дорожки по типам элементов, под ними кадр и реакции сцены; справа — таймлайн.
    // Элемент правится прямо в своей строке слева
    public class CutsceneWindow : EditorWindow
    {
        // Пустую или короткую катсцену всё равно показываем на шкале хотя бы такой длины — чтобы было куда ставить метки
        private const float MinTimelineLength = 10f;
        private const float MinLeftWidth = 200f;
        private const string LeftWidthKey = "SibJam.CutsceneWindow.LeftWidth";
        private const float ScrollbarSize = 15f;
        // Низ окна под докой чуть обрезан — полосу снизу ставим выше
        private const float BottomInset = 6f;
        // Под последней дорожкой — кнопка «Добавить дорожку»
        private const float AddPropertySpace = 42f;
        // Отступ полей сверху, чтобы они стояли по центру строки
        private const float RowPad = (CutsceneTimelineDrawer.LaneHeight - 18f) * 0.5f;

        private static GUIStyle _subtitleStyle;
        private static GUIStyle _hintStyle;

        [SerializeField] private Cutscene _cutscene;
        [SerializeField] private CutsceneClip _clip;
        [SerializeField] private bool _locked;
        [SerializeField] private float _time;

        private SerializedObject _clipObject;
        private SerializedObject _cutsceneObject;
        private CutsceneTimelineDrawer _timeline;
        private CutscenePreview _preview;
        private bool _isPlayingPreview;

        // Как кнопка Preview в окне Animation: пока включена, сцена показывает позу на ползунке
        private bool _previewMode;
        private double _lastTickTime;

        private static GUIStyle SubtitleStyle => _subtitleStyle ??= new GUIStyle(EditorStyles.whiteLargeLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true,
        };

        private static GUIStyle HintStyle => _hintStyle ??= new GUIStyle(EditorStyles.centeredGreyMiniLabel) { wordWrap = true };

        // Открытое окно: инспектор элемента перематывает его ползунок
        public static CutsceneWindow Current { get; private set; }

        public float Time => _time;

        private float Length => _cutscene != null ? _cutscene.Length : _clip != null ? _clip.Length : 0f;

        [MenuItem("Window/SibJam/Катсцены")]
        public static CutsceneWindow Open()
        {
            var window = GetWindow<CutsceneWindow>();
            window.titleContent = new GUIContent("Катсцены", EditorGUIUtility.IconContent("Animation.Record").image);
            window.minSize = new Vector2(480f, 320f);
            window.Show();
            return window;
        }

        public static void Open(Cutscene cutscene)
        {
            CutsceneWindow window = Open();
            window.SetTarget(cutscene, cutscene != null ? cutscene.Clip : null);
        }

        public static void Open(CutsceneClip clip)
        {
            CutsceneWindow window = Open();
            window.SetTarget(null, clip);
        }

        // Двойной клик по файлу катсцены открывает окно
        [OnOpenAsset]
        private static bool OnOpenAsset(EntityId entityId, int line)
        {
            if (!(EditorUtility.EntityIdToObject(entityId) is CutsceneClip clip))
                return false;

            Open(clip);
            return true;
        }

        private void OnEnable()
        {
            Current = this;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += Repaint;
            Bind();

            if (_cutscene == null && _clip == null)
                OnSelectionChange();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= Repaint;

            if (Current == this)
                Current = null;

            StopPreview();
            _preview?.Dispose();
            _preview = null;
        }

        // Как окно Animation: показываем то, что выделено, пока окно не закреплено замком
        private void OnSelectionChange()
        {
            _timeline?.ClearSelection();

            if (_locked)
            {
                Repaint();
                return;
            }

            if (Selection.activeGameObject != null && Selection.activeGameObject.TryGetComponent(out Cutscene cutscene))
                SetTarget(cutscene, cutscene.Clip);
            else if (Selection.activeObject is CutsceneClip clip)
                SetTarget(null, clip);
            else
                return;

            Repaint();
        }

        private void Update()
        {
            if (Application.isPlaying && _cutscene != null && _cutscene.IsPlaying)
                Repaint();
        }

        private void SetTarget(Cutscene cutscene, CutsceneClip clip)
        {
            if (cutscene == _cutscene && clip == _clip && _clipObject != null)
                return;

            StopPreview();
            _preview?.Dispose();
            _preview = null;

            _cutscene = cutscene;
            _clip = clip;
            _time = 0f;
            Bind();
        }

        private void Bind()
        {
            // Контекст — катсцена в сцене: через неё ExposedReference в клипе показывают и принимают объекты сцены
            _clipObject = _clip != null ? new SerializedObject(_clip, _cutscene) : null;
            _cutsceneObject = _cutscene != null ? new SerializedObject(_cutscene) : null;
            _timeline = new CutsceneTimelineDrawer(Seek, PlayClip);
            _timeline.SelectionChanged += Repaint;
            _preview ??= CutscenePreview.Create(_cutscene);
        }

        // Как окно Animation: слева транспорт, клип и список дорожек, справа на тех же строках — шкала и клипы элементов
        // Ширина левой панели: тянется мышью за разделитель, запоминается между сессиями
        private float _leftWidth = -1f;
        private bool _resizingLeft;

        private float LeftWidth
        {
            get
            {
                if (_leftWidth < 0f)
                    _leftWidth = EditorPrefs.GetFloat(LeftWidthKey, 300f);

                return Mathf.Clamp(_leftWidth, MinLeftWidth, Mathf.Max(MinLeftWidth, position.width - 200f));
            }
        }

        private void HandleSplitter()
        {
            Rect handle = new Rect(LeftWidth - 3f, 0f, 6f, position.height);
            EditorGUIUtility.AddCursorRect(handle, MouseCursor.ResizeHorizontal);
            Event e = Event.current;

            if (e.type == EventType.MouseDown && e.button == 0 && handle.Contains(e.mousePosition))
            {
                _resizingLeft = true;
                e.Use();
            }
            else if (_resizingLeft && e.type == EventType.MouseDrag)
            {
                _leftWidth = Mathf.Max(MinLeftWidth, e.mousePosition.x);
                e.Use();
                Repaint();
            }
            else if (_resizingLeft && e.rawType == EventType.MouseUp)
            {
                _resizingLeft = false;
                EditorPrefs.SetFloat(LeftWidthKey, LeftWidth);
                e.Use();
            }
        }

        private void OnGUI()
        {
            if (_clipObject != null && _clipObject.targetObject == null)
                SetTarget(null, null);

            if (_cutsceneObject != null && _cutsceneObject.targetObject == null)
                SetTarget(null, _clip);

            HandleSplitter();
            Rect left = new Rect(0f, 0f, LeftWidth, position.height);
            Rect right = new Rect(LeftWidth + 1f, 0f, Mathf.Max(1f, position.width - LeftWidth - 1f), position.height);
            EditorGUI.DrawRect(new Rect(LeftWidth, 0f, 1f, position.height), new Color(0f, 0f, 0f, 0.45f));

            if (_clip == null)
            {
                DrawLeftHeader(left, null);
                GUILayout.BeginArea(right);
                DrawEmpty();
                GUILayout.EndArea();
                return;
            }

            _clipObject.Update();
            _cutsceneObject?.Update();

            SerializedProperty actions = _clipObject.FindProperty("_actions");
            float length = Length;
            float shown = Mathf.Max(length, MinTimelineLength);
            float time = Application.isPlaying && _cutscene != null && _cutscene.IsPlaying ? _cutscene.CurrentTime : _time;

            // Как в окне Animation: полоса прокрутки шкалы снизу и дорожек справа
            Rect timeline = new Rect(right.x, right.y, Mathf.Max(1f, right.width - ScrollbarSize), Mathf.Max(1f, right.height - ScrollbarSize - BottomInset));
            Rect horizontal = new Rect(timeline.x, timeline.yMax, timeline.width, ScrollbarSize);
            Rect vertical = new Rect(timeline.xMax, CutsceneTimelineDrawer.RulerHeight, ScrollbarSize, timeline.yMax - CutsceneTimelineDrawer.RulerHeight);

            _timeline.Layout(actions, shown, timeline.width);
            DrawLeftHeader(left, actions);

            // Дорожки слева прокручиваются вместе со шкалой и обрезаются под шапкой
            float tracksTop = CutsceneTimelineDrawer.TracksTop;
            GUI.BeginClip(new Rect(left.x, tracksTop, left.width, Mathf.Max(0f, timeline.yMax - tracksTop)), new Vector2(0f, -tracksTop), Vector2.zero, false);
            DrawTracks(left, actions);
            DrawAddProperty(left, actions);
            GUI.EndClip();

            HandleVerticalScroll(left, vertical, timeline.yMax - tracksTop);

            _timeline.DrawTimeline(timeline, actions, shown, time, length);
            _timeline.DrawHorizontalScrollbar(horizontal);

            _clipObject.ApplyModifiedProperties();
            _cutsceneObject?.ApplyModifiedProperties();
        }

        // Полоса справа и колесо над списком дорожек. Снизу место под кнопку «Добавить дорожку»
        private void HandleVerticalScroll(Rect left, Rect bar, float visible)
        {
            float content = _timeline.TracksHeight + AddPropertySpace;
            float max = Mathf.Max(0f, content - visible);
            float scroll = Mathf.Clamp(_timeline.ScrollY, 0f, max);
            Event current = Event.current;

            if (current.type == EventType.ScrollWheel && left.Contains(current.mousePosition) && current.mousePosition.y > CutsceneTimelineDrawer.TracksTop)
            {
                scroll = Mathf.Clamp(scroll + current.delta.y * 10f, 0f, max);
                current.Use();
            }

            // Всё влезает — полоса неактивна
            using (new EditorGUI.DisabledScope(max <= 0f))
                scroll = GUI.VerticalScrollbar(bar, scroll, Mathf.Min(visible, content), 0f, Mathf.Max(content, visible));

            if (!Mathf.Approximately(scroll, _timeline.ScrollY))
            {
                _timeline.ScrollY = scroll;
                Repaint();
            }
        }

        // Две строки слева, вровень со шкалой и строкой под ней: транспорт и время, потом клип и кнопки
        private void DrawLeftHeader(Rect left, SerializedProperty actions)
        {
            float time = Application.isPlaying && _cutscene != null && _cutscene.IsPlaying ? _cutscene.CurrentTime : _time;

            GUILayout.BeginArea(new Rect(left.x, 0f, left.width, CutsceneTimelineDrawer.RulerHeight), EditorStyles.toolbar);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(actions == null || Application.isPlaying))
                {
                    if (GUILayout.Button(new GUIContent("Preview", "Открыть окно превью: кадр и субтитр на ползунке"),
                            EditorStyles.toolbarButton, GUILayout.Width(56)))
                    {
                        SetPreviewMode(true);
                        CutscenePreviewWindow.Open();
                    }

                    if (GUILayout.Button(Icon("Animation.FirstKey", "В начало"), EditorStyles.toolbarButton, GUILayout.Width(28)))
                        Seek(0f);

                    if (GUILayout.Button(Icon("Animation.PrevKey", "Предыдущий элемент"), EditorStyles.toolbarButton, GUILayout.Width(28)))
                        _timeline.SelectAdjacent(actions, _time, -1);

                    string playIcon = _isPlayingPreview ? "PauseButton" : "PlayButton";
                    string playTip = _isPlayingPreview ? "Пауза" : "Превью со звуком, без запуска игры";

                    if (GUILayout.Button(Icon(playIcon, playTip), EditorStyles.toolbarButton, GUILayout.Width(28)))
                        TogglePreviewPlayback();

                    if (GUILayout.Button(Icon("Animation.NextKey", "Следующий элемент"), EditorStyles.toolbarButton, GUILayout.Width(28)))
                        _timeline.SelectAdjacent(actions, _time, 1);

                    if (GUILayout.Button(Icon("Animation.LastKey", "В конец"), EditorStyles.toolbarButton, GUILayout.Width(28)))
                        Seek(Length);
                }

                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(actions == null || Application.isPlaying))
                {
                    EditorGUI.BeginChangeCheck();
                    // До сотых: точнее элементы всё равно не ставятся
                    float newTime = EditorGUILayout.FloatField((float)Math.Round(time, 2), EditorStyles.toolbarTextField, GUILayout.Width(52));

                    if (EditorGUI.EndChangeCheck())
                        Seek(newTime);
                }

                GUILayout.Label($"/ {Length:0.00}", EditorStyles.miniLabel, GUILayout.ExpandWidth(false));
            }

            GUILayout.EndArea();

            GUILayout.BeginArea(new Rect(left.x, CutsceneTimelineDrawer.RulerHeight, left.width, CutsceneTimelineDrawer.HeaderHeight), EditorStyles.toolbar);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_cutscene == null))
                {
                    // Как в окне Animation: список клипов проекта и создание нового
                    var label = new GUIContent(_clip != null ? _clip.name : "Нет клипа");

                    if (EditorGUILayout.DropdownButton(label, FocusType.Passive, EditorStyles.toolbarDropDown, GUILayout.MinWidth(80f)))
                        ShowClipMenu();
                }


                using (new EditorGUI.DisabledScope(_cutscene == null || Application.isPlaying))
                {
                    if (EditorGUILayout.DropdownButton(Icon("PlayButton On", "Запустить игру: с начала или с ползунка"), FocusType.Passive,
                            EditorStyles.toolbarDropDown, GUILayout.Width(36)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Игра с начала — как увидит игрок"), false, () => EnterPlayMode(0f));
                        menu.AddItem(new GUIContent($"Игра с {_time:0.00} с (пройденное — без звука и субтитров)"), false,
                            () => EnterPlayMode(_time));
                        menu.ShowAsContext();
                    }
                }

                _locked = GUILayout.Toggle(_locked, Icon(_locked ? "IN LockButton on" : "IN LockButton", "Закрепить: не переключаться на выделение"),
                    EditorStyles.toolbarButton, GUILayout.Width(26));
            }

            GUILayout.EndArea();
        }

        // Строки дорожек слева: цвет, имя, что идёт на ползунке, «+» — добавить такой элемент на ползунок, «−» — убрать дорожку
        private void DrawTracks(Rect left, SerializedProperty actions)
        {
            for (int i = 0; i < _timeline.Tracks.Count; i++)
            {
                CutsceneTimelineDrawer.Track track = _timeline.Tracks[i];
                Rect row = new Rect(left.x, track.Top, left.width, track.Height);
                Rect line = new Rect(row.x, row.y, row.width, CutsceneTimelineDrawer.LaneHeight);

                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUI.DrawRect(row, new Color(0.2f, 0.2f, 0.2f));
                    EditorGUI.DrawRect(new Rect(row.x, row.yMax - 1f, row.width, 1f), new Color(0f, 0f, 0f, 0.35f));
                    EditorGUI.DrawRect(new Rect(row.x, row.y, 3f, row.height - 1f), track.Info.Color);
                }

                using (new EditorGUI.DisabledScope(track.Hidden))
                {
                    // Стрелка — раскрыть дорожку по элементам и править их прямо здесь
                    bool expanded = EditorGUI.Foldout(new Rect(line.x + 8f, line.y + RowPad, 14f, 18f), track.Expanded, GUIContent.none, true);

                    if (expanded != track.Expanded)
                        CutsceneTimelineDrawer.SetExpanded(track.Type, expanded);

                    GUI.Label(new Rect(line.x + 22f, line.y + RowPad, 90f, 18f), track.Info.Name, EditorStyles.label);
                }

                // Скрытая дорожка — глазик, по нему вернуть. Скрыть — ПКМ по строке
                if (track.Hidden && GUI.Button(new Rect(line.xMax - 64f, line.y + RowPad, 18f, 16f),
                        Icon("scenevis_hidden_hover", "Показать дорожку"), EditorStyles.iconButton))
                    CutsceneTimelineDrawer.SetHidden(track.Type, false);

                if (Event.current.type == EventType.ContextClick && line.Contains(Event.current.mousePosition))
                {
                    Type type = track.Type;
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent(track.Hidden ? "Показать дорожку" : "Временно скрыть дорожку"), false,
                        () => CutsceneTimelineDrawer.SetHidden(type, !CutsceneTimelineDrawer.IsHidden(type)));
                    menu.ShowAsContext();
                    Event.current.Use();
                }

                if (GUI.Button(new Rect(line.xMax - 24f, line.y + RowPad, 18f, 16f),
                        Icon("Toolbar Plus", $"Добавить «{track.Info.Name}» на {_time:0.00} с"), EditorStyles.iconButton))
                {
                    _timeline.AddAction(actions, track.Type, _time);
                    CutsceneTimelineDrawer.SetExpanded(track.Type, true);
                }

                if (GUI.Button(new Rect(line.xMax - 44f, line.y + RowPad, 18f, 16f),
                        Icon("Toolbar Minus", "Убрать дорожку вместе с её элементами"), EditorStyles.iconButton))
                {
                    // Без подтверждения: удаление отменяется Ctrl+Z
                    _timeline.RemoveTrack(actions, track.Type);
                    // ExitGUI ниже прерывает OnGUI — сохраняем сейчас, иначе удаление пропадёт
                    actions.serializedObject.ApplyModifiedProperties();
                    Repaint();
                    GUIUtility.ExitGUI();
                }

                // Клик по строке дорожки (мимо кнопок) — тоже раскрыть/свернуть
                if (!track.Hidden && Event.current.type == EventType.MouseDown && Event.current.button == 0 && line.Contains(Event.current.mousePosition))
                {
                    CutsceneTimelineDrawer.SetExpanded(track.Type, !track.Expanded);
                    Event.current.Use();
                    Repaint();
                }

                if (track.Expanded && !track.Hidden)
                {
                    foreach (CutsceneTimelineDrawer.Row child in track.Rows)
                        DrawElementRow(left, actions, child);
                }
            }
        }

        // Строка элемента: время, главное поле и удаление прямо в строке. Клик — выбор и перемотка к нему
        private void DrawElementRow(Rect left, SerializedProperty actions, CutsceneTimelineDrawer.Row child)
        {
            Rect line = new Rect(left.x, child.Top, left.width, CutsceneTimelineDrawer.LaneHeight);

            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(new Rect(line.x + 3f, line.y, line.width - 3f, 1f), new Color(0f, 0f, 0f, 0.1f));

                if (_timeline.SelectedActions.Contains(child.Index))
                    EditorGUI.DrawRect(new Rect(line.x + 3f, line.y + 1f, line.width - 3f, line.height - 1f), new Color(0.24f, 0.37f, 0.59f, 0.6f));
            }

            if (child.Index >= actions.arraySize)
                return;

            SerializedProperty action = actions.GetArrayElementAtIndex(child.Index);
            SerializedProperty start = action.FindPropertyRelative("_time");

            // Время правится прямо тут; клик по полю — ещё и выбор элемента
            EditorGUI.BeginChangeCheck();
            float time = EditorGUI.DelayedFloatField(new Rect(line.x + 22f, line.y + RowPad, 44f, 17f), start.floatValue, EditorStyles.miniTextField);

            if (EditorGUI.EndChangeCheck())
                start.floatValue = Mathf.Max(0f, (float)Math.Round(time, 2));

            // Длина справа, перед корзиной: у растягиваемых правится, у остальных просто видна
            SerializedProperty duration = CutsceneTimelineDrawer.FindDuration(action);
            Rect length = new Rect(line.xMax - 70f, line.y + RowPad, 42f, 17f);

            if (duration != null)
            {
                EditorGUI.BeginChangeCheck();
                float value = EditorGUI.DelayedFloatField(length, (float)Math.Round(child.Action.Duration, 2), EditorStyles.miniTextField);

                if (EditorGUI.EndChangeCheck())
                {
                    float max = child.Action.MaxDuration;
                    CutsceneTimelineDrawer.SetDuration(duration, Mathf.Clamp(value, 0.01f, max), max);
                }
            }
            else if (child.Action.Duration > 0f)
            {
                GUI.Label(length, $"{child.Action.Duration:0.00}", EditorStyles.miniLabel);
            }

            // Без длины главное поле тянется до корзины
            SerializedProperty main = FindMainField(action, child.Action.GetType());
            float fieldEnd = duration != null || child.Action.Duration > 0f ? length.x - 4f : line.xMax - 28f;
            Rect field = new Rect(line.x + 70f, line.y + RowPad, fieldEnd - line.x - 70f, 17f);

            if (main == null)
                GUI.Label(field, child.Action.Summary, EditorStyles.miniLabel);
            else if (main.propertyType == SerializedPropertyType.String)
                main.stringValue = EditorGUI.TextField(field, main.stringValue);
            else
                EditorGUI.PropertyField(field, main, GUIContent.none);

            // Удаление сразу, без подтверждения: отменяется Ctrl+Z
            if (GUI.Button(new Rect(line.xMax - 24f, line.y + RowPad, 18f, 16f), Icon("TreeEditor.Trash", "Удалить элемент"), EditorStyles.iconButton))
            {
                actions.DeleteArrayElementAtIndex(child.Index);
                _timeline.Selected = -1;
                actions.serializedObject.ApplyModifiedProperties();
                Repaint();
                GUIUtility.ExitGUI();
            }

            // Клик по строке (мимо полей) — выбрать элемент и перемотать к нему
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && line.Contains(Event.current.mousePosition))
            {
                _timeline.Selected = child.Index;
                Seek(child.Action.Time);
                Event.current.Use();
            }
        }

        // Поле с [CutsceneMainField], без него — первое видимое
        private static SerializedProperty FindMainField(SerializedProperty action, Type type)
        {
            SerializedProperty field = action.Copy();
            SerializedProperty end = action.GetEndProperty();
            SerializedProperty first = null;

            if (!field.NextVisible(true))
                return null;

            while (!SerializedProperty.EqualContents(field, end))
            {
                first ??= field.Copy();
                FieldInfo info = type.GetField(field.name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (info != null && info.IsDefined(typeof(CutsceneMainFieldAttribute)))
                    return field.Copy();

                if (!field.NextVisible(false))
                    break;
            }

            return first;
        }

        // «Добавить дорожку», как Add Property в окне Animation: меню дорожек, которых ещё нет
        private void DrawAddProperty(Rect left, SerializedProperty actions)
        {
            List<Type> missing = _timeline.MissingTracks.ToList();
            Rect button = new Rect(left.x + 30f, _timeline.ContentHeight + 10f, left.width - 60f, 22f);

            using (new EditorGUI.DisabledScope(missing.Count == 0))
            {
                if (!GUI.Button(button, new GUIContent("Добавить дорожку", missing.Count == 0 ? "Все дорожки уже добавлены" : "Добавить дорожку")))
                    return;
            }

            var menu = new GenericMenu();

            foreach (Type type in missing)
            {
                Type track = type;
                string name = CutsceneTimelineDrawer.TrackName(type);
                menu.AddItem(new GUIContent(name), false, () =>
                {
                    _timeline.AddTrack(track);
                    Repaint();
                });
            }

            menu.DropDown(button);
        }

        private void DrawEmpty()
        {
            GUILayout.FlexibleSpace();
            GUILayout.Label(
                _cutscene != null
                    ? $"У катсцены «{_cutscene.name}» нет клипа. Создайте новый или выберите файл слева сверху."
                    : "Выделите объект с катсценой в сцене или файл катсцены в проекте.",
                HintStyle);

            if (_cutscene != null)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Создать клип катсцены", GUILayout.Width(200), GUILayout.Height(28)))
                        CreateClip();

                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.FlexibleSpace();
        }

        // Кадр на текущей секунде (если катсцена умеет его показать) и субтитр поверх. Рисует окно превью
        public void DrawFrame(float width)
        {
            if (_clipObject == null || _clipObject.targetObject == null)
            {
                GUILayout.Label("Нет клипа катсцены.", HintStyle);
                return;
            }

            if (Application.isPlaying)
            {
                GUILayout.Label("Идёт игра — кадр смотрите в окне Game.", HintStyle);
                return;
            }

            _clipObject.Update();
            SerializedProperty actions = _clipObject.FindProperty("_actions");
            bool showFrame = _preview != null && _previewMode;
            Rect rect = showFrame ? _preview.Draw(width, _time) : Rect.zero;

            if (rect.width <= 0f)
                rect = GUILayoutUtility.GetRect(width, _preview != null ? width * 9f / 16f : 70f, GUILayout.ExpandWidth(true));

            if (Event.current.type != EventType.Repaint)
                return;

            if (!showFrame)
            {
                EditorGUI.DrawRect(rect, new Color(0.05f, 0.05f, 0.05f));
                string label = _preview != null ? "Preview выключен — нажмите Preview или перемотайте шкалу" : "Субтитры";
                GUI.Label(new Rect(rect.x + 6f, rect.y + 4f, rect.width - 12f, 14f), label, EditorStyles.miniLabel);
            }

            string subtitle = GetSubtitleAt(actions, _time);

            if (subtitle == null)
                return;

            Rect subtitleRect = new Rect(rect.x + 20f, rect.yMax - 46f, rect.width - 40f, 36f);
            EditorGUI.DrawRect(subtitleRect, new Color(0f, 0f, 0f, 0.55f));
            GUI.Label(subtitleRect, subtitle, SubtitleStyle);
        }

        // Последняя реплика или субтитр, который на этой секунде висит на экране (как Subtitles: новый заменяет старый)
        private static string GetSubtitleAt(SerializedProperty actions, float time)
        {
            string result = null;
            float latestStart = float.MinValue;

            for (int i = 0; i < actions.arraySize; i++)
            {
                object value = actions.GetArrayElementAtIndex(i).managedReferenceValue;

                if (CutsceneTimelineDrawer.IsHidden(value?.GetType()))
                    continue;

                string speaker;
                string text;
                float duration;

                if (value is VoiceLineAction voice && voice.Line != null)
                    (speaker, text, duration) = (voice.Line.Speaker, voice.Line.Text, voice.Line.Duration);
                else if (value is SubtitleAction subtitle)
                    (speaker, text, duration) = (subtitle.Speaker, subtitle.Text, subtitle.Duration);
                else
                    continue;

                float start = ((CutsceneAction)value).Time;

                if (time < start || start < latestStart || string.IsNullOrEmpty(text))
                    continue;

                latestStart = start;
                result = time <= start + duration
                    ? (string.IsNullOrEmpty(speaker) ? text : $"<color=#FFBF59>{speaker}:</color> {text}")
                    : null;
            }

            return result;
        }

        private void CreateClip()
        {
            string path = EditorUtility.SaveFilePanelInProject("Новый клип катсцены", _cutscene.name, "asset",
                "Куда сохранить клип катсцены", "Assets/_Project/Configs/Cutscenes");

            if (string.IsNullOrEmpty(path))
                return;

            var clip = CreateInstance<CutsceneClip>();
            AssetDatabase.CreateAsset(clip, path);
            AssetDatabase.SaveAssets();
            AssignClip(clip);
        }

        private void ShowClipMenu()
        {
            var menu = new GenericMenu();
            IEnumerable<CutsceneClip> clips = AssetDatabase.FindAssets("t:" + nameof(CutsceneClip))
                .Select(guid => AssetDatabase.LoadAssetAtPath<CutsceneClip>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(clip => clip != null)
                .OrderBy(clip => clip.name);

            foreach (CutsceneClip clip in clips)
            {
                CutsceneClip picked = clip;
                menu.AddItem(new GUIContent(clip.name), clip == _clip, () => AssignClip(picked));
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Создать новый клип..."), false, CreateClip);
            menu.ShowAsContext();
        }

        private void AssignClip(CutsceneClip clip)
        {
            if (_cutscene == null)
                return;

            var cutsceneObject = new SerializedObject(_cutscene);
            cutsceneObject.FindProperty("_cutsceneClip").objectReferenceValue = clip;
            cutsceneObject.ApplyModifiedProperties();

            _clip = clip;
            Bind();
        }

        public void Seek(float time)
        {
            StopPlayback();
            SetPreviewMode(true);
            SetTime(Mathf.Clamp(time, 0f, Mathf.Max(Length, MinTimelineLength)));
        }

        private void SetTime(float time)
        {
            _time = time;

            if (_previewMode)
                _preview?.OnTimeChanged(time);

            Repaint();
            CutscenePreviewWindow.RepaintOpen();
        }

        private void SetPreviewMode(bool enabled)
        {
            if (enabled == _previewMode || (enabled && Application.isPlaying))
                return;

            _previewMode = enabled;

            if (enabled)
                _preview?.OnTimeChanged(_time);
            else
            {
                StopPlayback();
                _preview?.Stop();
            }

            Repaint();
            CutscenePreviewWindow.RepaintOpen();
        }

        private void TogglePreviewPlayback()
        {
            if (_isPlayingPreview)
            {
                StopPlayback();
                return;
            }

            if (_time >= Length)
                _time = 0f;

            SetPreviewMode(true);
            _isPlayingPreview = true;
            _lastTickTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Tick;
        }

        private void Tick()
        {
            if (_clip == null || Length <= 0f)
            {
                StopPlayback();
                return;
            }

            double now = EditorApplication.timeSinceStartup;
            float previous = _time;
            float time = Mathf.Min(_time + (float)(now - _lastTickTime), Length);
            _lastTickTime = now;

            foreach (CutsceneAction action in _clip.Actions)
            {
                if (action != null && !CutsceneTimelineDrawer.IsHidden(action.GetType()) && action.Time >= previous && action.Time < time)
                    PlayClip(action.PreviewClip);
            }

            SetTime(time);

            if (time >= Length)
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
            _previewMode = false;
            _preview?.Stop();
        }

        private void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingEditMode)
                StopPreview();

            Repaint();
        }

        private void EnterPlayMode(float startTime)
        {
            StopPreview();
            var cutsceneObject = new SerializedObject(_cutscene);
            cutsceneObject.FindProperty("_editorStartTime").floatValue = startTime;
            cutsceneObject.ApplyModifiedProperties();
            EditorApplication.isPlaying = true;
        }

        private static GUIContent Icon(string name, string tooltip) =>
            new GUIContent(EditorGUIUtility.IconContent(name).image, tooltip);

        // Прослушка в редакторе через внутренний AudioUtil (публичного API для этого нет)
        public static void PlayClip(AudioClip clip)
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
