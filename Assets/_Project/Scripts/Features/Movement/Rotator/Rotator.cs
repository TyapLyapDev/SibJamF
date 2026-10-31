using System;
using UnityEngine;
using VContainer;

namespace SibJam
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Transform _pitchTransform;
        [SerializeField] private float _minimumPitch = -90;
        [SerializeField] private float _maximumPitch = 90;

        private IPlayerInputReader _inputReader;
        private ISaver _saver;
        private float _currentPitch;

        [Inject]
        public void Construct(IPlayerInputReader inputReader, ISaver saver)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
            _saver = saver ?? throw new ArgumentNullException(nameof(saver));
        }

        private void Update() =>
            Rotate(_inputReader.LookDirection);

        private void Rotate(Vector2 lookInput)
        {
            if (lookInput == Vector2.zero)
                return;

            if (_pitchTransform == null || transform == null)
                return;

            float sensitivity = _saver.Data.MouseSensitivity;

            float pitchDelta = lookInput.y * sensitivity;
            float yawDelta = lookInput.x * sensitivity;

            _currentPitch = Mathf.Clamp(_currentPitch - pitchDelta, _minimumPitch, _maximumPitch);
            _pitchTransform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
            transform.Rotate(0f, yawDelta, 0f, Space.Self);
        }
    }
}