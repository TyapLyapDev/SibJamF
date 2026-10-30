using UnityEngine;

namespace SibJam
{
    [RequireComponent(typeof(IInputReader))]
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Transform _pitchTransform;
        [SerializeField] private float _minimumPitch = -90;
        [SerializeField] private float _maximumPitch = 90;
        [SerializeField] private float _sensitivity = 5f;

        private IInputReader _inputReader;
        private float _currentPitch;

        private void Awake() =>
            _inputReader = GetComponent<IInputReader>();

        private void Update() =>
            Rotate(_inputReader.LookDirection);

        private void Rotate(Vector2 lookInput)
        {
            if (lookInput == Vector2.zero)
                return;

            if (_pitchTransform == null || transform == null)
                return;

            float pitchDelta = lookInput.y * _sensitivity;
            float yawDelta = lookInput.x * _sensitivity;

            _currentPitch = Mathf.Clamp(_currentPitch - pitchDelta, _minimumPitch, _maximumPitch);
            _pitchTransform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
            transform.Rotate(0f, yawDelta, 0f, Space.Self);
        }
    }
}