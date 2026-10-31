using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam
{
    public class InputReader : MonoBehaviour, IInputReader
    {
        private const string PlayerActionMapName = "Player";
        private const string MoveActionName = "Move";
        private const string SprintActionName = "Sprint";
        private const string LookActionName = "Look";
        private const string JumpActionName = "Jump";
        private const string AttackActionName = "Attack";

        [SerializeField] private InputActionAsset _inputActions;

        private InputAction _moveAction;
        private InputAction _sprintAction;
        private InputAction _lookAction;
        private InputAction _jumpAction;
        private InputAction _attackAction;

        public Vector2 MoveDirection { get; private set; }

        public bool IsSprinting { get; private set; }

        public Vector2 LookDirection { get; private set; }

        public bool IsJumpPressed { get; private set; }

        public bool IsAttackPressed { get; private set; }

        private void Awake()
        {
            InputActionMap map = _inputActions.FindActionMap(PlayerActionMapName, throwIfNotFound: true);
            _moveAction = map.FindAction(MoveActionName, throwIfNotFound: true);
            _sprintAction = map.FindAction(SprintActionName, throwIfNotFound: true);
            _lookAction = map.FindAction(LookActionName, throwIfNotFound: true);
            _jumpAction = map.FindAction(JumpActionName, throwIfNotFound: true);
            _attackAction = map.FindAction(AttackActionName, throwIfNotFound: true);
        }

        private void OnEnable()
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

        private void OnDisable()
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