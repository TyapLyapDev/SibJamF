#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    [CustomPropertyDrawer(typeof(ComponentIcon))]
    public class ComponentIconDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            SerializedProperty iconProperty = property.FindPropertyRelative(ComponentIcon.IconPropertyName);
            SerializedProperty typeNameProperty = property.FindPropertyRelative(ComponentIcon.TypePropertyName);

            EditorGUI.BeginProperty(rect, label, property);

            int size = Constants.Icon.TextureSize;
            int spaceBetweenFields = Constants.Icon.SpaceBetweenFields;

            Rect textureRect = new(rect.x, rect.y, size, size);
            DrawTextureField(textureRect, iconProperty);

            float width = rect.width - size - spaceBetweenFields;
            float buttonHeight = EditorGUIUtility.singleLineHeight;
            float xPosition = rect.x + size + spaceBetweenFields;
            float yPosition = rect.y + (size - buttonHeight) * Constants.Icon.HalfValue;

            Rect componentRect = new(xPosition, yPosition, width, buttonHeight);

            Type currentType = Type.GetType(typeNameProperty.stringValue);
            GUIContent buttonContent = new(currentType != null ? currentType.Name : Constants.Icon.NoComponentSelectedText);
            DrawComponentField(componentRect, buttonContent, typeNameProperty);

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            Constants.Icon.TextureSize;

        private void DrawTextureField(Rect textureRect, SerializedProperty iconProperty)
        {
            Texture2D currentTexture = (Texture2D)iconProperty.objectReferenceValue;
            int controlID = GUIUtility.GetControlID(FocusType.Passive);

            if (currentTexture != null)
                GUI.DrawTexture(textureRect, currentTexture, ScaleMode.ScaleToFit);
            else
                EditorGUI.DrawRect(textureRect, Constants.Icon.NoneTextureColor);

            if (Event.current.type == EventType.MouseDown && textureRect.Contains(Event.current.mousePosition))
            {
                EditorGUIUtility.ShowObjectPicker<Texture2D>(
                    currentTexture,
                    false,
                    Constants.Icon.SearchFilter,
                    controlID);

                Event.current.Use();
            }

            if (Event.current.commandName == Constants.Icon.CommandObjectSelectorUpdated
                && EditorGUIUtility.GetObjectPickerControlID() == controlID)
            {
                iconProperty.objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
                iconProperty.serializedObject.ApplyModifiedProperties();

                if (Event.current.type != EventType.Layout)
                    Event.current.Use();
            }
        }

        private void DrawComponentField(Rect rect, GUIContent content, SerializedProperty typeNameProperty)
        {
            if (EditorGUI.DropdownButton(rect, content, FocusType.Keyboard))
            {
                ComponentTypeDropdown dropdown = new(new(), selectedType =>
                {
                    typeNameProperty.stringValue = selectedType.AssemblyQualifiedName;
                    typeNameProperty.serializedObject.ApplyModifiedProperties();
                });

                dropdown.Show(rect);
            }
        }
    }
}
#endif