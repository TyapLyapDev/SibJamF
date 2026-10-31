#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public static class ConfigLoader
    {
        private static Config _config;

        public static Config Config => Load();

        public static Config Load()
        {
            if (_config != null)
                return _config;

            _config = AssetDatabase.LoadAssetAtPath<Config>(Constants.Config.FallbackPath);

            if (_config != null)
                return _config;

            string[] guids = AssetDatabase.FindAssets(
                $"t:{nameof(HierarchyComponentIconAssigner.Config)}",
                new[] { Constants.Config.RootFolder });

            if (guids.Length == 0)
                return CreateConfig();

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            _config = AssetDatabase.LoadAssetAtPath<Config>(assetPath);

            if (guids.Length > 1)
                Debug.LogWarning(string.Format(Constants.Config.MessageMultipleFilesFound, assetPath));

            return _config;
        }

        private static string GetConfigFolderPath()
        {
            string[] guids = AssetDatabase.FindAssets($"{nameof(ConfigLoader)} t:script");

            if (guids.Length > 0)
            {
                string scriptPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                string folder = Path.GetDirectoryName(scriptPath);
                folder = Path.GetDirectoryName(folder);
                folder = Path.GetDirectoryName(folder);

                if (string.IsNullOrEmpty(folder) == false && AssetDatabase.IsValidFolder(folder))
                    return folder.Replace('\\', '/');
            }

            Debug.Log(Constants.Config.MessageConfigNotFound);
            Debug.Log(string.Format(Constants.Config.MessageWillUseDefaultPath, Constants.Config.FallbackPath));

            return Path.GetDirectoryName(Constants.Config.FallbackPath).Replace('\\', '/');
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
            string folderName = Path.GetFileName(folderPath);

            EnsureFolderExists(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static Config CreateConfig()
        {
            string folderPath = GetConfigFolderPath();
            string settingsPath = Path.Combine(folderPath, Constants.Config.FileName).Replace('\\', '/');

            EnsureFolderExists(folderPath);

            Config instance = ScriptableObject.CreateInstance<Config>();
            AssetDatabase.CreateAsset(instance, settingsPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _config = AssetDatabase.LoadAssetAtPath<Config>(settingsPath);

            if (_config == null)
            {
                Debug.LogError(string.Format(
                    Constants.Config.MessageFailedCreateConfig,
                    nameof(HierarchyComponentIconAssigner.Config),
                    settingsPath));
            }

            return _config;
        }
    }
}
#endif