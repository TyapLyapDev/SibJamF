using UnityEngine;

namespace SibJam
{
    public interface IPlayerInputReader
    {
        public Vector2 MoveDirection { get; }

        public bool IsSprinting { get; }

        public Vector2 LookDirection { get; }

        public bool IsJumpPressed { get; }

        public void Enable();

        public void Disable();
    }
}