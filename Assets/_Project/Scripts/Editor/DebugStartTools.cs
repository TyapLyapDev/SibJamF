using SibJam.Boss;
using UnityEditor;

namespace SibJam.Editor
{
    // Меню SibJam/Debug: переключатель старта сразу у босса, галочка показывает, включён ли он
    public static class DebugStartTools
    {
        private const string StartAtBoss = "SibJam/Debug/Start At Boss";

        [MenuItem(StartAtBoss, priority = 0)]
        private static void ToggleStartAtBoss() =>
            EditorPrefs.SetBool(BossDebugStart.PrefKey, !EditorPrefs.GetBool(BossDebugStart.PrefKey, false));

        [MenuItem(StartAtBoss, true)]
        private static bool ValidateStartAtBoss()
        {
            Menu.SetChecked(StartAtBoss, EditorPrefs.GetBool(BossDebugStart.PrefKey, false));
            return true;
        }
    }
}
