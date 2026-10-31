using System;
using UnityEngine;

namespace SibJam.SecurityCameras
{
    public class SecurityCamera : MonoBehaviour
    {
        [SerializeField, Min(1)] private int _hitsToBreak = 3;

        private int _hitsTaken;

        public event Action<SecurityCamera> Broken;

        public bool IsBroken { get; private set; }

        public int HitsLeft => _hitsToBreak - _hitsTaken;

        public void TakeHit()
        {
            if (IsBroken) return;

            _hitsTaken++;

            if (_hitsTaken >= _hitsToBreak)
                Break();
        }

        private void Break()
        {
            IsBroken = true;
            Broken?.Invoke(this);
            Destroy(gameObject);
        }
    }
}
