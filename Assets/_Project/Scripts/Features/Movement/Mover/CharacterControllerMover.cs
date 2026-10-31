using System;
using UnityEngine;
using VContainer;

namespace SibJam
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(IGroundDetector))]
    public class CharacterControllerMover : MonoBehaviour, IMover
    {
        [SerializeField] private float _walkSpeed = 5f;
        [SerializeField] private float _sprintSpeed = 9f;

        private CharacterController _controller;
        private IPlayerInputReader _inputReader;
        private IGroundDetector _groundDetector;

        [Inject]
        public void Construct(IPlayerInputReader inputReader)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _groundDetector = GetComponent<IGroundDetector>();
        }

        private void Update() =>
            Move(Time.deltaTime);

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
            _controller.Move(speed * deltaTime * moveDirection);
        }
    }
}