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

        [Header("Земля")]
        [Tooltip("Разгон на земле (м/с²) — меньше = мягче старт")]
        [SerializeField, Min(0f)] private float _groundAcceleration = 90f;
        [Tooltip("Торможение на земле без ввода (м/с²)")]
        [SerializeField, Min(0f)] private float _groundDeceleration = 110f;

        [Header("Воздух")]
        [Tooltip("Как быстро в воздухе горизонтальная скорость тянется к заданной вводом (м/с²)")]
        [SerializeField, Min(0f)] private float _airAcceleration = 55f;
        [Tooltip("Торможение в воздухе без ввода (м/с²) — не дрейфуем бесконечно")]
        [SerializeField, Min(0f)] private float _airDrag = 6f;

        private CharacterController _controller;
        private IPlayerInputReader _inputReader;
        private IGroundDetector _groundDetector;

        private Vector3 _airVelocity;

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

        private void OnDisable() =>
            _airVelocity = Vector3.zero;

        private void Update() =>
            Move(Time.deltaTime);

        public void Move(float deltaTime)
        {
            Vector2 input = _inputReader.MoveDirection;
            Vector3 wish = transform.forward * input.y + transform.right * input.x;
            wish.y = 0f;
            wish = Vector3.ClampMagnitude(wish, 1f);

            float speed = _inputReader.IsSprinting ? _sprintSpeed : _walkSpeed;
            Vector3 targetVelocity = wish * speed;

            if (_groundDetector.TryGetGroundNormal(out Vector3 groundNormal))
            {
                // На земле — быстрый, но плавный разгон/торможение; та же скорость уносится в воздух
                float groundRate = wish == Vector3.zero ? _groundDeceleration : _groundAcceleration;
                _airVelocity = Vector3.MoveTowards(_airVelocity, targetVelocity, groundRate * deltaTime);

                float speedNow = _airVelocity.magnitude;
                if (speedNow < 0.0001f)
                    return;

                Vector3 along = Vector3.ProjectOnPlane(_airVelocity, groundNormal);
                if (along.sqrMagnitude < 0.0001f)
                    return;

                CollisionFlags groundFlags = _controller.Move(along.normalized * (speedNow * deltaTime));
                if ((groundFlags & CollisionFlags.Sides) != 0)
                    _airVelocity = Vector3.ProjectOnPlane(_controller.velocity, Vector3.up);
                return;
            }

            // В воздухе — плавное управление: импульс сохраняется, ввод его корректирует
            float rate = wish == Vector3.zero ? _airDrag : _airAcceleration;
            _airVelocity = Vector3.MoveTowards(_airVelocity, targetVelocity, rate * deltaTime);

            Vector3 delta = _airVelocity * deltaTime;

            // На крутом склоне не даём вводу толкать вверх по нему
            if (_groundDetector.TryGetSteepSlopeNormal(out Vector3 steepNormal))
            {
                Vector3 uphill = Vector3.ProjectOnPlane(steepNormal, Vector3.up).normalized * -1f;
                float up = Vector3.Dot(delta, uphill);
                if (up > 0f)
                {
                    delta -= uphill * up;
                    _airVelocity -= uphill * Vector3.Dot(_airVelocity, uphill);
                }
            }

            if (delta == Vector3.zero)
                return;

            CollisionFlags flags = _controller.Move(delta);

            // Упёрлись в стену — гасим накопленную скорость, чтобы не «ползти» по ней
            if ((flags & CollisionFlags.Sides) != 0)
                _airVelocity = Vector3.ProjectOnPlane(_controller.velocity, Vector3.up);
        }
    }
}
