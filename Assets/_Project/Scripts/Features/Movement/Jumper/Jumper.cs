using System;
using UnityEngine;
using VContainer;

namespace SibJam
{
    // Прыжок на фиксированную высоту: скорость считается из высоты и гравитации (v = √(2gh)),
    // поэтому высота не зависит от того, что было до прыжка, и вертикальная скорость не копится.
    // Срабатывает по нажатию или удержанию, но только от настоящего пола.
    [RequireComponent(typeof(IGravity))]
    [RequireComponent(typeof(IGroundDetector))]
    public class Jumper : MonoBehaviour
    {
        [Tooltip("Высота прыжка в метрах")]
        [SerializeField, Min(0.1f)] private float _jumpHeight = 1.1f;
        [Tooltip("Сколько секунд после схода с пола ещё можно прыгнуть (только с пола, не со стены)")]
        [SerializeField, Min(0f)] private float _coyoteTime = 0.12f;
        [Tooltip("Сколько секунд помним нажатие прыжка до приземления")]
        [SerializeField, Min(0f)] private float _jumpBufferTime = 0.12f;
        [Tooltip("Удержание пробела — прыгаем снова при каждом касании пола (только от пола, не от стен)")]
        [SerializeField] private bool _jumpWhileHeld = true;
        [Tooltip("Минимальная пауза между прыжками — не даёт прыгнуть повторно, пока ещё касаемся пола")]
        [SerializeField, Min(0f)] private float _jumpCooldown = 0.2f;

        private IGravity _gravity;
        private IGroundDetector _groundChecker;
        private IPlayerInputReader _inputReader;

        private bool _wasJumpHeld;
        private float _lastSupportTime = float.NegativeInfinity;
        private float _lastJumpPressTime = float.NegativeInfinity;
        private float _lastJumpTime = float.NegativeInfinity;

        [Inject]
        public void Construct(IPlayerInputReader inputReader)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        }

        private void Awake()
        {
            _gravity = GetComponent<IGravity>();
            _groundChecker = GetComponent<IGroundDetector>();
        }

        private void OnDisable() =>
            _lastJumpPressTime = _lastSupportTime = float.NegativeInfinity;

        private void Update()
        {
            float now = Time.time;
            bool held = _inputReader.IsJumpPressed;

            if (held && _wasJumpHeld == false)
                _lastJumpPressTime = now;

            _wasJumpHeld = held;

            bool cooledDown = now - _lastJumpTime >= _jumpCooldown;

            if (cooledDown && _groundChecker.HasJumpSupport())
                _lastSupportTime = now;

            bool buffered = now - _lastJumpPressTime <= _jumpBufferTime || (_jumpWhileHeld && held);
            bool supported = now - _lastSupportTime <= _coyoteTime;

            if (buffered && supported && cooledDown)
                Jump(now);
        }

        private void Jump(float now)
        {
            _lastJumpTime = now;
            _lastJumpPressTime = float.NegativeInfinity;
            _lastSupportTime = float.NegativeInfinity;

            float velocity = Mathf.Sqrt(2f * _gravity.Strength * _jumpHeight);
            _gravity.SetVerticalVelocity(velocity);
        }
    }
}
