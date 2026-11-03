using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Имя и цвет типа элемента в редакторе таймлайна: в меню «Добавить» и на полосе
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class CutsceneActionAttribute : Attribute
    {
        public CutsceneActionAttribute(string name, string htmlColor)
        {
            Name = name;
            Color = ColorUtility.TryParseHtmlString(htmlColor, out Color color) ? color : UnityEngine.Color.gray;
        }

        public string Name { get; }
        public Color Color { get; }
    }

    // Подпись поля элемента в редакторе таймлайна
    [AttributeUsage(AttributeTargets.Field)]
    public class CutsceneLabelAttribute : Attribute
    {
        public CutsceneLabelAttribute(string text, string tooltip = null)
        {
            Text = text;
            Tooltip = tooltip;
        }

        public string Text { get; }
        public string Tooltip { get; }
    }

    // Главное поле элемента: его правят прямо в строке раскрытой дорожки окна катсцен. Без атрибута — первое поле
    [AttributeUsage(AttributeTargets.Field)]
    public class CutsceneMainFieldAttribute : Attribute
    {
    }
}
