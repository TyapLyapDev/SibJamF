using UnityEngine;

namespace SibJam
{
    public interface IGroundDetector
    {
        // Пол под ногами с уклоном не круче slopeLimit (стены не считаются)
        public bool HasGround();

        // Земля или голова врага — от неё можно оттолкнуться, но стоять на ней нельзя
        public bool HasJumpSupport();

        public bool TryGetGroundNormal(out Vector3 normal);

        // Под ногами склон круче slopeLimit — по нему нужно сползать
        public bool TryGetSteepSlopeNormal(out Vector3 normal);
    }
}
