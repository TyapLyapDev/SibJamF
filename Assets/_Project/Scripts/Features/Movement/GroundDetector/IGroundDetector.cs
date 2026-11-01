using UnityEngine;

namespace SibJam
{
    public interface IGroundDetector
    {
        public bool HasGround();

        // Земля или голова врага — от неё можно оттолкнуться, но стоять на ней нельзя
        public bool HasJumpSupport();

        public bool TryGetGroundNormal(out Vector3 normal);
    }
}