using System;
using _Project.Scripts.Enemies;
using SibJam.SecurityCameras;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace SibJam.Waves.Testing
{
    public class RaycastEnemyDamager : MonoBehaviour
    {
        [SerializeField] private Transform _aimTransform;
        [SerializeField, Min(0f)] private float _damage = 50f;
        [SerializeField, Min(0f)] private float _distance = 50f;
        [SerializeField] private LayerMask _hitMask = ~0;

        private readonly ICursorShower _cursorShower = new CursorShower();

        private IPlayerInputReader _inputReader;
        private bool _wasAttackPressed;

        [Inject]
        public void Construct(IPlayerInputReader inputReader)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        }

        private void OnEnable() =>
            _cursorShower.Hide();

        private void OnDisable() =>
            _cursorShower.Show();

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                _cursorShower.Show();

            bool isAttackPressed = _inputReader.IsAttackPressed;

            if (isAttackPressed && !_wasAttackPressed)
            {
                if (_cursorShower.IsShow)
                    _cursorShower.Hide();
                else
                    Attack();
            }

            _wasAttackPressed = isAttackPressed;
        }

        private void Attack()
        {
            if (_aimTransform == null)
                return;

            Ray ray = new Ray(_aimTransform.position, _aimTransform.forward);
            Debug.DrawRay(ray.origin, ray.direction * _distance, Color.red, 0.5f);

            if (!Physics.Raycast(ray, out RaycastHit hit, _distance, _hitMask, QueryTriggerInteraction.Ignore))
                return;

            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                enemy.TakeDamage(_damage);
                return;
            }

            SecurityCamera securityCamera = hit.collider.GetComponentInParent<SecurityCamera>();

            if (securityCamera != null)
                securityCamera.TakeHit();
        }
    }
}
