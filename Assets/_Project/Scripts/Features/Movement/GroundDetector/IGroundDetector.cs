using UnityEngine;

namespace SibJam
{
    public interface IGroundDetector
    {
        public bool HasGround();

        public bool TryGetGroundNormal(out Vector3 normal);
    }
}