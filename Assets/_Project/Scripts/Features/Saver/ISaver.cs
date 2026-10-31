namespace SibJam
{
    public interface ISaver
    {
        public SavesData Data { get; }

        public void Save();
    }
}