using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Таймлайн катсцены, как в Unity Timeline: шкала, под ней дорожки по типам элементов, каждый элемент — цветной клип.
    // Левый край клипа — когда элемент запускается, длина — сколько он звучит или висит на экране. Мгновенный элемент
    // (действие) — флажок: черта в момент вызова и подпись. Пересекающиеся клипы дорожка раскладывает в несколько строк.
    // Шкала — перемотка. Клик по клипу — выбрать (Shift — добавить, Ctrl — переключить), перетаскивание — сдвинуть
    // все выбранные. Рамка по пустому месту — выбрать несколько. Колесо — зум, средняя кнопка или Alt+перетаскивание — прокрутка.
    // Двойной клик по дорожке — новый элемент этого типа на эту секунду. ПКМ — меню.
    // Клавиши (после клика по таймлайну): Del — удалить, Ctrl+C/V/D — копировать, вставить на ползунок, дублировать на ползунок,
    // Ctrl+A — выбрать все, F — показать выбранные, A — показать всё, ←/→ — соседний элемент.
    public class CutsceneTimelineDrawer
    {
        public const float RulerHeight = 21f;
        public const float HeaderHeight = 21f;
        public const float LaneHeight = 28f;
        private const float Padding = 12f;
        private const float MinClipWidth = 6f;
        private const float FlagWidth = 2f;
        private const float FlagHead = 8f;
        private const float LaneGap = 4f;
        private const float TimeStep = 0.01f;
        // Сколько пикселей у правого края клипа хватает за растягивание
        private const float EdgeGrab = 4f;
        // Клип уже этого на экране краями не обрезается — за него только тащат. Обрезать: приблизить шкалу или ввести в строке
        private const float MinTrimWidth = 24f;
        private const float MinViewLength = 0.5f;
        private const float MaxViewLength = 600f;
        // Зум колесом: во сколько раз за единицу прокрутки (через экспоненту — плавно и одинаково в обе стороны)
        private const float WheelZoom = 0.02f;
        // Полоса прокрутки снизу: кнопки-стрелки и ширина краёв ползунка, за которые меняют масштаб
        private const float ScrollButton = 15f;
        private const float ThumbEdge = 7f;
        private const float MinThumbWidth = 24f;

        private static readonly Color Background = new Color(0.165f, 0.165f, 0.165f);
        private static readonly Color RowBand = new Color(0.2f, 0.2f, 0.2f);
        private static readonly Color RowLine = new Color(0f, 0f, 0f, 0.35f);
        private static readonly Color GuideColor = new Color(1f, 0.75f, 0.3f, 0.9f);
        private static readonly Color RowLineFaint = new Color(0f, 0f, 0f, 0.1f);
        private static readonly Color RulerBackground = new Color(0.235f, 0.235f, 0.235f);
        private static readonly Color HeaderBackground = new Color(0.19f, 0.19f, 0.19f);
        private static readonly Color TickColor = new Color(1f, 1f, 1f, 0.3f);
        private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.07f);
        private static readonly Color SelectedColor = new Color(0.35f, 0.6f, 1f);
        private static readonly Color PlayheadColor = new Color(1f, 1f, 1f, 0.9f);
        private static readonly Color EndColor = new Color(1f, 1f, 1f, 0.5f);
        private static readonly Color EndShade = new Color(0f, 0f, 0f, 0.3f);
        private static readonly Color BoxFill = new Color(0.36f, 0.62f, 1f, 0.12f);

        // Скопированные элементы: время от первого и клоны. Общий на все окна, как буфер Animation
        private static List<KeyValuePair<float, object>> _clipboard;

        private static GUIStyle _playheadStyle;
        private static GUIStyle _rulerStyle;
        private static GUIStyle _clipLabel;
        private static GUIStyle _rulerLabel;
        private static GUIStyle _guideLabel;
        private static GUIStyle _summaryLabel;
        private static Type[] _actionTypes;

        private readonly Action<float> _seek;
        private readonly Action<AudioClip> _playClip;
        private readonly HashSet<int> _selection = new HashSet<int>();
        private readonly List<Item> _items = new List<Item>();
        private readonly List<Track> _tracks = new List<Track>();
        private readonly Dictionary<int, float> _dragStart = new Dictionary<int, float>();

        // Добавленные через «Добавить дорожку» пустые дорожки
        private readonly HashSet<Type> _shownTracks = new HashSet<Type>();

        private int _selected = -1;
        private bool _dragging;
        private float _grabTime;
        private int _resizing = -1;
        private bool _resizeLeft;
        private float _resizeStart;
        private float _resizeEnd;
        private float _resizeOffset;
        // Полная длина клипа элемента (без обрезки). У субтитров — без предела
        private float _resizeFull;
        // Время, которое показывает направляющая при перетаскивании. Меньше нуля — направляющей нет
        private float _guideTime = -1f;
        private bool _scrubbing;
        private bool _panning;
        private bool _boxing;
        private Vector2 _boxStart;
        private Vector2 _boxEnd;

        // Видимый кусок шкалы. Пока его не трогали зумом, он следует за длиной катсцены
        private float _viewStart;
        private float _viewLength = 10f;
        private bool _viewTouched;
        // Перетаскивание ползунка снизу: 0 — нет, 1 — весь, 2 — левый край, 3 — правый край
        private int _thumbDrag;
        private float _thumbGrabX;
        private float _thumbGrabStart;
        private float _thumbGrabLength;
        private float _thumbTotal;
        private float _length = 10f;
        private float _playheadTime;

        public CutsceneTimelineDrawer(Action<float> seek, Action<AudioClip> playClip)
        {
            _seek = seek;
            _playClip = playClip;
        }

        // Выбор поменялся: окно перерисовывает карточку
        public event Action SelectionChanged;

        // Главный выбранный элемент (его поля показаны в карточке). Присвоение выбирает только его
        public int Selected
        {
            get => _selected;
            set => SetSelection(value >= 0 ? new[] { value } : Array.Empty<int>(), value);
        }

        public IReadOnlyCollection<int> SelectedActions => _selection;

        public void SetSelection(IEnumerable<int> actions, int primary)
        {
            var next = new HashSet<int>(actions);

            if (primary >= 0)
                next.Add(primary);
            else if (next.Count > 0)
                primary = next.Min();

            if (next.SetEquals(_selection) && primary == _selected)
                return;

            _selection.Clear();
            _selection.UnionWith(next);
            _selected = primary;
            SelectionChanged?.Invoke();
        }

        // Снять выделение без события: выделение уже ушло на другой объект
        public void ClearSelection()
        {
            _selection.Clear();
            _selected = -1;
        }

        // Клип на шкале. Top — от верха таймлайна
        private struct Item
        {
            public int Index;
            public CutsceneAction Action;
            public float X;
            public float Width;
            public float Extent;
            public float Top;
        }

        // Дорожка — строки одного типа элементов. Top — от верха таймлайна
        public struct Track
        {
            public Type Type;
            public CutsceneActionAttribute Info;
            public float Top;
            public float Height;
            public int Count;
            public bool Expanded;
            public bool Hidden;
            public List<Row> Rows;
        }

        // Строка одного элемента под раскрытой дорожкой. Top — от верха таймлайна
        public struct Row
        {
            public int Index;
            public CutsceneAction Action;
            public float Top;
        }

        // Какие дорожки раскрыты: общее на окна, живёт до перезагрузки домена
        private static readonly HashSet<Type> ExpandedTracks = new HashSet<Type>();

        public static bool IsExpanded(Type type) => ExpandedTracks.Contains(type);

        public static void SetExpanded(Type type, bool expanded)
        {
            if (expanded)
                ExpandedTracks.Add(type);
            else
                ExpandedTracks.Remove(type);
        }

        // Временно скрытые дорожки (глазик): в редакторе не видны на таймлайне и не звучат в превью. На игру не влияет
        private static readonly HashSet<Type> HiddenTracks = new HashSet<Type>();

        public static bool IsHidden(Type type) => type != null && HiddenTracks.Contains(type);

        public static void SetHidden(Type type, bool hidden)
        {
            if (hidden)
                HiddenTracks.Add(type);
            else
                HiddenTracks.Remove(type);
        }

        public static string TrackName(Type type) => GetInfo(type).Name;

        // Типы, которых сейчас нет среди дорожек, — для меню «Добавить дорожку»
        public IEnumerable<Type> MissingTracks => ActionTypes.Where(type => _tracks.All(track => track.Type != type));

        public void AddTrack(Type type) => _shownTracks.Add(type);

        // Убрать дорожку вместе со всеми её элементами
        public void RemoveTrack(SerializedProperty actions, Type type)
        {
            for (int i = actions.arraySize - 1; i >= 0; i--)
            {
                if (actions.GetArrayElementAtIndex(i).managedReferenceValue?.GetType() == type)
                    actions.DeleteArrayElementAtIndex(i);
            }

            _shownTracks.Remove(type);
            Selected = -1;
        }

        public IReadOnlyList<Track> Tracks => _tracks;

        // Верх области дорожек (под шкалой и строкой под ней)
        public const float TracksTop = RulerHeight + HeaderHeight;

        // Насколько дорожки прокручены вниз. Ставит окно (полоса справа и колесо над списком дорожек)
        public float ScrollY { get; set; }

        // Высота всех дорожек без прокрутки
        public float TracksHeight => ContentHeight + ScrollY - TracksTop;

        public float ContentHeight => _tracks.Count > 0
            ? _tracks[_tracks.Count - 1].Top + _tracks[_tracks.Count - 1].Height
            : RulerHeight + HeaderHeight;

        private static GUISkin BuiltinSkin =>
            EditorGUIUtility.GetBuiltinSkin(EditorGUIUtility.isProSkin ? EditorSkin.Scene : EditorSkin.Inspector);

        // Штатные стили окна Animation; если в версии Unity их нет — рисуем своими цветами
        private static GUIStyle PlayheadStyle => _playheadStyle ??= BuiltinSkin.FindStyle("AnimationPlayHead");
        private static GUIStyle RulerStyle => _rulerStyle ??= BuiltinSkin.FindStyle("TimeRulerBackground");

        private static GUIStyle ClipLabel => _clipLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            clipping = TextClipping.Clip,
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(5, 3, 0, 0),
            normal = { textColor = Color.white },
        };

        private static GUIStyle GuideLabel => _guideLabel ??= new GUIStyle(EditorStyles.miniBoldLabel)
        {
            normal = { textColor = Color.black },
        };

        private static GUIStyle RulerLabel => _rulerLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = new Color(1f, 1f, 1f, 0.55f) },
        };

        private static GUIStyle SummaryLabel => _summaryLabel ??= new GUIStyle(EditorStyles.miniLabel)
        {
            clipping = TextClipping.Clip,
            normal = { textColor = new Color(1f, 1f, 1f, 0.45f) },
        };

        private static Type[] ActionTypes => _actionTypes ??= TypeCache.GetTypesDerivedFrom<CutsceneAction>()
            .Where(type => !type.IsAbstract && !type.IsGenericType)
            .OrderBy(type => GetInfo(type).Name)
            .ToArray();

        // Раскладка дорожек и клипов под ширину таймлайна. Окно зовёт до отрисовки левой панели, чтобы строки совпали
        public void Layout(SerializedProperty actions, float length, float width)
        {
            UpdateView(length);
            float pixelsPerSecond = PixelsPerSecond(width);
            var all = new List<Item>();

            for (int i = 0; i < actions.arraySize; i++)
            {
                if (!(actions.GetArrayElementAtIndex(i).managedReferenceValue is CutsceneAction action))
                    continue;

                float clipWidth = action.Duration > 0f ? Mathf.Max(MinClipWidth, action.Duration * pixelsPerSecond) : FlagWidth;
                float labelWidth = ClipLabel.CalcSize(new GUIContent(action.Summary)).x;
                all.Add(new Item
                {
                    Index = i,
                    Action = action,
                    X = TimeToX(width, action.Time),
                    Width = clipWidth,
                    // У клипа с длиной подпись обрезается по плашке, у разового — идёт дальше флажка
                    Extent = action.Duration > 0f ? clipWidth : FlagHead + labelWidth,
                });
            }

            _items.Clear();
            _tracks.Clear();
            float top = TracksTop - ScrollY;

            // Скрытые дорожки — в самом низу
            foreach (Type type in ActionTypes.OrderBy(IsHidden))
            {
                List<Item> own = all.Where(item => item.Action.GetType() == type).OrderBy(item => item.Action.Time).ToList();

                // Дорожка видна, если в ней что-то есть или её добавили через «Добавить дорожку»
                if (own.Count == 0 && !_shownTracks.Contains(type))
                    continue;

                // Скрытая: только строка-заголовок, клипов на таймлайне нет
                if (IsHidden(type))
                {
                    _tracks.Add(new Track { Type = type, Info = GetInfo(type), Top = top, Height = LaneHeight, Count = own.Count, Hidden = true });
                    top += LaneHeight;
                    continue;
                }

                // Раскрытая: строка-заголовок, под ней по строке на элемент — слева его поля, справа его клип
                if (IsExpanded(type))
                {
                    var rows = new List<Row>();
                    float rowTop = top + LaneHeight;

                    foreach (Item source in own)
                    {
                        Item item = source;
                        item.Top = rowTop;
                        _items.Add(item);
                        rows.Add(new Row { Index = item.Index, Action = item.Action, Top = rowTop });
                        rowTop += LaneHeight;
                    }

                    _tracks.Add(new Track
                    {
                        Type = type, Info = GetInfo(type), Top = top, Height = rowTop - top, Count = own.Count, Expanded = true, Rows = rows,
                    });
                    top = rowTop;
                    continue;
                }

                // Свёрнутая: все клипы в одной строке. Подпись, на которую налезает следующий клип, обрезается
                // (полностью она видна в подсказке при наведении)
                int last = -1;

                foreach (Item source in own)
                {
                    if (last >= 0)
                    {
                        Item previous = _items[last];
                        previous.Extent = Mathf.Max(previous.Width, Mathf.Min(previous.Extent, source.X - LaneGap - previous.X));
                        _items[last] = previous;
                    }

                    Item item = source;
                    item.Top = top;
                    last = _items.Count;
                    _items.Add(item);
                }

                float height = LaneHeight;
                _tracks.Add(new Track { Type = type, Info = GetInfo(type), Top = top, Height = height, Count = own.Count });
                top += height;
            }
        }

        // Шкала, дорожки с клипами и ползунок. end — где катсцена кончается: дальше затемняем
        public void DrawTimeline(Rect area, SerializedProperty actions, float length, float time, float end = -1f)
        {
            Layout(actions, length, area.width);
            _playheadTime = time;

            // Всё рисуем в своей системе координат и обрезаем по краю — при зуме клипы уходят за край
            GUI.BeginClip(area);
            Rect rect = new Rect(0f, 0f, area.width, area.height);
            Rect ruler = new Rect(0f, 0f, rect.width, RulerHeight);
            Rect header = new Rect(0f, ruler.yMax, rect.width, HeaderHeight);

            HandleInput(rect, ruler, actions);

            if (Event.current.type == EventType.Repaint)
                Paint(rect, ruler, header, time, end);

            GUI.EndClip();
        }

        private void Paint(Rect rect, Rect ruler, Rect header, float time, float end)
        {
            EditorGUI.DrawRect(rect, Background);
            EditorGUI.DrawRect(header, HeaderBackground);
            BeginTracksClip(rect);

            foreach (Track track in _tracks)
            {
                // Дорожка подкрашена в цвет своего типа элементов
                Color tint = track.Hidden ? Color.gray : track.Info.Color;
                EditorGUI.DrawRect(new Rect(0f, track.Top, rect.width, track.Height), Color.Lerp(RowBand, tint, 0.12f));
                EditorGUI.DrawRect(new Rect(0f, track.Top, 3f, track.Height), WithAlpha(tint, 0.8f));
                EditorGUI.DrawRect(new Rect(0f, track.Top + track.Height - 1f, rect.width, 1f), RowLine);

                if (!track.Expanded)
                    continue;

                // Строки раскрытой дорожки разделены еле заметно — дорожка читается одним блоком
                foreach (Row row in track.Rows)
                {
                    if (_selection.Contains(row.Index))
                        EditorGUI.DrawRect(new Rect(0f, row.Top, rect.width, LaneHeight), WithAlpha(SelectedColor, 0.12f));

                    EditorGUI.DrawRect(new Rect(3f, row.Top, rect.width - 3f, 1f), RowLineFaint);
                }
            }

            GUI.EndClip();

            EditorGUI.DrawRect(ruler, RulerBackground);

            if (RulerStyle != null)
                RulerStyle.Draw(ruler, GUIContent.none, false, false, false, false);

            EditorGUI.DrawRect(new Rect(0f, ruler.yMax - 1f, rect.width, 1f), RowLine);
            EditorGUI.DrawRect(new Rect(0f, header.yMax - 1f, rect.width, 1f), RowLine);
            DrawRuler(rect, ruler);

            // До нуля и после конца катсцены — тень
            float zeroX = TimeToX(rect.width, 0f);

            if (zeroX > 0f)
                EditorGUI.DrawRect(new Rect(0f, ruler.yMax, zeroX, rect.height - RulerHeight), EndShade);

            if (end >= 0f)
            {
                float endX = TimeToX(rect.width, end);

                if (endX < rect.width)
                {
                    EditorGUI.DrawRect(new Rect(endX, ruler.yMax, rect.width - endX, rect.height - RulerHeight), EndShade);
                    EditorGUI.DrawRect(new Rect(endX, ruler.yMax, 1f, rect.height - RulerHeight), EndColor);
                    GUI.Label(new Rect(endX + 4f, header.y + 2f, 60f, 16f), "конец", RulerLabel);
                }
            }

            BeginTracksClip(rect);

            foreach (Item item in _items)
            {
                if (item.X > rect.width || item.X + item.Extent < 0f)
                    continue;

                DrawClip(item, _selection.Contains(item.Index));
            }

            GUI.EndClip();

            if (_boxing)
            {
                Rect box = BoxRect;
                EditorGUI.DrawRect(box, BoxFill);
                GUI.DrawTexture(box, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, WithAlpha(SelectedColor, 0.8f), 1f, 0f);
            }

            DrawPlayhead(rect, ruler, time);

            if (_guideTime >= 0f)
                DrawGuide(rect, ruler, _guideTime);
        }

        // Направляющая при перетаскивании: линия через таймлайн и время на шкале
        private void DrawGuide(Rect rect, Rect ruler, float time)
        {
            float x = TimeToX(rect.width, time);
            EditorGUI.DrawRect(new Rect(x, ruler.y, 1f, rect.height), GuideColor);

            var content = new GUIContent($"{time:0.00} с");
            Vector2 size = RulerLabel.CalcSize(content);
            Rect label = new Rect(Mathf.Min(x + 3f, rect.width - size.x - 6f), ruler.y + 1f, size.x + 6f, ruler.height - 2f);
            DrawRounded(label, GuideColor, 3f);
            GUI.Label(new Rect(label.x + 3f, label.y, size.x, label.height), content, GuideLabel);
        }

        // Клип с длиной — плашка своего цвета с подписью; разовый — флажок: черта с треугольником и подпись без полосы
        private static void DrawClip(Item item, bool selected)
        {
            Color color = GetInfo(item.Action.GetType()).Color;
            Rect body = new Rect(item.X, item.Top + 2f, item.Width, LaneHeight - 4f);
            bool instant = item.Action.Duration <= 0f;
            Rect label = new Rect(item.X + (instant ? FlagHead : 0f), body.y, item.Extent - (instant ? FlagHead : 0f), body.height);

            if (item.Action.Duration > 0f)
            {
                DrawRounded(body, WithAlpha(Color.Lerp(color, Color.black, 0.45f), 1f), 3f);
                DrawRounded(new Rect(body.x, body.y, Mathf.Min(3f, body.width), body.height), color, 1f);
            }
            else
            {
                EditorGUI.DrawRect(new Rect(item.X - FlagWidth * 0.5f, body.y - 1f, FlagWidth, body.height + 2f), selected ? Color.white : color);
                DrawFlag(item.X, body.y - 1f, selected ? Color.white : color);
            }

            if (selected && !instant)
            {
                Rect outline = new Rect(item.X - 1f, body.y - 1f, item.Width + 2f, body.height + 2f);
                GUI.DrawTexture(outline, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, Color.white, 1.5f, 3f);
            }

            Color text = ClipLabel.normal.textColor;
            ClipLabel.normal.textColor = instant && !selected ? new Color(1f, 1f, 1f, 0.7f) : Color.white;
            if (item.Action.IsEmpty)
                GUI.Label(new Rect(label.x, label.y, 16f, label.height), Icon("console.warnicon.sml", "Ничего не задано"));
            else
                GUI.Label(label, item.Action.Summary, ClipLabel);

            // Полное название при наведении — только если подпись обрезана соседним клипом
            if (!item.Action.IsEmpty && ClipLabel.CalcSize(new GUIContent(item.Action.Summary)).x > label.width)
                GUI.Label(new Rect(item.X, item.Top, Mathf.Max(item.Width, item.Extent), LaneHeight), new GUIContent(string.Empty, item.Action.Summary));

            ClipLabel.normal.textColor = text;
        }

        // Показать отрезок шкалы целиком (F — выбранные, A — всё)
        public void Frame(float from, float to)
        {
            float span = Mathf.Max(to - from, MinViewLength);
            float margin = span * 0.08f;
            _viewStart = from - margin;
            _viewLength = Mathf.Clamp(span + margin * 2f, MinViewLength, MaxViewLength);
            _viewTouched = true;
        }

        public void FrameAll(float length)
        {
            _viewTouched = false;
            UpdateView(length);
        }

        public void FrameSelected(SerializedProperty actions, float length)
        {
            List<CutsceneAction> valid = _selection.Select(index => Get(actions, index)).Where(action => action != null).ToList();

            if (valid.Count == 0)
            {
                FrameAll(length);
                return;
            }

            Frame(valid.Min(action => action.Time), valid.Max(action => action.End));
        }

        // Новый элемент нужного типа на секунду; становится выбранным
        public void AddAction(SerializedProperty actions, Type type, float time)
        {
            int index = actions.arraySize;
            actions.arraySize++;
            SerializedProperty added = actions.GetArrayElementAtIndex(index);
            added.managedReferenceValue = Activator.CreateInstance(type);
            added.FindPropertyRelative("_time").floatValue = Round(time);
            added.isExpanded = true;
            _shownTracks.Add(type);
            Selected = index;
            ApplySort(actions);
        }

        // Соседний элемент от ползунка: −1 — предыдущий, 1 — следующий
        public void SelectAdjacent(SerializedProperty actions, float time, int direction)
        {
            int best = -1;
            float bestTime = 0f;

            for (int i = 0; i < actions.arraySize; i++)
            {
                float start = GetTime(actions, i);
                bool ahead = direction > 0 ? start > time + TimeStep * 0.5f : start < time - TimeStep * 0.5f;

                if (ahead && (best < 0 || (direction > 0 ? start < bestTime : start > bestTime)))
                {
                    best = i;
                    bestTime = start;
                }
            }

            Select(actions, best);
        }

        public void DeleteSelected(SerializedProperty actions)
        {
            foreach (int index in _selection.Where(index => IsValid(actions, index)).OrderByDescending(index => index))
                actions.DeleteArrayElementAtIndex(index);

            Selected = -1;
        }

        public void Copy(SerializedProperty actions)
        {
            List<int> valid = _selection.Where(index => Get(actions, index) != null).OrderBy(index => index).ToList();

            if (valid.Count == 0)
                return;

            float first = valid.Min(index => GetTime(actions, index));
            _clipboard = valid
                .Select(index => new KeyValuePair<float, object>(GetTime(actions, index) - first, Clone(Get(actions, index))))
                .ToList();
        }

        public static bool CanPaste => _clipboard != null && _clipboard.Count > 0;

        // Вставить скопированное так, чтобы первый элемент встал на time. Вставленные становятся выбранными
        public void Paste(SerializedProperty actions, float time)
        {
            if (!CanPaste)
                return;

            var added = new List<int>();

            foreach (KeyValuePair<float, object> entry in _clipboard)
            {
                int index = actions.arraySize;
                actions.arraySize++;
                SerializedProperty action = actions.GetArrayElementAtIndex(index);
                action.managedReferenceValue = Clone(entry.Value);
                action.FindPropertyRelative("_time").floatValue = Round(time + entry.Key);
                added.Add(index);
            }

            SetSelection(added, added[0]);
            ApplySort(actions);
        }

        public void Duplicate(SerializedProperty actions, float time)
        {
            List<KeyValuePair<float, object>> saved = _clipboard;
            Copy(actions);
            Paste(actions, time);
            _clipboard = saved;
        }

        public void SelectAll(SerializedProperty actions) =>
            SetSelection(Enumerable.Range(0, actions.arraySize), actions.arraySize > 0 ? 0 : -1);

        // Меню срабатывает после отрисовки: свойство к тому моменту устарело, находим его заново по пути
        private static GenericMenu.MenuFunction Edit(SerializedProperty property, Action<SerializedProperty> change)
        {
            SerializedObject serializedObject = property.serializedObject;
            string path = property.propertyPath;

            return () =>
            {
                serializedObject.Update();
                change(serializedObject.FindProperty(path));
                serializedObject.ApplyModifiedProperties();
            };
        }

        private void HandleInput(Rect rect, Rect ruler, SerializedProperty actions)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Keyboard);
            Event current = Event.current;

            // Курсор обрезки над краями клипов, длину которых можно менять
            if (current.type == EventType.Repaint)
            {
                foreach (Item item in _items)
                {
                    if (!CanTrim(item, actions))
                        continue;

                    EditorGUIUtility.AddCursorRect(new Rect(item.X - EdgeGrab, item.Top, EdgeGrab * 2f, LaneHeight), MouseCursor.ResizeHorizontal);
                    EditorGUIUtility.AddCursorRect(new Rect(item.X + item.Width - EdgeGrab, item.Top, EdgeGrab * 2f, LaneHeight), MouseCursor.ResizeHorizontal);
                }
            }

            switch (current.GetTypeForControl(controlId))
            {
                case EventType.ScrollWheel when rect.Contains(current.mousePosition):
                {
                    // Колесо — зум вокруг курсора, Shift+колесо или горизонтальная прокрутка — сдвиг
                    float scroll = Mathf.Abs(current.delta.x) > Mathf.Abs(current.delta.y) ? current.delta.x : current.delta.y;

                    if (current.shift || Mathf.Abs(current.delta.x) > Mathf.Abs(current.delta.y))
                        _viewStart += scroll * _viewLength * 0.02f;
                    else
                        Zoom(rect.width, current.mousePosition.x, Mathf.Exp(Mathf.Clamp(scroll, -6f, 6f) * WheelZoom));

                    _viewTouched = true;
                    current.Use();
                    break;
                }

                case EventType.MouseDown when rect.Contains(current.mousePosition):
                {
                    GUIUtility.keyboardControl = controlId;
                    float clickTime = XToTime(rect.width, current.mousePosition.x);

                    // Средняя кнопка или Alt — прокрутка шкалы
                    if (current.button == 2 || (current.button == 0 && current.alt))
                    {
                        _panning = true;
                        GUIUtility.hotControl = controlId;
                        current.Use();
                        break;
                    }

                    // Шкала — перемотка
                    if (ruler.Contains(current.mousePosition))
                    {
                        if (current.button == 0)
                        {
                            _scrubbing = true;
                            GUIUtility.hotControl = controlId;
                            _seek(clickTime);
                        }

                        current.Use();
                        break;
                    }

                    int hit = HitItem(current.mousePosition);
                    int track = HitTrack(current.mousePosition);
                    bool left = false;
                    int edge = current.button == 0 ? HitEdge(current.mousePosition, actions, out left) : -1;

                    // Край клипа с длиной — обрезать: правый — конец, левый — начало
                    if (edge >= 0)
                    {
                        Selected = edge;
                        BeginTrim(actions, edge, left);
                        GUIUtility.hotControl = controlId;
                        current.Use();
                        break;
                    }

                    if (current.button == 1)
                    {
                        if (hit >= 0 && !_selection.Contains(hit))
                            Selected = hit;

                        ShowContextMenu(actions, hit, track, clickTime);
                        current.Use();
                        break;
                    }

                    if (current.button != 0)
                        break;

                    bool toggle = current.control || current.command;

                    if (hit >= 0)
                    {
                        if (toggle && _selection.Contains(hit))
                        {
                            SetSelection(_selection.Where(index => index != hit), _selected == hit ? -1 : _selected);
                            current.Use();
                            break;
                        }

                        if (toggle || current.shift || _selection.Contains(hit))
                            SetSelection(_selection, hit);
                        else
                            Selected = hit;

                        // Тянем все выбранные разом
                        _dragging = true;
                        _grabTime = clickTime;
                        _dragStart.Clear();

                        foreach (int index in _selection.Where(index => IsValid(actions, index)))
                            _dragStart[index] = GetTime(actions, index);

                        GUIUtility.hotControl = controlId;
                    }
                    else if (current.clickCount == 2 && track >= 0)
                    {
                        AddAction(actions, _tracks[track].Type, clickTime);
                    }
                    else
                    {
                        // Рамка выделения
                        if (!toggle && !current.shift)
                            Selected = -1;

                        _boxing = true;
                        _boxStart = _boxEnd = current.mousePosition;
                        GUIUtility.hotControl = controlId;
                    }

                    current.Use();
                    break;
                }

                case EventType.MouseDrag when GUIUtility.hotControl == controlId:
                {
                    if (_panning)
                    {
                        _viewStart -= current.delta.x / PixelsPerSecond(rect.width);
                        _viewTouched = true;
                    }
                    else if (_dragging)
                    {
                        float delta = Round(XToTime(rect.width, current.mousePosition.x) - _grabTime, true);
                        float shift = Mathf.Max(delta, -_dragStart.Values.DefaultIfEmpty(0f).Min());

                        foreach (KeyValuePair<int, float> start in _dragStart)
                        {
                            if (IsValid(actions, start.Key))
                                TimeProperty(actions, start.Key).floatValue = Round(start.Value + shift);
                        }

                        _guideTime = _dragStart.TryGetValue(_selected, out float grabbed) ? Round(grabbed + shift) : -1f;
                    }
                    else if (_resizing >= 0)
                    {
                        if (IsValid(actions, _resizing))
                            Trim(actions, Round(XToTime(rect.width, current.mousePosition.x)));
                    }
                    else if (_scrubbing)
                    {
                        _seek(XToTime(rect.width, current.mousePosition.x));
                    }
                    else if (_boxing)
                    {
                        _boxEnd = current.mousePosition;
                    }

                    current.Use();
                    break;
                }

                case EventType.MouseUp when GUIUtility.hotControl == controlId:
                {
                    GUIUtility.hotControl = 0;

                    if (_dragging || _resizing >= 0 && _resizeLeft)
                        ApplySort(actions);

                    if (_boxing)
                        SelectInBox(current.shift || current.control || current.command);

                    _dragging = false;
                    _resizing = -1;
                    _guideTime = -1f;
                    _scrubbing = false;
                    _panning = false;
                    _boxing = false;
                    current.Use();
                    break;
                }

                case EventType.ValidateCommand when GUIUtility.keyboardControl == controlId:
                {
                    if (IsCommand(current.commandName))
                        current.Use();

                    break;
                }

                case EventType.ExecuteCommand when GUIUtility.keyboardControl == controlId:
                {
                    if (ExecuteCommand(current.commandName, actions))
                        current.Use();

                    break;
                }

                case EventType.KeyDown when GUIUtility.keyboardControl == controlId:
                {
                    bool plain = !current.control && !current.command && !current.alt;

                    if ((current.keyCode == KeyCode.Delete || current.keyCode == KeyCode.Backspace) && _selection.Count > 0)
                        DeleteSelected(actions);
                    else if (current.keyCode == KeyCode.LeftArrow)
                        SelectAdjacent(actions, _playheadTime, -1);
                    else if (current.keyCode == KeyCode.RightArrow)
                        SelectAdjacent(actions, _playheadTime, 1);
                    else if (current.keyCode == KeyCode.F && plain)
                        FrameSelected(actions, _length);
                    else if (current.keyCode == KeyCode.A && plain)
                        FrameAll(_length);
                    else
                        break;

                    current.Use();
                    break;
                }
            }
        }

        private static bool IsCommand(string name) =>
            name == "Copy" || name == "Paste" || name == "Duplicate" || name == "Delete" || name == "SoftDelete"
            || name == "SelectAll" || name == "FrameSelected";

        // Штатные команды редактора (Ctrl+C/V/D, Del, Ctrl+A, F)
        private bool ExecuteCommand(string name, SerializedProperty actions)
        {
            switch (name)
            {
                case "Copy":
                    Copy(actions);
                    return true;
                case "Paste":
                    Paste(actions, _playheadTime);
                    return true;
                case "Duplicate":
                    Duplicate(actions, _playheadTime);
                    return true;
                case "Delete":
                case "SoftDelete":
                    DeleteSelected(actions);
                    return true;
                case "SelectAll":
                    SelectAll(actions);
                    return true;
                case "FrameSelected":
                    FrameSelected(actions, _length);
                    return true;
                default:
                    return false;
            }
        }

        // Выбрать клипы, задетые рамкой
        private void SelectInBox(bool additive)
        {
            Rect box = BoxRect;
            var picked = new HashSet<int>(additive ? _selection : Enumerable.Empty<int>());

            foreach (Item item in _items)
            {
                if (box.Overlaps(ItemRect(item)))
                    picked.Add(item.Index);
            }

            SetSelection(picked, picked.Contains(_selected) ? _selected : -1);
        }

        private Rect BoxRect => Rect.MinMaxRect(
            Mathf.Min(_boxStart.x, _boxEnd.x), Mathf.Min(_boxStart.y, _boxEnd.y),
            Mathf.Max(_boxStart.x, _boxEnd.x), Mathf.Max(_boxStart.y, _boxEnd.y));

        // Выбрать элемент и перемотать к его началу
        private void Select(SerializedProperty actions, int index)
        {
            if (!IsValid(actions, index))
                return;

            Selected = index;
            _seek(GetTime(actions, index));
        }

        private void ShowContextMenu(SerializedProperty actions, int hit, int track, float clickTime)
        {
            var menu = new GenericMenu();
            float playhead = _playheadTime;

            if (hit >= 0)
            {
                CutsceneAction action = Get(actions, hit);
                string count = _selection.Count > 1 ? $" ({_selection.Count})" : string.Empty;
                Rect at = new Rect(Event.current.mousePosition, Vector2.zero);
                int[] selected = _selection.ToArray();
                SerializedProperty list = actions.Copy();
                menu.AddItem(new GUIContent($"Настройки элемента{count}..."), false, () => CutsceneActionPopup.Show(at, list, hit, selected));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Перемотать к началу"), false, () => _seek(action.Time));

                if (action.Duration > 0f)
                    menu.AddItem(new GUIContent("Перемотать к концу"), false, () => _seek(action.End));

                if (action.PreviewClip != null)
                {
                    menu.AddItem(new GUIContent("Прослушать"), false, () => _playClip(action.PreviewClip));
                    menu.AddItem(new GUIContent("Остановить прослушивание"), false, CutsceneWindow.StopClips);
                }

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent($"Копировать{count}"), false, Edit(actions, Copy));
                menu.AddItem(new GUIContent($"Дублировать на {playhead:0.00} с{count}"), false, Edit(actions, list => Duplicate(list, playhead)));
                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent($"Удалить{count}"), false, Edit(actions, DeleteSelected));
            }
            else
            {
                foreach (Type type in ActionTypes)
                {
                    Type added = type;
                    string prefix = track >= 0 && _tracks[track].Type == type ? string.Empty : "Добавить/";
                    menu.AddItem(new GUIContent($"{prefix}{GetInfo(type).Name} на {clickTime:0.00} с"), false,
                        Edit(actions, list => AddAction(list, added, clickTime)));
                }

                if (CanPaste)
                    menu.AddItem(new GUIContent($"Вставить на {clickTime:0.00} с"), false, Edit(actions, list => Paste(list, clickTime)));
                else
                    menu.AddDisabledItem(new GUIContent("Вставить"));

                menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Перемотать сюда"), false, () => _seek(clickTime));
                menu.AddItem(new GUIContent("Показать всё  (A)"), false, () => FrameAll(_length));
            }

            menu.ShowAsContext();
        }

        // Копия элемента массива делила бы с оригиналом тот же объект [SerializeReference] — клонируем
        private static object Clone(object value)
        {
            if (value == null)
                return null;

            object copy = Activator.CreateInstance(value.GetType());
            EditorJsonUtility.FromJsonOverwrite(EditorJsonUtility.ToJson(value), copy);
            return copy;
        }

        private void SetTime(SerializedProperty actions, int index, float time)
        {
            TimeProperty(actions, index).floatValue = Round(time);
            Selected = index;
            ApplySort(actions);
        }

        // Держим элементы по порядку времени; выбор переезжает вместе с ними
        private void ApplySort(SerializedProperty actions)
        {
            int count = actions.arraySize;
            List<int> order = Enumerable.Range(0, count).ToList();

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count - 1 - i; j++)
                {
                    if (GetTime(actions, j) <= GetTime(actions, j + 1))
                        continue;

                    actions.MoveArrayElement(j + 1, j);
                    (order[j], order[j + 1]) = (order[j + 1], order[j]);
                }
            }

            // map[старый индекс] = новый
            var map = new int[count];

            for (int position = 0; position < count; position++)
                map[order[position]] = position;

            List<int> moved = _selection.Where(index => index >= 0 && index < count).Select(index => map[index]).ToList();
            int primary = _selected >= 0 && _selected < count ? map[_selected] : -1;
            SetSelection(moved, primary);
        }

        // Пока шкалу не зумили, она показывает катсцену целиком
        private void UpdateView(float length)
        {
            _length = Mathf.Max(length, MinViewLength);
            // Левее нуля и правее конца катсцены листать нечего
            _viewStart = Mathf.Clamp(_viewStart, 0f, Mathf.Max(0f, _length - _viewLength));

            if (_viewTouched)
                return;

            _viewStart = 0f;
            _viewLength = Mathf.Clamp(_length, MinViewLength, MaxViewLength);
        }

        // Область под шапкой; координаты внутри — те же, что снаружи
        private static void BeginTracksClip(Rect rect) =>
            GUI.BeginClip(new Rect(0f, TracksTop, rect.width, Mathf.Max(0f, rect.height - TracksTop)), new Vector2(0f, -TracksTop), Vector2.zero, false);

        // Полоса прокрутки снизу, как в окне Animation: ползунок — видимый отрезок шкалы.
        // Тащишь ползунок — листаешь, тащишь его край — меняешь масштаб, стрелки и клик мимо — листают
        public void DrawHorizontalScrollbar(Rect rect)
        {
            int id = GUIUtility.GetControlID(FocusType.Passive);
            Event current = Event.current;

            Rect leftButton = new Rect(rect.x, rect.y, ScrollButton, rect.height);
            Rect rightButton = new Rect(rect.xMax - ScrollButton, rect.y, ScrollButton, rect.height);
            Rect track = new Rect(leftButton.xMax, rect.y, Mathf.Max(1f, rect.width - ScrollButton * 2f), rect.height);

            // Вся шкала — катсцена целиком (или видимый отрезок, если он длиннее): ползунок при листании не меняет размер
            float total = Mathf.Max(_length, _viewLength);
            float thumbWidth = Mathf.Max(MinThumbWidth, _viewLength / total * track.width);
            float thumbX = track.x + _viewStart / total * track.width;
            Rect thumb = new Rect(Mathf.Min(thumbX, track.xMax - thumbWidth), track.y, thumbWidth, track.height);

            if (current.type == EventType.Repaint)
            {
                GUI.skin.horizontalScrollbar.Draw(track, false, false, false, false);
                GUIStyle thumbStyle = GUI.skin.FindStyle("horizontalMinMaxScrollbarThumb") ?? GUI.skin.horizontalScrollbarThumb;
                thumbStyle.Draw(thumb, thumb.Contains(current.mousePosition) || _thumbDrag != 0, false, _thumbDrag != 0, false);
                GUI.skin.horizontalScrollbarLeftButton.Draw(leftButton, false, false, false, false);
                GUI.skin.horizontalScrollbarRightButton.Draw(rightButton, false, false, false, false);
            }

            EditorGUIUtility.AddCursorRect(new Rect(thumb.x, thumb.y, ThumbEdge, thumb.height), MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(new Rect(thumb.xMax - ThumbEdge, thumb.y, ThumbEdge, thumb.height), MouseCursor.ResizeHorizontal);

            switch (current.GetTypeForControl(id))
            {
                case EventType.MouseDown when current.button == 0 && rect.Contains(current.mousePosition):
                {
                    if (leftButton.Contains(current.mousePosition) || rightButton.Contains(current.mousePosition))
                    {
                        float step = _viewLength * 0.1f * (leftButton.Contains(current.mousePosition) ? -1f : 1f);
                        _viewStart = Mathf.Max(0f, _viewStart + step);
                    }
                    else if (thumb.Contains(current.mousePosition))
                    {
                        _thumbDrag = current.mousePosition.x < thumb.x + ThumbEdge ? 2 : current.mousePosition.x > thumb.xMax - ThumbEdge ? 3 : 1;
                        _thumbGrabX = current.mousePosition.x;
                        _thumbGrabStart = _viewStart;
                        _thumbGrabLength = _viewLength;
                        _thumbTotal = total;
                        GUIUtility.hotControl = id;
                    }
                    else
                    {
                        // Клик мимо ползунка — на страницу в ту сторону
                        _viewStart = Mathf.Max(0f, _viewStart + (current.mousePosition.x < thumb.x ? -_viewLength : _viewLength));
                    }

                    _viewTouched = true;
                    current.Use();
                    break;
                }

                case EventType.MouseDrag when GUIUtility.hotControl == id:
                {
                    float delta = (current.mousePosition.x - _thumbGrabX) / track.width * _thumbTotal;
                    float end = _thumbGrabStart + _thumbGrabLength;

                    if (_thumbDrag == 1)
                    {
                        _viewStart = Mathf.Max(0f, _thumbGrabStart + delta);
                    }
                    else if (_thumbDrag == 2)
                    {
                        _viewStart = Mathf.Clamp(_thumbGrabStart + delta, Mathf.Max(0f, end - MaxViewLength), end - MinViewLength);
                        _viewLength = end - _viewStart;
                    }
                    else
                    {
                        _viewLength = Mathf.Clamp(_thumbGrabLength + delta, MinViewLength, MaxViewLength);
                    }

                    _viewTouched = true;
                    current.Use();
                    break;
                }

                case EventType.MouseUp when GUIUtility.hotControl == id:
                    GUIUtility.hotControl = 0;
                    _thumbDrag = 0;
                    current.Use();
                    break;
            }
        }

        private void Zoom(float width, float mouseX, float factor)
        {
            float pivot = XToTime(width, mouseX, false);
            _viewLength = Mathf.Clamp(_viewLength * factor, MinViewLength, MaxViewLength);
            _viewStart = pivot - (mouseX - Padding) / PixelsPerSecond(width);
        }

        private float PixelsPerSecond(float width) => Mathf.Max(1f, width - Padding * 2f) / _viewLength;

        private float TimeToX(float width, float time) => Padding + (time - _viewStart) * PixelsPerSecond(width);

        private float XToTime(float width, float x, bool clamp = true)
        {
            float time = _viewStart + (x - Padding) / PixelsPerSecond(width);
            return clamp ? Mathf.Max(0f, time) : time;
        }

        // Деления и сетка: шаг подбирается под зум, подписи — «минуты:секунды»
        private void DrawRuler(Rect rect, Rect ruler)
        {
            float pixelsPerSecond = PixelsPerSecond(rect.width);
            float[] steps = { 0.1f, 0.2f, 0.5f, 1f, 2f, 5f, 10f, 15f, 30f, 60f, 120f };
            float major = steps.FirstOrDefault(step => step * pixelsPerSecond >= 60f);

            if (major <= 0f)
                major = steps[steps.Length - 1];

            float minor = major * pixelsPerSecond / 5f >= 8f ? major / 5f : major / 2f;
            float viewEnd = XToTime(rect.width, rect.width, false);
            int first = Mathf.FloorToInt(Mathf.Max(0f, _viewStart) / minor);

            for (int i = first; i * minor <= viewEnd; i++)
            {
                float t = i * minor;
                float x = TimeToX(rect.width, t);
                bool isMajor = Mathf.Abs(t / major - Mathf.Round(t / major)) < 0.001f;
                float tick = isMajor ? ruler.height - 4f : 5f;
                EditorGUI.DrawRect(new Rect(x, ruler.yMax - tick, 1f, tick), TickColor);

                // Промежуточные деления — только засечкой на шкале, без линии через дорожки
                if (!isMajor)
                    continue;

                EditorGUI.DrawRect(new Rect(x, ruler.yMax, 1f, rect.yMax - ruler.yMax), GridColor);
                GUI.Label(new Rect(x + 3f, ruler.y + 2f, 50f, 16f), FormatTime(t, major < 1f), RulerLabel);
            }
        }

        private static string FormatTime(float time, bool fraction)
        {
            int minutes = Mathf.FloorToInt(time / 60f);
            float seconds = time - minutes * 60f;
            return fraction ? $"{minutes}:{seconds:00.0}" : $"{minutes}:{Mathf.RoundToInt(seconds):00}";
        }

        // Ползунок: светлая линия через весь таймлайн и ручка на шкале
        private void DrawPlayhead(Rect rect, Rect ruler, float time)
        {
            float x = TimeToX(rect.width, time);

            if (x < -6f || x > rect.width + 6f)
                return;

            EditorGUI.DrawRect(new Rect(x, ruler.yMax, 1f, rect.yMax - ruler.yMax), PlayheadColor);

            if (PlayheadStyle != null)
                PlayheadStyle.Draw(new Rect(x - 5f, ruler.y, 11f, ruler.height), GUIContent.none, false, false, false, false);
            else
                DrawRounded(new Rect(x - 6f, ruler.y + 2f, 13f, ruler.height - 5f), PlayheadColor, 3f);
        }

        // Треугольный флажок вправо от черты
        private static void DrawFlag(float x, float y, Color color)
        {
            for (int i = 0; i < 8; i++)
                EditorGUI.DrawRect(new Rect(x, y + i, FlagHead - Mathf.Abs(i - 3.5f) * 2f, 1f), color);
        }

        private static void DrawRounded(Rect rect, Color color, float radius) =>
            GUI.DrawTexture(rect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0f, color, 0f, radius);

        private static Color WithAlpha(Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

        private static GUIContent Icon(string name, string tooltip)
        {
            GUIContent content = EditorGUIUtility.IconContent(name);
            return new GUIContent(content.image, tooltip);
        }

        // За мгновенный элемент удобно хвататься и чуть левее черты
        private static Rect ItemRect(Item item) => new Rect(item.X - 3f, item.Top, item.Extent + 3f, LaneHeight);

        private int HitItem(Vector2 mouse)
        {
            if (mouse.y < TracksTop)
                return -1;

            // Сверху нарисован последний — его и берём
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (ItemRect(_items[i]).Contains(mouse))
                    return _items[i].Index;
            }

            return -1;
        }

        // Обрезать можно клип, у которого длина задаётся в самом элементе (поле _duration) и который на экране не слишком узкий
        private static bool CanTrim(Item item, SerializedProperty actions) =>
            item.Width >= MinTrimWidth && IsValid(actions, item.Index) && DurationProperty(actions, item.Index) != null;

        // Край клипа под мышью. left — левый (начало), иначе правый (конец)
        private int HitEdge(Vector2 mouse, SerializedProperty actions, out bool left)
        {
            left = false;

            if (mouse.y < TracksTop)
                return -1;

            for (int i = _items.Count - 1; i >= 0; i--)
            {
                Item item = _items[i];

                if (mouse.y < item.Top || mouse.y >= item.Top + LaneHeight || !CanTrim(item, actions))
                    continue;

                if (Mathf.Abs(mouse.x - (item.X + item.Width)) <= EdgeGrab)
                    return item.Index;

                if (Mathf.Abs(mouse.x - item.X) <= EdgeGrab)
                {
                    left = true;
                    return item.Index;
                }
            }

            return -1;
        }

        // Запоминаем элемент как был: обрезка считается от этого состояния, а не накапливается
        private void BeginTrim(SerializedProperty actions, int index, bool left)
        {
            CutsceneAction action = Get(actions, index);
            SerializedProperty offset = OffsetProperty(actions, index);

            _resizing = index;
            _resizeLeft = left;
            _resizeStart = GetTime(actions, index);
            _resizeEnd = _resizeStart + (action != null ? action.Duration : 0f);
            _resizeOffset = offset != null ? offset.floatValue : 0f;
            _resizeFull = action != null ? action.MaxDuration + _resizeOffset : float.PositiveInfinity;
        }

        private void Trim(SerializedProperty actions, float time)
        {
            SerializedProperty duration = DurationProperty(actions, _resizing);

            if (!_resizeLeft)
            {
                float max = _resizeFull - _resizeOffset;
                float end = Mathf.Clamp(time, _resizeStart + TimeStep, _resizeStart + max);
                SetDuration(duration, end - _resizeStart, max);
                _guideTime = end;
                return;
            }

            // Левый край: конец стоит на месте. Клип со сдвигом начала не тянется левее начала самого клипа
            SerializedProperty offset = OffsetProperty(actions, _resizing);
            float earliest = offset != null ? _resizeStart - _resizeOffset : 0f;
            float start = Mathf.Clamp(time, Mathf.Max(0f, earliest), _resizeEnd - TimeStep);
            float newOffset = Mathf.Max(0f, _resizeOffset + (start - _resizeStart));

            TimeProperty(actions, _resizing).floatValue = start;

            if (offset != null)
                offset.floatValue = (float)Math.Round(newOffset, 2);

            SetDuration(duration, _resizeEnd - start, offset != null ? _resizeFull - newOffset : float.PositiveInfinity);
            _guideTime = start;
        }

        private int HitTrack(Vector2 mouse)
        {
            if (mouse.y < TracksTop)
                return -1;

            for (int i = 0; i < _tracks.Count; i++)
            {
                if (mouse.y >= _tracks[i].Top && mouse.y < _tracks[i].Top + _tracks[i].Height)
                    return i;
            }

            return -1;
        }

        public static CutsceneActionAttribute GetInfo(Type type) =>
            type.GetCustomAttribute<CutsceneActionAttribute>() ?? new CutsceneActionAttribute(ObjectNames.NicifyVariableName(type.Name), "#999999");


        private static bool IsValid(SerializedProperty actions, int index) => index >= 0 && index < actions.arraySize;

        private static CutsceneAction Get(SerializedProperty actions, int index) =>
            IsValid(actions, index) ? actions.GetArrayElementAtIndex(index).managedReferenceValue as CutsceneAction : null;

        private static SerializedProperty TimeProperty(SerializedProperty actions, int index) =>
            actions.GetArrayElementAtIndex(index).FindPropertyRelative("_time");

        private static SerializedProperty DurationProperty(SerializedProperty actions, int index) =>
            actions.GetArrayElementAtIndex(index).FindPropertyRelative("_duration");

        // Дотянули до конца клипа — снова «весь клип» (0), чтобы замена клипа не обрезала его по старой длине
        public static void SetDuration(SerializedProperty duration, float value, float max) =>
            duration.floatValue = value >= max - TimeStep * 0.5f && !float.IsPositiveInfinity(max) ? 0f : (float)Math.Round(value, 2);

        private static SerializedProperty OffsetProperty(SerializedProperty actions, int index) =>
            actions.GetArrayElementAtIndex(index).FindPropertyRelative("_offset");

        public static SerializedProperty FindDuration(SerializedProperty action) => action.FindPropertyRelative("_duration");

        private static float GetTime(SerializedProperty actions, int index) => TimeProperty(actions, index)?.floatValue ?? 0f;

        // signed — для сдвига, он бывает отрицательным
        private static float Round(float time, bool signed = false)
        {
            float rounded = Mathf.Round(time / TimeStep) * TimeStep;
            return signed ? rounded : Mathf.Max(0f, rounded);
        }
    }
}
