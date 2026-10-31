using System;
using System.IO;
using UnityEngine;

namespace SibJam
{
    public class Saver : ISaver
    {
        private const string FileName = "saves.json";

        private static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

        public SavesData Data { get; private set; }

        public Saver()
        {
            Load();
        }

        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, prettyPrint: true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[Saver] Failed to save to '{SavePath}': {exception}");
            }
        }

        private void Load()
        {
            if (File.Exists(SavePath) == false)
            {
                Data = new SavesData();

                return;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                Data = JsonUtility.FromJson<SavesData>(json) ?? new SavesData();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Saver] Failed to load from '{SavePath}', using defaults: {exception}");
                Data = new SavesData();
            }
        }
    }
}