#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FiXiK.SceneBrowserWindow.Editor
{
    public class SceneBrowserConfig : ScriptableObject
    {
        public List<string> HiddenScenes = new();

        private const string ConfigPath = "Assets/Plugins/FiXiK_Utilites/SceneBrowserWindow/Config.asset";

        public static SceneBrowserConfig LoadOrCreate()
        {
            SceneBrowserConfig config = AssetDatabase.LoadAssetAtPath<SceneBrowserConfig>(ConfigPath);

            if (config == null)
            {
                config = CreateInstance<SceneBrowserConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
                AssetDatabase.SaveAssets();
            }

            return config;
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif