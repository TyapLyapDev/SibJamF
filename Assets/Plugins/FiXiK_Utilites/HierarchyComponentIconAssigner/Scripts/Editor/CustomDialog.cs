#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public class CustomDialog : EditorWindow
    {
        private static GUIStyle s_RichWordWrappedLabel;

        private string _message;
        private string _buttonText;
        private string _secondaryButtonText;
        private Action _secondaryButtonAction;
        private int _space;
        private Vector2 _buttonSize;
        private Vector2 _secondaryButtonSize;
        private bool _closeOnSecondaryClick;
        private Action _onConfirm;

        private static GUIStyle RichWordWrappedLabel
        {
            get
            {
                s_RichWordWrappedLabel ??= new GUIStyle(EditorStyles.wordWrappedLabel)
                {
                    richText = true
                };

                return s_RichWordWrappedLabel;
            }
        }

        public static void ShowDialog(
            string title,
            string message,
            string buttonText,
            Vector2 size,
            Vector2 buttonSize,
            int space,
            System.Action onConfirm = null)
        {
            CustomDialog window = GetWindow<CustomDialog>(true, title, true);
            window._message = message;
            window._buttonText = buttonText;
            window._onConfirm = onConfirm;
            window.minSize = size;
            window.maxSize = size;
            window._buttonSize = buttonSize;
            window._space = space;
            window.ShowUtility();
        }

        public static void ShowDialog(
            string title,
            string message,
            string primaryButtonText,
            string secondaryButtonText,
            Action secondaryButtonAction,
            Vector2 size,
            Vector2 buttonSize,
            Vector2 secondButtonSize,
            int space,
            Action onPrimaryConfirm = null,
            bool closeOnSecondaryClick = false)
        {
            CustomDialog window = GetWindow<CustomDialog>(true, title, true);
            window._message = message;
            window._buttonText = primaryButtonText;
            window._secondaryButtonText = secondaryButtonText;
            window._secondaryButtonAction = secondaryButtonAction;
            window.minSize = size;
            window.maxSize = size;
            window._buttonSize = buttonSize;
            window._secondaryButtonSize = secondButtonSize;
            window._space = space;
            window._onConfirm = onPrimaryConfirm;
            window._closeOnSecondaryClick = closeOnSecondaryClick;
            window.ShowUtility();
        }

        private void OnGUI()
        {
            GUILayout.Space(_space);
            EditorGUILayout.LabelField(_message, RichWordWrappedLabel, GUILayout.ExpandHeight(true));
            GUILayout.Space(_space);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            DrawFirstButton();

            if (string.IsNullOrEmpty(_secondaryButtonText) == false && _secondaryButtonAction != null)
            {
                GUILayout.Space(_space);
                DrawSecondButton();
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(_space);
        }

        private void DrawFirstButton()
        {
            if (GUILayout.Button(
                _buttonText,
                GUILayout.Width(_buttonSize.x), GUILayout.Height(_buttonSize.y)))
            {
                _onConfirm?.Invoke();
                Close();
            }
        }

        private void DrawSecondButton()
        {
            if (GUILayout.Button(_secondaryButtonText, GUILayout.Width(_secondaryButtonSize.x), GUILayout.Height(_secondaryButtonSize.y)))
            {
                _secondaryButtonAction?.Invoke();

                if (_closeOnSecondaryClick)
                    Close();
            }
        }
    }
}
#endif