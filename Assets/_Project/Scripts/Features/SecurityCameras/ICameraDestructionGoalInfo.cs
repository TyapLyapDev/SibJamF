using System;

namespace SibJam.SecurityCameras
{
    public interface ICameraDestructionGoalInfo
    {
        public event Action Changed;

        public int TotalCount { get; }

        public int BrokenCount { get; }

        public int LeftCount { get; }

        public bool IsCompleted { get; }
    }
}
