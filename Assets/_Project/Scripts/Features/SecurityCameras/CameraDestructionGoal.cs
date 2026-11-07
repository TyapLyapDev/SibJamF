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
                if (securityCamera == null) continue;

                securityCamera.Broken += OnCameraBroken;
                securityCamera.Repaired += OnCameraRepaired;
            }
        }

        private void OnDisable()
        {
            foreach (SecurityCamera securityCamera in _securityCameras)
            {
                if (securityCamera == null) continue;

                securityCamera.Broken -= OnCameraBroken;
                securityCamera.Repaired -= OnCameraRepaired;
            }
        }

        // Починенную камеру снова нужно ломать; Completed сработает повторно
        private void OnCameraRepaired(SecurityCamera securityCamera)
        {
            securityCamera.Broken += OnCameraBroken;
            BrokenCount = Mathf.Max(0, BrokenCount - 1);
            IsCompleted = false;
            Changed?.Invoke();
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
