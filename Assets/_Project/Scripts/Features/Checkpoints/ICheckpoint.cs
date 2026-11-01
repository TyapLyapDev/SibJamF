namespace SibJam.Checkpoints
{
    public interface ICheckpoint
    {
        public bool HasSaved { get; }

        public int LocationIndex { get; }

        // Следующая загрузка игровой сцены начнётся с чекпоинта, а не с катсцены
        public bool IsContinueRequested { get; }

        public void RequestContinue();

        public void RequestNewGame();

        public void ConsumeRequest();

        public void Save(int locationIndex);

        public void Clear();
    }
}
