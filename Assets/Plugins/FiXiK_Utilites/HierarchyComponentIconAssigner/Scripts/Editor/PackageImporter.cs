#if UNITY_EDITOR
using UnityEditor;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public class PackageImporter
    {
        [InitializeOnLoadMethod]
        private static void InitializeOnLoad()
        {
            string key = Constants.Prefs.Key;
            bool hasBeenImported = EditorPrefs.GetBool(key, false);

            if (hasBeenImported == false)
            {
                EditorApplication.delayCall += () =>
                {
                    HierarchyIconsWindow.ShowWelcomeDialog();
                    EditorPrefs.SetBool(key, true);
                };
            }
        }
    }
}
#endif