using System;
using UnityEngine;

namespace SibJam
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(IGroundDetector))]
    public class Gravity : MonoBehaviour, IGravity
    {
        private const float DefaultGravity = 10f;

        private CharacterController _controller;
        private IGroundDetector _groundDetector;

        private float _velocityY;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _groundDetector = GetComponent<IGroundDetector>();
        }

        private void Update() =>
            Apply(Time.deltaTime);

        public void SetVerticalVelocity(float velocity)
        {
            if (velocity < 0)
                throw new ArgumentOutOfRangeException(nameof(velocity), velocity, "Vertical velocity must be non-negative.");

            _velocityY = velocity;
        }

        private void Apply(float deltaTime)
        {
            if (deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "DeltaTime must be non-negative.");

            if (deltaTime == 0)
                return;

            if (_groundDetector.HasGround() && _velocityY <= 0f)
            {
                _velocityY = 0;

                return;
            }

            _velocityY -= DefaultGravity * deltaTime;
            Vector3 verticalMovement = new(0f, _velocityY * deltaTime, 0f);
            _controller.Move(verticalMovement);
        }
    }
}