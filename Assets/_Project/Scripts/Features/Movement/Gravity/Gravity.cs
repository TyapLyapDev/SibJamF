using System;
using System.Collections.Generic;
using _Project.Scripts.Enemies;
using SibJam.Feedback;
using UnityEngine;
using VContainer;

namespace SibJam
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(IGroundDetector))]
    public class Gravity : MonoBehaviour, IGravity
    {
        [SerializeField, Min(0.1f)] private float _gravity = 22f;
        [Tooltip("Во сколько раз сильнее тянет вниз при падении — прыжок резче")]
        [SerializeField, Min(1f)] private float _fallMultiplier = 1.35f;
        [Tooltip("Предельная скорость падения — вертикальная скорость не копится бесконечно")]
        [SerializeField, Min(1f)] private float _maxFallSpeed = 30f;
        [Tooltip("Небольшое прижатие к полу, чтобы не подпрыгивать на спусках")]
        [SerializeField, Min(0f)] private float _groundStickSpeed = 2f;

        [Header("Крутые склоны")]
        [Tooltip("Скорость сползания по склону круче slopeLimit")]
        [SerializeField, Min(0f)] private float _slideSpeed = 6f;

        [Header("Проход сквозь врагов")]
        [Tooltip("С какой скоростью расталкиваем врагов, сквозь которых падаем/стоим")]
        [SerializeField, Min(0f)] private float _shoveSpeed = 8f;

        [Header("Приземление")]
        [Tooltip("Минимальная скорость падения, с которой камера приседает")]
        [SerializeField, Min(0f)] private float _landingMinSpeed = 4f;
        [Tooltip("Сколько скорости приседа камеры даёт каждая единица скорости падения")]
        [SerializeField, Min(0f)] private float _landingDipPerSpeed = 0.18f;
        [SerializeField, Min(0f)] private float _landingMaxDipSpeed = 4f;

        private const int MaxOverlaps = 16;

        private CharacterController _controller;
        private IGroundDetector _groundDetector;

        private CameraShaker _shaker;
        private float _velocityY;
        private float _defaultStepOffset;

        public float Strength => _gravity;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly HashSet<Collider> _ignoredEnemies = new();
        private readonly HashSet<Collider> _touchedEnemies = new();

        [Inject]
        public void Construct(CameraShaker shaker) => _shaker = shaker;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _groundDetector = GetComponent<IGroundDetector>();
            _defaultStepOffset = _controller.stepOffset;
        }

        private void Update() =>
            Apply(Time.deltaTime);

        public void SetVerticalVelocity(float velocity)
        {
            if (velocity < 0)
                throw new ArgumentOutOfRangeException(nameof(velocity), velocity, "Vertical velocity must be non-negative.");

            _velocityY = velocity;
        }

        public void Slam(float downwardSpeed)
        {
            if (downwardSpeed < 0)
                throw new ArgumentOutOfRangeException(nameof(downwardSpeed), downwardSpeed, "Downward speed must be non-negative.");

            if (_groundDetector.HasGround())
                return;

            _velocityY = Mathf.Min(_velocityY, -downwardSpeed);
        }

        private void Apply(float deltaTime)
        {
            if (deltaTime < 0)
                throw new ArgumentOutOfRangeException(nameof(deltaTime), deltaTime, "DeltaTime must be non-negative.");

            if (deltaTime == 0)
                return;

            PassThroughEnemies(deltaTime);

            bool grounded = _groundDetector.HasGround();

            // В воздухе stepOffset выключен — иначе контроллер «перешагивает» на уступы и стены
            _controller.stepOffset = grounded ? _defaultStepOffset : 0f;

            if (grounded && _velocityY <= 0f)
            {
                if (_velocityY < -_landingMinSpeed)
                    _shaker.Dip(Mathf.Min(-_velocityY * _landingDipPerSpeed, _landingMaxDipSpeed));

                _velocityY = 0;
                _controller.Move(_groundStickSpeed * deltaTime * Vector3.down);

                return;
            }

            _velocityY -= _gravity * (_velocityY < 0f ? _fallMultiplier : 1f) * deltaTime;
            _velocityY = Mathf.Max(_velocityY, -_maxFallSpeed);

            Vector3 movement = new(0f, _velocityY * deltaTime, 0f);

            // Склон круче slopeLimit: сползаем вдоль него вниз, а не стоим/карабкаемся
            if (_velocityY <= 0f && _groundDetector.TryGetSteepSlopeNormal(out Vector3 steepNormal))
            {
                Vector3 slideDirection = Vector3.ProjectOnPlane(Vector3.down, steepNormal).normalized;
                movement += _slideSpeed * deltaTime * slideDirection;
            }

            CollisionFlags flags = _controller.Move(movement);

            // Ударились головой — подъём гасится сразу, без «прилипания» к потолку
            if ((flags & CollisionFlags.Above) != 0 && _velocityY > 0f)
                _velocityY = 0f;
        }

        // В воздухе игрок не встаёт на врагов, а проваливается сквозь них и расталкивает в стороны.
        // Коллизия возвращается, только когда игрок полностью вышел из врага.
        private void PassThroughEnemies(float deltaTime)
        {
            bool isAirborne = _groundDetector.HasGround() == false;
            Vector3 center = transform.TransformPoint(_controller.center);
            float halfHeight = Mathf.Max(0f, _controller.height * 0.5f - _controller.radius);
            float radius = _controller.radius + _controller.skinWidth + 0.05f;
            Vector3 top = center + Vector3.up * halfHeight;
            Vector3 bottom = center - Vector3.up * (halfHeight + 0.1f);

            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _overlaps, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            _touchedEnemies.Clear();

            for (int i = 0; i < count; i++)
            {
                Collider hit = _overlaps[i];
                EnemyMotor enemy = hit.GetComponentInParent<EnemyMotor>();

                if (enemy == null)
                    continue;

                if (_ignoredEnemies.Contains(hit) == false)
                {
                    if (isAirborne == false)
                        continue;

                    Physics.IgnoreCollision(_controller, hit, true);
                    _ignoredEnemies.Add(hit);
                }

                _touchedEnemies.Add(hit);

                Vector3 away = enemy.transform.position - transform.position;
                away.y = 0f;

                if (away.sqrMagnitude < 0.0001f)
                    away = transform.forward;

                enemy.Shove(_shoveSpeed * deltaTime * away.normalized);
            }

            _ignoredEnemies.RemoveWhere(RestoreIfLeft);
        }

        private bool RestoreIfLeft(Collider enemyCollider)
        {
            if (enemyCollider == null)
                return true;

            if (_touchedEnemies.Contains(enemyCollider))
                return false;

            Physics.IgnoreCollision(_controller, enemyCollider, false);

            return true;
        }
    }
}
