using UnityEditor;
using UnityEngine;

namespace SibJam.EditorTools
{
    // Кадр катсцены на ползунке окна катсцен, с субтитром поверх. Отдельное окно — его можно положить рядом с Game
    public class CutscenePreviewWindow : EditorWindow
    {
        private static CutscenePreviewWindow _open;

        [MenuItem("Window/SibJam/Превью катсцены")]
        public static void Open()
        {
            var window = GetWindow<CutscenePreviewWindow>();
            window.titleContent = new GUIContent("Превью катсцены", EditorGUIUtility.IconContent("Camera Icon").image);
            window.minSize = new Vector2(240f, 160f);
            window.Show();
        }

        public static void RepaintOpen()
        {
            if (_open != null)
                _open.Repaint();
        }

        private void OnEnable() => _open = this;

        private void OnDisable()
        {
            if (_open == this)
                _open = null;
        }

        private void OnGUI()
        {
            CutsceneWindow cutscenes = CutsceneWindow.Current;

            if (cutscenes == null)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Откройте окно катсцен: Window → SibJam → Катсцены.", EditorStyles.centeredGreyMiniLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Открыть", GUILayout.Width(100)))
                        CutsceneWindow.Open();

                    GUILayout.FlexibleSpace();
                }

                GUILayout.FlexibleSpace();
                return;
            }

            // Кадр 16:9, вписанный в окно
            float width = Mathf.Min(position.width - 8f, (position.height - 8f) * 16f / 9f);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                using (new EditorGUILayout.VerticalScope(GUILayout.Width(width)))
                {
                    GUILayout.Space(4f);
                    cutscenes.DrawFrame(width);
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
