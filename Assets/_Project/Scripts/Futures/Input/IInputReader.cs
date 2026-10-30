using UnityEngine;

namespace SibJam
{
    public interface IInputReader
    {
        public Vector2 MoveDirection { get; }

        public bool IsSprinting { get; }

        public Vector2 LookDirection { get; }

        public bool IsJumpPressed { get; }
    }
}