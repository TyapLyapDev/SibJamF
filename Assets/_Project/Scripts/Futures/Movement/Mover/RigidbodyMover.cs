using UnityEngine;

namespace SibJam
{
    public class RigidbodyMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 9f;

        private Rigidbody _rigidbody;
        private IInputReader _inputReader;
        private IGroundDetector _groundDetector;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _inputReader = GetComponent<IInputReader>();
            _groundDetector = GetComponent<IGroundDetector>();
        }

        private void FixedUpdate() =>
            Move(Time.fixedDeltaTime);

        public void Move(float deltaTime)
        {
            Vector2 input = _inputReader.MoveDirection;

            if (input == Vector2.zero)
                return;

            Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
            moveDirection.y = 0f;
            moveDirection.Normalize();

            if (_groundDetector.TryGetGroundNormal(out Vector3 groundNormal))
            {
                moveDirection = Vector3.ProjectOnPlane(moveDirection, groundNormal);

                if (moveDirection.sqrMagnitude < 0.0001f)
                    return;
                moveDirection.Normalize();
            }

            float speed = _inputReader.IsSprinting ? _sprintSpeed : _walkSpeed;
            Vector3 velocity = speed * moveDirection;
            velocity.y = _rigidbody.linearVelocity.y;
            _rigidbody.linearVelocity = velocity;
        }
    }
}