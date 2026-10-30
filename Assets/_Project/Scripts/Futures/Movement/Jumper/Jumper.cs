using UnityEngine;

namespace SibJam
{
    [RequireComponent(typeof(IGravity))]
    [RequireComponent(typeof(IGroundDetector))]
    [RequireComponent(typeof(IInputReader))]
    public class Jumper : MonoBehaviour
    {
        [SerializeField] private float _jumpImpulse;
        [SerializeField] private float _mass;

        private IGravity _gravity;
        private IGroundDetector _groundChecker;
        private IInputReader _inputReader;

        private void Awake()
        {
            _gravity = GetComponent<IGravity>();
            _groundChecker = GetComponent<IGroundDetector>();
            _inputReader = GetComponent<IInputReader>();
        }

        private void Update()
        {
            if (_inputReader.IsJumpPressed)
                Jump();
        }

        private void Jump()
        {
            if (_groundChecker.HasGround() == false)
                return;

            if (_mass <= 0f)
                return;

            float velocity = _jumpImpulse / _mass;
            _gravity.SetVerticalVelocity(velocity);
        }
    }
}