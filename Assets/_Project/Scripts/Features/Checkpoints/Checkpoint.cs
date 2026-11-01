using System;

namespace SibJam.Checkpoints
{
    // Чекпоинт — начало локации. Индекс лежит в сейве, запрос «продолжить» живёт между сценами в ProjectLifetimeScope
    public class Checkpoint : ICheckpoint
    {
        private const int None = -1;

        private readonly ISaver _saver;

        public Checkpoint(ISaver saver)
        {
            _saver = saver ?? throw new ArgumentNullException(nameof(saver));
        }

        public bool HasSaved => _saver.Data.CheckpointLocation > None;

        public int LocationIndex => _saver.Data.CheckpointLocation;

        public bool IsContinueRequested { get; private set; }

        public void RequestContinue() => IsContinueRequested = HasSaved;

        public void RequestNewGame()
        {
            IsContinueRequested = false;
            Clear();
        }

        public void ConsumeRequest() => IsContinueRequested = false;

        public void Save(int locationIndex)
        {
            if (_saver.Data.CheckpointLocation == locationIndex) return;

            _saver.Data.CheckpointLocation = locationIndex;
            _saver.Save();
        }

        public void Clear()
        {
            if (!HasSaved) return;

            _saver.Data.CheckpointLocation = None;
            _saver.Save();
        }
    }
}
