#if UNITY_EDITOR
using UnityEditor;

namespace FiXiK.SceneBrowserWindow.Editor
{
    public partial class PackageImporter
    {
        private const string EditorPrefsKey = "FiXiK.SceneBrowserWindow";

        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            bool hasBeenImported = EditorPrefs.GetBool(EditorPrefsKey, false);

            if (hasBeenImported == false)
            {
                EditorApplication.delayCall += () =>
                {
                    ShowWelcomeWindow();
                    EditorPrefs.SetBool(EditorPrefsKey, true);
                };
            }
        }

        private static void ShowWelcomeWindow()
        {
            SceneBrowserWindow.ShowWindow();

            string tittle = "Scene Browser Window";
            string message = "Пакет успешно установлен!\n\n" +
                "Окно работы со сценами открыто автоматически.\n" +
                $"Вы всегда можете открыть его через меню: \n" +
                $"\"{SceneBrowserConstants.MenuPath} -> {SceneBrowserConstants.MenuName}\"";

            string author = "С уважением, ваш FiXiK";
            string buttonCancel = "От души, бро!";

            EditorUtility.DisplayDialog(tittle, message + "\n\n" + author, buttonCancel);
        }
    }
}
#endif