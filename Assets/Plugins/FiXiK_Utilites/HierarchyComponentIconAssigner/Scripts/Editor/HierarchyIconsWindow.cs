#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public class HierarchyIconsWindow : EditorWindow
    {
        private SerializedObject _serializedObject;
        private Vector2 _scrollPos;

        private void OnEnable()
        {
            if (ConfigLoader.Config != null)
            {
                _serializedObject = new(ConfigLoader.Config);
                ConfigLoader.Config.Changed += OnChangesSettings;
            }
        }

        private void OnDisable()
        {
            if (ConfigLoader.Config != null)
                ConfigLoader.Config.Changed -= OnChangesSettings;
        }

        private void OnChangesSettings()
        {
            HierarchyIconDrawer.Reload();
            _serializedObject.Update();
            Repaint();
            EditorUtility.SetDirty(ConfigLoader.Config);
        }

        private void OnGUI()
        {
            GUILayout.Space(Constants.Window.Space);

            if (ConfigLoader.Config == null || _serializedObject == null)
            {
                EditorGUILayout.HelpBox(Constants.Window.MessageNoConfig, MessageType.Error);

                return;
            }

            _serializedObject.Update();

            EditorGUILayout.PropertyField(_serializedObject.FindProperty(Config.EnablerPropertyName));

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty(Config.ComponentIconListPropertyName), true);
            EditorGUILayout.EndScrollView();

            if (_serializedObject.ApplyModifiedProperties())
                EditorUtility.SetDirty(ConfigLoader.Config);

            DrawVersionLabel();
        }

        [MenuItem(Constants.MenuPath + "/" + Constants.Window.Title)]
        public static void ShowWindow()
        {
            HierarchyIconsWindow window = GetWindow<HierarchyIconsWindow>(Constants.Window.Title);
            window.minSize = Constants.Window.Size;
            window.Show();
        }

        public static void ShowWelcomeDialog()
        {
            CustomDialog.ShowDialog(
                Constants.WelcomeDialog.Title,
                Constants.WelcomeDialog.Message,
                Constants.WelcomeDialog.ButtonOk,
                Constants.WelcomeDialog.Size,
                Constants.WelcomeDialog.ButtonSize,
                Constants.WelcomeDialog.Space);
        }

        public static void ShowAboutDialog()
        {
            CustomDialog.ShowDialog(
                Constants.AboutDialog.Title,
                Constants.AboutDialog.Message,
                Constants.AboutDialog.ButtonOk,
                Constants.AboutDialog.ButtonTelegram,
                OpenLink,
                Constants.AboutDialog.Size,
                Constants.AboutDialog.ButtonSize,
                Constants.AboutDialog.SecondaryButtonSize,
                Constants.AboutDialog.Space);
        }

        private static void OpenLink() =>
            Application.OpenURL(Constants.AboutDialog.TelegramUrl);

        private void DrawVersionLabel()
        {
            float width = Constants.ContextMenu.IconSize.x;
            float height = Constants.ContextMenu.IconSize.y;

            float indentX = position.width - width;
            float indentY = 0;

            Rect headerRect = new(indentX, indentY, width, height);
            GUILayout.BeginArea(headerRect);
            EditorGUILayout.BeginHorizontal();


            if (GUILayout.Button(
                EditorGUIUtility.IconContent(Constants.ContextMenu.IconName),
                EditorStyles.toolbarButton,
                GUILayout.Width(width), GUILayout.Height(height)))
            {
                ShowHeaderMenu();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void ShowHeaderMenu()
        {
            GenericMenu menu = new();
            menu.AddItem(new GUIContent(Constants.ContextMenu.AboutItem), false, ShowAboutDialog);
            menu.AddItem(new GUIContent(Constants.ContextMenu.WelcomeItem), false, ShowWelcomeDialog);
            menu.ShowAsContext();
        }
    }
}
#endif