using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SibJam.Editor
{
    // Стиль «мониторы наблюдения» для UI: тёмные панели с тонкой рамкой, прямые углы, кнопки с красной заливкой при наведении.
    // Меню SibJam/UI/Apply Redesign перекрашивает префабы окон; сцены — методы для Main и Menu
    public static class UiRedesign
    {
        public static readonly Color Ground = Hex("0B0C0F");
        public static readonly Color Panel = Hex("0E0F13F0");
        public static readonly Color Line = Hex("3A3D46");
        public static readonly Color Text = Hex("ECE7DD");
        public static readonly Color Muted = Hex("A8A399");
        public static readonly Color Alarm = Hex("E5484D");
        public static readonly Color Signal = Hex("F0A040");

        private const string SpritesPath = "Assets/_Project/Art/Sprites/UI/";
        private const string FontsPath = "Assets/_Project/Art/Fonts/Bergamasco/";

        public static Sprite Solid => AssetDatabase.LoadAssetAtPath<Sprite>(SpritesPath + "UI_Solid.png");
        public static Sprite Frame => AssetDatabase.LoadAssetAtPath<Sprite>(SpritesPath + "UI_Frame.png");
        public static Sprite Scanlines => AssetDatabase.LoadAssetAtPath<Sprite>(SpritesPath + "UI_Scanlines.png");
        public static TMP_FontAsset Bold => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsPath + "Bergamasco Bold SDF.asset");
        public static TMP_FontAsset Regular => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontsPath + "Bergamasco Regular SDF.asset");

        public static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out Color color) ? color : Color.magenta;

        // Сплошная подложка с тонкой рамкой поверх (рамка — дочерний объект, в раскладке не участвует)
        public static void StylePanel(Image image, Color fill, Color? frame = null)
        {
            image.sprite = Solid;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            image.color = fill;
            EnsureFrame(image.transform, frame ?? Line);
        }

        public static Image EnsureFrame(Transform parent, Color color)
        {
            // Кнопка — вложенный префаб с уже своей рамкой: вторую, добавленную поверх, убираем
            bool kept = false;

            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Transform child = parent.GetChild(i);

                if (child.name != "Frame")
                    continue;

                bool owned = PrefabUtility.IsPartOfPrefabInstance(child) && !PrefabUtility.IsAddedGameObjectOverride(child.gameObject);

                if (!kept && owned)
                {
                    kept = true;
                    continue;
                }

                if (!owned && (kept || HasOwnedFrame(parent)))
                    Object.DestroyImmediate(child.gameObject);
            }

            Image frame = EnsureChildImage(parent, "Frame", Frame, color, first: false);
            frame.type = Image.Type.Sliced;
            // Рамка в 2 единицы UI: в 1 единицу она на 1600–1920 px рассыпается в пунктир
            frame.pixelsPerUnitMultiplier = 0.5f;
            return frame;
        }

        private static bool HasOwnedFrame(Transform parent)
        {
            foreach (Transform child in parent)
            {
                if (child.name == "Frame" && PrefabUtility.IsPartOfPrefabInstance(child) && !PrefabUtility.IsAddedGameObjectOverride(child.gameObject))
                    return true;
            }

            return false;
        }

        // Главное меню: обои с названием остаются, колонка кнопок справа в новом стиле
        public static void StyleMenuScene()
        {
            Transform buttons = GameObject.Find("UI/MenuButtons").transform;
            var panel = buttons.GetComponent<Image>();
            panel.sprite = Solid;
            panel.color = new Color(0f, 0f, 0f, 0f);

            var rect = (RectTransform)buttons;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-110f, -40f);
            rect.localScale = Vector3.one;

            var layout = buttons.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 12f;
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.childAlignment = TextAnchor.MiddleRight;

            foreach (Transform child in buttons)
            {
                if (child.GetComponent<Button>() == null)
                    continue;

                StyleButton(child.gameObject, primary: child.name == "ContinueButton", fontSize: 30f);
                ((RectTransform)child).sizeDelta = new Vector2(440f, 66f);
                child.localScale = Vector3.one;
            }
        }

        public static Image EnsureScanlines(Transform parent)
        {
            Image lines = EnsureChildImage(parent, "Scanlines", Scanlines, Color.white, first: true);
            lines.type = Image.Type.Tiled;
            lines.pixelsPerUnitMultiplier = 0.5f;
            return lines;
        }

        private static Image EnsureChildImage(Transform parent, string name, Sprite sprite, Color color, bool first)
        {
            Transform child = parent.Find(name);

            if (child == null)
            {
                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
                child = go.transform;
                child.SetParent(parent, false);
            }

            if (first)
                child.SetAsFirstSibling();
            else
                child.SetAsLastSibling();

            var rect = (RectTransform)child;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            child.GetComponent<LayoutElement>().ignoreLayout = true;

            var image = child.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static void StyleTitle(TMP_Text text, float size)
        {
            text.font = Bold;
            text.fontSharedMaterial = Bold.material;
            text.fontSize = size;
            text.fontStyle = FontStyles.UpperCase;
            text.characterSpacing = 8f;
            text.color = Text;
        }

        public static void StyleLabel(TMP_Text text, float size, Color color)
        {
            text.font = Regular;
            text.fontSharedMaterial = Regular.material;
            text.fontSize = size;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.color = color;
        }

        // Кнопка: сплошной прямоугольник, рамка, капс; HoverColor заменяется HoverFill (он сам красит и фон, и текст)
        public static void StyleButton(GameObject button, bool primary, float fontSize = 32f)
        {
            var image = button.GetComponent<Image>();
            image.sprite = Solid;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            Image frame = EnsureFrame(button.transform, Line);

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                text.font = Bold;
                text.fontSharedMaterial = Bold.material;
                text.fontSize = fontSize;
                text.fontStyle = FontStyles.UpperCase;
                text.characterSpacing = 6f;
                text.enableAutoSizing = false;
            }

            // Цвет кнопки ведёт HoverFill; стандартный тинт Button смешивался бы с ним
            var selectable = button.GetComponent<Selectable>();

            if (selectable != null)
                selectable.transition = Selectable.Transition.None;

            var hoverColor = button.GetComponent<HoverColor>();

            if (hoverColor != null)
                Object.DestroyImmediate(hoverColor, true);

            var fill = button.GetComponent<HoverFill>() ?? button.AddComponent<HoverFill>();
            var so = new SerializedObject(fill);
            so.FindProperty("_fill").objectReferenceValue = image;
            so.FindProperty("_frame").objectReferenceValue = frame;
            so.FindProperty("_text").objectReferenceValue = text;
            so.FindProperty("_isPrimary").boolValue = primary;
            so.ApplyModifiedPropertiesWithoutUndo();

            image.color = primary ? Alarm : new Color(0.055f, 0.06f, 0.075f, 0.6f);
            frame.enabled = !primary;

            if (text != null)
                text.color = primary ? Ground : Text;
        }

        public static void StyleSlider(Slider slider, float width)
        {
            var rect = (RectTransform)slider.transform;
            rect.localScale = Vector3.one;
            rect.sizeDelta = new Vector2(width, 28f);

            Transform background = slider.transform.Find("Background");

            if (background != null)
            {
                var image = background.GetComponent<Image>();
                image.sprite = Solid;
                image.type = Image.Type.Simple;
                image.color = Hex("2A2D34");
                var bgRect = (RectTransform)background;
                bgRect.anchorMin = new Vector2(0f, 0.5f);
                bgRect.anchorMax = new Vector2(1f, 0.5f);
                bgRect.sizeDelta = new Vector2(0f, 6f);
            }

            if (slider.fillRect != null)
            {
                var image = slider.fillRect.GetComponent<Image>();
                image.sprite = Solid;
                image.type = Image.Type.Simple;
                image.color = Alarm;
                var area = (RectTransform)slider.fillRect.parent;
                area.anchorMin = new Vector2(0f, 0.5f);
                area.anchorMax = new Vector2(1f, 0.5f);
                area.sizeDelta = new Vector2(-18f, 6f);
            }

            if (slider.handleRect != null)
            {
                var image = slider.handleRect.GetComponent<Image>();
                image.sprite = Solid;
                image.type = Image.Type.Simple;
                image.color = Text;
                slider.handleRect.sizeDelta = new Vector2(18f, 0f);
                var area = (RectTransform)slider.handleRect.parent;
                area.sizeDelta = new Vector2(-18f, 0f);
            }
        }

        [MenuItem("SibJam/UI/Apply Redesign To Prefabs")]
        public static void ApplyToPrefabs()
        {
            Edit("Assets/_Project/Prefabs/UI/GameButtonsPopUp.prefab", StylePause);
            Edit("Assets/_Project/Prefabs/UI/GameExitDialog.prefab", StyleExitDialog);
            Edit("Assets/_Project/Prefabs/UI/SettingsPopUp.prefab", StyleSettings);
            Edit("Assets/_Project/Prefabs/Audio/SliderBox.prefab", root => StyleSliderBox(root.transform));
            Edit("Assets/_Project/Prefabs/UI/MenuButton/StartGameButton.prefab", root => StyleButton(root, primary: false, fontSize: 28f));
            AssetDatabase.SaveAssets();
        }

        // HUD, бой с боссом, субтитры и экран поражения — объекты сцены Main, не префабы
        public static void StyleMainScene()
        {
            Transform hud = GameObject.Find("Core/Interface/HudCanvas").transform;
            Transform health = hud.Find("HealthBar");
            StylePanel(health.GetComponent<Image>(), Hex("0B0C0FB3"));
            StyleBarFill(health.Find("Fill").GetComponent<Image>(), Alarm);
            var healthText = health.Find("HealthText").GetComponent<TMP_Text>();
            StyleTitle(healthText, 32f);
            healthText.characterSpacing = 2f;
            healthText.alignment = TextAlignmentOptions.MidlineLeft;
            healthText.margin = new Vector4(14f, 0f, 0f, 0f);

            foreach (string panelName in new[] { "CamerasPanel", "WavesPanel" })
            {
                Transform panel = hud.Find(panelName);
                StylePanel(panel.GetComponent<Image>(), Hex("0B0C0FB3"));
                panel.Find("Frame").GetComponent<LayoutElement>().ignoreLayout = true;

                foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
                {
                    StyleTitle(text, text.fontSize);
                    text.characterSpacing = 2f;
                }
            }

            hud.Find("CamerasPanel/IconTemplate").GetComponent<Image>().color = Text;
            hud.Find("WavesPanel/EnemiesGroup/RobotIcon").GetComponent<Image>().color = Alarm;
            StyleLabel(hud.Find("WavesPanel/DelayGroup/DelayText").GetComponent<TMP_Text>(), 28f, Muted);
            Transform waveBar = hud.Find("WavesPanel/DelayGroup/Bar");
            StyleBarFill(waveBar.GetComponent<Image>(), Hex("2A2D34"));
            StyleBarFill(waveBar.Find("Fill").GetComponent<Image>(), Signal);

            Transform boss = GameObject.Find("Core/BossFight/BossHUD").transform;
            var counter = boss.Find("Counter").GetComponent<TMP_Text>();
            StyleTitle(counter, 28f);
            ((RectTransform)counter.transform).anchoredPosition = new Vector2(0f, -22f);
            ((RectTransform)counter.transform).sizeDelta = new Vector2(720f, 40f);
            Transform stagger = boss.Find("StaggerBar");
            StylePanel(stagger.GetComponent<Image>(), Hex("0B0C0FCC"));
            ((RectTransform)stagger).anchoredPosition = new Vector2(0f, -72f);
            ((RectTransform)stagger).sizeDelta = new Vector2(720f, 18f);
            StyleBarFill(stagger.Find("Fill").GetComponent<Image>(), Signal);
            var hint = boss.Find("Hint").GetComponent<TMP_Text>();
            StyleTitle(hint, 36f);
            hint.color = Signal;
            ((RectTransform)hint.transform).anchoredPosition = new Vector2(0f, -120f);

            Transform subtitles = GameObject.Find("Core/Systems/SubtitlesCanvas").transform;
            var subtitle = subtitles.Find("Subtitle").GetComponent<Image>();
            subtitle.sprite = Solid;
            subtitle.color = Hex("0B0C0FD1");
            StyleLabel(subtitles.Find("Subtitle/Text").GetComponent<TMP_Text>(), 34f, Text);
            StyleLabel(subtitles.Find("SkipHint").GetComponent<TMP_Text>(), 24f, Muted);

            Transform defeat = GameObject.Find("Core/Interface/UI/DefeatScreen/Fade").transform;
            defeat.GetComponent<Image>().color = Hex("280406C7");
            Transform window = defeat.Find("Window");
            var windowImage = window.GetComponent<Image>();
            windowImage.sprite = Solid;
            windowImage.type = Image.Type.Simple;
            windowImage.color = new Color(0f, 0f, 0f, 0f);
            ((RectTransform)window).sizeDelta = new Vector2(1100f, 460f);

            var defeatTitle = window.Find("Tittle").GetComponent<TMP_Text>();
            StyleTitle(defeatTitle, 150f);
            defeatTitle.characterSpacing = 6f;
            defeatTitle.text = "Ты пал";
            var defeatTitleRect = (RectTransform)defeatTitle.transform;
            defeatTitleRect.sizeDelta = new Vector2(1100f, 180f);
            defeatTitleRect.anchoredPosition = new Vector2(0f, 80f);

            GameObject retry = window.Find("RetryButton").gameObject;
            GameObject menu = window.Find("MenuButton").gameObject;
            StyleButton(retry, primary: true);
            StyleButton(menu, primary: false);
            retry.GetComponentInChildren<TMP_Text>(true).text = "С чекпоинта";
            ((RectTransform)retry.transform).anchoredPosition = new Vector2(-186f, -120f);
            ((RectTransform)menu.transform).anchoredPosition = new Vector2(186f, -120f);

            foreach (GameObject button in new[] { retry, menu })
            {
                ((RectTransform)button.transform).sizeDelta = new Vector2(340f, 72f);
                var label = (RectTransform)button.GetComponentInChildren<TMP_Text>(true).transform;
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.offsetMin = Vector2.zero;
                label.offsetMax = Vector2.zero;
            }
        }

        private static void StyleBarFill(Image image, Color color)
        {
            image.sprite = Solid;
            image.color = color;
        }

        // Экземпляры окон в сцене держат свои размеры и размер шрифта поверх префаба: сбрасываем, чтобы работала раскладка префаба
        public static void RevertLayoutOverridesInOpenScenes()
        {
            string[] prefabs =
            {
                "Assets/_Project/Prefabs/UI/GameButtonsPopUp.prefab", "Assets/_Project/Prefabs/UI/GameExitDialog.prefab",
                "Assets/_Project/Prefabs/UI/SettingsPopUp.prefab", "Assets/_Project/Prefabs/UI/MenuButton/StartGameButton.prefab",
            };

            foreach (RectTransform root in Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include))
            {
                if (!PrefabUtility.IsAnyPrefabInstanceRoot(root.gameObject))
                    continue;

                if (System.Array.IndexOf(prefabs, PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(root.gameObject)) < 0)
                    continue;

                foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
                    Revert(rect, rect == root ? new[] { "m_SizeDelta" } : new[] { "m_SizeDelta", "m_AnchorMin", "m_AnchorMax", "m_AnchoredPosition" });

                foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
                    Revert(text, new[] { "m_fontSize", "m_fontColor", "m_fontColor32", "m_fontStyle", "m_characterSpacing", "m_enableAutoSizing" });
            }
        }

        private static void Revert(Object target, string[] properties)
        {
            var so = new SerializedObject(target);

            foreach (string property in properties)
            {
                SerializedProperty prop = so.FindProperty(property);

                if (prop != null && prop.prefabOverride)
                    PrefabUtility.RevertPropertyOverride(prop, InteractionMode.AutomatedAction);
            }
        }

        private static void Edit(string path, System.Action<GameObject> style)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);

            try
            {
                style(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void StylePause(GameObject root)
        {
            StylePanel(root.GetComponent<Image>(), Panel);
            ((RectTransform)root.transform).sizeDelta = new Vector2(660f, 0f);
            var layout = root.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 40, 48);
            layout.spacing = 12f;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;

            var fitter = root.GetComponent<ContentSizeFitter>();

            if (fitter != null)
                fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            foreach (Transform child in root.transform)
            {
                if (child.name == "Frame")
                    continue;

                var rect = (RectTransform)child;
                rect.sizeDelta = new Vector2(564f, child.GetComponent<Button>() != null ? 72f : 90f);
            }

            var title = root.transform.Find("PauseText (TMP)").GetComponent<TMP_Text>();
            StyleTitle(title, 64f);
            title.alignment = TextAlignmentOptions.Left;

            StyleButton(root.transform.Find("ContinueButton").gameObject, primary: true);
            StyleButton(root.transform.Find("SettingsButton").gameObject, primary: false);
            StyleButton(root.transform.Find("MenuOpenerButton").gameObject, primary: false);
            StyleButton(root.transform.Find("GameCloserButton").gameObject, primary: false);
        }

        private static void StyleExitDialog(GameObject root)
        {
            StylePanel(root.GetComponent<Image>(), Hex("0E0F13F7"), Alarm);
            ((RectTransform)root.transform).sizeDelta = new Vector2(800f, 380f);

            var title = root.transform.Find("Tittle").GetComponent<TMP_Text>();
            StyleTitle(title, 50f);
            title.fontStyle = FontStyles.Normal;
            title.characterSpacing = 0f;
            title.text = "Точно хочешь выйти из игры?";
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(700f, 140f);
            titleRect.anchoredPosition = new Vector2(0f, 70f);

            GameObject accept = root.transform.Find("AcceptButton").gameObject;
            GameObject cancel = root.transform.Find("CancelButton").gameObject;
            StyleButton(accept, primary: true);
            StyleButton(cancel, primary: false);
            accept.GetComponentInChildren<TMP_Text>(true).text = "Выйти";
            cancel.GetComponentInChildren<TMP_Text>(true).text = "Остаться";

            ((RectTransform)accept.transform).anchoredPosition = new Vector2(176f, -110f);
            ((RectTransform)cancel.transform).anchoredPosition = new Vector2(-176f, -110f);

            foreach (GameObject button in new[] { accept, cancel })
            {
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(340f, 72f);
                var label = (RectTransform)button.GetComponentInChildren<TMP_Text>(true).transform;
                label.anchorMin = Vector2.zero;
                label.anchorMax = Vector2.one;
                label.offsetMin = Vector2.zero;
                label.offsetMax = Vector2.zero;
            }
        }

        private static void StyleSettings(GameObject root)
        {
            StylePanel(root.GetComponent<Image>(), Hex("0E0F13F5"));
            ((RectTransform)root.transform).sizeDelta = new Vector2(1000f, 800f);

            var title = root.transform.Find("Tittle").GetComponent<TMP_Text>();
            StyleTitle(title, 64f);
            ((RectTransform)title.transform).sizeDelta = new Vector2(800f, 80f);

            Transform vertical = root.transform.Find("Vertical");
            var layout = vertical.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 26f;

            foreach (Transform box in vertical)
                StyleSliderBox(box);

            Transform close = root.transform.Find("ClosePopUpButton");
            var closeImage = close.GetComponent<Image>();
            closeImage.sprite = Solid;
            closeImage.color = new Color(0f, 0f, 0f, 0f);
            EnsureFrame(close, Line);
            ((RectTransform)close).sizeDelta = new Vector2(64f, 64f);
            ((RectTransform)close).anchoredPosition = new Vector2(-28f, -28f);
            Transform icon = close.Find("Icon");

            if (icon != null)
            {
                icon.GetComponent<Image>().color = Text;
                ((RectTransform)icon).sizeDelta = new Vector2(24f, 24f);
            }
        }

        // Подпись над слайдером Regular, сам слайдер — тонкая полоса с квадратным ползунком
        private static void StyleSliderBox(Transform box)
        {
            ((RectTransform)box).sizeDelta = new Vector2(720f, 110f);
            TMP_Text label = box.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                StyleLabel(label, 32f, Text);
                var labelRect = (RectTransform)label.transform;
                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.sizeDelta = new Vector2(0f, 54f);
            }

            Slider slider = box.GetComponentInChildren<Slider>(true);

            if (slider != null)
                StyleSlider(slider, 680f);
        }
    }
}
