using System.IO;
using UnityEditor;
using UnityEngine;

namespace SibJam.Editor
{
    // Меню SibJam/Save Data: сброс сейва целиком или только чекпоинта, открыть папку с файлом
    public static class SaveDataTools
    {
        private const string Menu = "SibJam/Save Data/";
        private const string FileName = "saves.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        [MenuItem(Menu + "Clear All", priority = 0)]
        private static void ClearAll()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log($"[SaveDataTools] Nothing to clear: '{SavePath}' does not exist");
                return;
            }

            if (!EditorUtility.DisplayDialog("Очистить сейв", "Удалить saves.json? Сбросятся настройки звука, чувствительность и чекпоинт.", "Удалить", "Отмена"))
                return;

            File.Delete(SavePath);
            Debug.Log($"[SaveDataTools] Deleted '{SavePath}'");
            WarnIfPlaying();
        }

        [MenuItem(Menu + "Clear Checkpoint", priority = 1)]
        private static void ClearCheckpoint()
        {
            if (!File.Exists(SavePath))
            {
                Debug.Log($"[SaveDataTools] Nothing to clear: '{SavePath}' does not exist");
                return;
            }

            SavesData data = JsonUtility.FromJson<SavesData>(File.ReadAllText(SavePath)) ?? new SavesData();
            data.CheckpointLocation = new SavesData().CheckpointLocation;
            File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
            Debug.Log("[SaveDataTools] Checkpoint cleared, settings kept");
            WarnIfPlaying();
        }

        [MenuItem(Menu + "Open Folder", priority = 20)]
        private static void OpenFolder() => EditorUtility.RevealInFinder(File.Exists(SavePath) ? SavePath : Application.persistentDataPath);

        [MenuItem(Menu + "Log Contents", priority = 21)]
        private static void LogContents() =>
            Debug.Log(File.Exists(SavePath) ? $"[SaveDataTools] {SavePath}\n{File.ReadAllText(SavePath)}" : $"[SaveDataTools] '{SavePath}' does not exist");

        // Saver читает файл один раз при старте, поэтому в Play Mode в памяти остаются старые данные и перезапишут файл при следующем Save
        private static void WarnIfPlaying()
        {
            if (Application.isPlaying)
                Debug.LogWarning("[SaveDataTools] Play Mode is running: Saver still holds old data in memory. Restart Play Mode");
        }
    }
}
