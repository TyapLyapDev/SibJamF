using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam
{
    public class PlayerInputReader : IPlayerInputReader, IDisposable
    {
        private const string PlayerActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string SprintActionName = "Sprint";
        private const string LookActionName = "Look";
        private const string JumpActionName = "Jump";
        private const string AttackActionName = "Attack";

        private readonly InputActionAsset _inputActions;

        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;

        public PlayerInputReader(InputActionAsset inputActions)
        {
            _inputActions = inputActions != null ? inputActions : throw new ArgumentNullException(nameof(inputActions));
            CreateMap();
            Enable();
        }

        public Vector2 MoveDirection { get; private set; }

        public bool IsSprinting { get; private set; }

        public Vector2 LookDirection { get; private set; }

        public bool IsJumpPressed { get; private set; }

        public bool IsAttackPressed { get; private set; }

        public void Dispose() =>
            Disable();

        private void CreateMap()
        {
            InputActionMap map = _inputActions.FindActionMap(PlayerActionMapName, throwIfNotFound: true);
            _moveAction = map.FindAction(MoveActionName, throwIfNotFound: true);
            _sprintAction = map.FindAction(SprintActionName, throwIfNotFound: true);
            _lookAction = map.FindAction(LookActionName, throwIfNotFound: true);
            _jumpAction = map.FindAction(JumpActionName, throwIfNotFound: true);
            _attackAction = map.FindAction(AttackActionName, throwIfNotFound: true);
        }

        public void Enable()
        {
            _moveAction.Enable();
            _sprintAction.Enable();
            _lookAction.Enable();
            _jumpAction.Enable();
            _attackAction.Enable();

            _moveAction.performed += OnMove;
            _moveAction.canceled += OnMove;

            _sprintAction.performed += OnSprint;
            _sprintAction.canceled += OnSprint;

            _lookAction.performed += OnLook;
            _lookAction.canceled += OnLook;

            _jumpAction.performed += OnJump;
            _jumpAction.canceled += OnJump;

            _attackAction.performed += OnAttack;
            _attackAction.canceled += OnAttack;
        }

        public void Disable()
        {
            _moveAction.performed -= OnMove;
            _moveAction.canceled -= OnMove;

            _sprintAction.performed -= OnSprint;
            _sprintAction.canceled -= OnSprint;

            _lookAction.performed -= OnLook;
            _lookAction.canceled -= OnLook;

            _jumpAction.performed -= OnJump;
            _jumpAction.canceled -= OnJump;

            _attackAction.performed -= OnAttack;
            _attackAction.canceled -= OnAttack;

            _moveAction.Disable();
            _sprintAction.Disable();
            _lookAction.Disable();
            _jumpAction.Disable();
            _attackAction.Disable();

            MoveDirection = Vector2.zero;
            LookDirection = Vector2.zero;
            IsSprinting = false;
            IsJumpPressed = false;
            IsAttackPressed = false;
        }

        private void OnMove(InputAction.CallbackContext context) =>
            MoveDirection = context.ReadValue<Vector2>();

        private void OnSprint(InputAction.CallbackContext context) =>
            IsSprinting = context.ReadValueAsButton();

        private void OnLook(InputAction.CallbackContext context) =>
            LookDirection = context.ReadValue<Vector2>();

        private void OnJump(InputAction.CallbackContext context) =>
            IsJumpPressed = context.ReadValueAsButton();

        private void OnAttack(InputAction.CallbackContext context) =>
            IsAttackPressed = context.ReadValueAsButton();
    }
}