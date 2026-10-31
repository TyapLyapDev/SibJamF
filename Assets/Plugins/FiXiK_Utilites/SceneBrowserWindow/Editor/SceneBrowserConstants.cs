#if UNITY_EDITOR

namespace FiXiK.SceneBrowserWindow.Editor
{
    public static class SceneBrowserConstants
    {
        public const string MenuPath = "Tools/";
        public const string MenuName = "Список сцен";
        public const string WindowName = "Список сцен";
        public const string FolderName = "Assets";
        public const string SceneType = "t:Scene";
        public const string Tittle = "Избранное:";

        public const string SceneIconName = "SceneAsset Icon";
        public const string FolderIconName = "d_Folder Icon";
        public const string OpenEyeIconName = "d_scenevis_hidden";
        public const string ClosedEyeIconName = "d_scenevis_visible";

        public const string FolderTextureTooltip = "Показать в проекте";
        public const string OpenEyeTextureTooltip = "Скрыть из избранного";
        public const string ClosedEyeTextureTooltip = "Перенести в избранное";
        public const string HiddenSceneBlockName = "Остальные сцены";
        public const string Version = "1.0.4";

        public const int LabelHeight = 25;
    }
}
#endif