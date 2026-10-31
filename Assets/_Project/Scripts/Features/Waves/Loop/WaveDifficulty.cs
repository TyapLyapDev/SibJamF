namespace SibJam.Waves
{
    // Усиление волны: камеры «учатся» на бою, каждая следующая волна сильнее
    public readonly struct WaveDifficulty
    {
        public static readonly WaveDifficulty Default = new(0, 1f);

        public WaveDifficulty(int extraEnemiesPerEntry, float statsMultiplier)
        {
            ExtraEnemiesPerEntry = extraEnemiesPerEntry;
            StatsMultiplier = statsMultiplier;
        }

        public int ExtraEnemiesPerEntry { get; }

        public float StatsMultiplier { get; }
    }
}
