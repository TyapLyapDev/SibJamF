using System;
using UnityEngine;

namespace SibJam.SecurityCameras
{
    public class CameraDestructionGoal : MonoBehaviour, ICameraDestructionGoalInfo
    {
        [SerializeField] private SecurityCamera[] _securityCameras;

        public event Action Completed;
        public event Action Changed;

        public int TotalCount => _securityCameras.Length;

        public int BrokenCount { get; private set; }

        public int LeftCount => TotalCount - BrokenCount;

        public bool IsCompleted { get; private set; }

        private void OnEnable()
        {
            foreach (SecurityCamera securityCamera in _securityCameras)
            {
                if (securityCamera != null)
                    securityCamera.Broken += OnCameraBroken;
            }
        }

        private void OnDisable()
        {
            foreach (SecurityCamera securityCamera in _securityCameras)
            {
                if (securityCamera != null)
                    securityCamera.Broken -= OnCameraBroken;
            }
        }

        private void OnCameraBroken(SecurityCamera securityCamera)
        {
            securityCamera.Broken -= OnCameraBroken;
            BrokenCount++;
            Changed?.Invoke();

            if (IsCompleted || BrokenCount < TotalCount) return;

            IsCompleted = true;
            Completed?.Invoke();
        }
    }
}
