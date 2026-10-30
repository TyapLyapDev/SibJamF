#if UNITY_EDITOR
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public static class Constants
    {
        public const string Version = "2.2.1";
        public const string MenuPath = "Tools";

        public class Window
        {
            public const string Title = "Иконки в иерархии сцены";
            public const string MessageNoConfig = "Не удалось создать или найти конфиг с настройками";
            public const float Space = 10;

            public static readonly Vector2 Size = new(300, 200);
        }

        public static class Settings
        {
            public const string TooltipIconRenderingSwitch = "Включить/выключить отрисовку иконок";
            public const string TooltipChoosingIconAndComponent = "Выбери иконку, компонент (или интерфейс) и радуйся =)";
        }

        public static class ContextMenu
        {
            public const string IconName = "_Menu";
            public const string AboutItem = "О программе";
            public const string WelcomeItem = "Показать приветственный диалог";

            public static readonly Vector2 IconSize = new(20, 20);
        }

        public static class Icon
        {
            public const string NoComponentSelectedText = "Компонент не выбран";
            public const string CommandObjectSelectorUpdated = "ObjectSelectorUpdated";
            public const string SearchFilter = "HierarchyIcon";
            public const float HalfValue = 0.5f;
            public const int TextureSize = 32;
            public const int SpaceBetweenFields = 4;
            public const int VerticalPadding = 4;

            public static readonly Color NoneTextureColor = new(0.5f, 0.5f, 0.5f, 0.3f);
        }

        public static class HierarchyIcon
        {
            public const string MessageNoDataReceived = "Настройки не загружены";
            public const string MessageComponentIsNull = "Компонент null";

            public static readonly Vector2 IconSize = new(16, 16);
        }

        public static class Component
        {
            public const string Title = "Выберите компонент";
            public const string NoneNamespacesText = "Без пространства имён";
            public const string MessageTypeWithIdNotFound = "Выбран элемент без типа (возможно, группа): {0}";
        }

        public static class AboutDialog
        {
            public const string Title = "О программе";
            public const string Message =
                "Иконки в иерархии сцены\n" +
                "<color=yellow>Версия: " + Version + "</color>\n\n" +
                "Пакет позволяет назначать иконки компонентам прямо в окне иерархии.\n\n" +
                "С уважением, ваш FiXiK";

            public const string ButtonOk = "Ok";
            public const string ButtonTelegram = "Мой телеграм";
            public static readonly string TelegramUrl = "https://t.me/VL_dogs";

            public const int Space = 10;

            public static readonly Vector2 Size = new(400, 170);
            public static readonly Vector2 ButtonSize = new(50, 25);
            public static readonly Vector2 SecondaryButtonSize = new(110, 25);
        }

        public static class WelcomeDialog
        {
            public const string Title = "Hierarchy Component Icon Assigner";
            public const string Message =
                "<color=green>Пакет успешно установлен!</color>\n\n" +
                "Окно настройки иконок открыто автоматически.\n" +
                "Вы всегда можете открыть его через меню: \n" +
                "\"" + MenuPath + " -> " + Window.Title + "\"\n\n" +
                "С уважением, ваш FiXiK";

            public const string ButtonOk = "От души, бро!";

            public const int Space = 10;

            public static readonly Vector2 Size = new(400, 170);
            public static readonly Vector2 ButtonSize = new(120, 25);
        }

        public static class Config
        {
            public const string FallbackPath = "Assets/Plugins/FiXiK_Utilites/HierarchyComponentIconAssigner/Config.asset";
            public const string RootFolder = "Assets";
            public const string FileName = "Config.asset";
            public const string MessageMultipleFilesFound = "Найдено несколько файлов настроек. Используется первый: {0}";
            public const string MessageConfigNotFound = "Не удалось найти конфиг";
            public const string MessageWillUseDefaultPath = "Будет использован путь по умолчанию: {0}";
            public const string MessageFailedCreateConfig = "Не удалось создать файл настроек {0} по пути {1}";
        }

        public static class Prefs
        {
            public const string Key = "FiXiK.HierarchyComponentIconAssigner_Imported";
        }
    }
}
#endif