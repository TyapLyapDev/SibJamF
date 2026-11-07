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
        private const string BlockActionName = "Block";

        private readonly InputActionAsset _inputActions;

        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;
        private InputAction _blockAction;
        private bool _enabled;
        private int _jumpBlockCount;

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

        public bool IsBlockPressed { get; private set; }

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
            _blockAction = map.FindAction(BlockActionName, throwIfNotFound: true);
        }

        public void Enable()
        {
            if (_enabled)
                return;

            _enabled = true;
            _moveAction.Enable();
            _sprintAction.Enable();
            _lookAction.Enable();
            if (_jumpBlockCount == 0)
                _jumpAction.Enable();
            _attackAction.Enable();
            _blockAction.Enable();

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

            _blockAction.performed += OnBlock;
            _blockAction.canceled += OnBlock;
        }

        public void Disable()
        {
            _enabled = false;
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

            _blockAction.performed -= OnBlock;
            _blockAction.canceled -= OnBlock;

            _moveAction.Disable();
            _sprintAction.Disable();
            _lookAction.Disable();
            _jumpAction.Disable();
            _attackAction.Disable();
            _blockAction.Disable();

            MoveDirection = Vector2.zero;
            LookDirection = Vector2.zero;
            IsSprinting = false;
            IsJumpPressed = false;
            IsAttackPressed = false;
            IsBlockPressed = false;
        }

        public void BlockJump()
        {
            _jumpBlockCount++;
            _jumpAction.Disable();
            IsJumpPressed = false;
        }

        public void UnblockJump()
        {
            if (_jumpBlockCount == 0)
                return;

            _jumpBlockCount--;

            // Button без initialStateCheck не подхватит зажатый при пропуске пробел.
            if (_jumpBlockCount == 0 && _enabled)
                _jumpAction.Enable();
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

        private void OnBlock(InputAction.CallbackContext context) =>
            IsBlockPressed = context.ReadValueAsButton();
    }
}
