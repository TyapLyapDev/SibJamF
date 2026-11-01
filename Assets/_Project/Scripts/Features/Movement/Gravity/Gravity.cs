using System;
using System.Collections.Generic;
using _Project.Scripts.Enemies;
using SibJam.Feedback;
using UnityEngine;

namespace SibJam
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(IGroundDetector))]
    public class Gravity : MonoBehaviour, IGravity
    {
        [SerializeField, Min(0f)] private float _gravity = 20f;
        [Tooltip("Во сколько раз сильнее тянет вниз при падении — прыжок резче")]
        [SerializeField, Min(1f)] private float _fallMultiplier = 1.5f;

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

        private float _velocityY;

        private readonly Collider[] _overlaps = new Collider[MaxOverlaps];
        private readonly HashSet<Collider> _ignoredEnemies = new();
        private readonly HashSet<Collider> _touchedEnemies = new();

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

            if (_groundDetector.HasGround() && _velocityY <= 0f)
            {
                if (_velocityY < -_landingMinSpeed)
                    CameraShake.Dip(Mathf.Min(-_velocityY * _landingDipPerSpeed, _landingMaxDipSpeed));

                _velocityY = 0;

                return;
            }

            _velocityY -= _gravity * (_velocityY < 0f ? _fallMultiplier : 1f) * deltaTime;
            Vector3 verticalMovement = new(0f, _velocityY * deltaTime, 0f);
            _controller.Move(verticalMovement);
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
