using System;

namespace SibJam
{
    [Serializable]
    public class SavesData
    {
        public float MusicVolume = 0.5f;
        public float SfxVolume = 0.75f;
    }

    public class Saver : ISaver
    {
        public SavesData Data { get; private set; }

        public void Save()
        {
            throw new NotImplementedException();
        }
    }

    public interface ISaver
    {
        public SavesData Data { get; }

        public void Save();
    }
}