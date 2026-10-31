using System;
using System.Collections;
using _Project.Scripts.Enemies;
using SibJam.SecurityCameras;
using UnityEngine;
using VContainer;

namespace SibJam.Weapons
{
    // Меч: по ЛКМ — рубящий удар сферой перед прицелом, урон врагам и удары по камерам
    public class MeleeWeapon : MonoBehaviour
    {
        [SerializeField] private Transform _aimTransform;
        [SerializeField] private Transform _model;
        [SerializeField, Min(0f)] private float _damage = 50f;
        [SerializeField, Min(0f)] private float _range = 2.5f;
        [SerializeField, Min(0f)] private float _radius = 0.5f;
        [SerializeField, Min(0f)] private float _cooldown = 0.45f;
        [SerializeField] private LayerMask _hitMask = ~0;

        private IPlayerInputReader _inputReader;
        private bool _wasAttackPressed;
        private float _nextAttackTime;
        private Quaternion _modelRestRotation;

        [Inject]
        public void Construct(IPlayerInputReader inputReader)
        {
            _inputReader = inputReader ?? throw new ArgumentNullException(nameof(inputReader));
        }

        private void Awake()
        {
            if (_model != null)
                _modelRestRotation = _model.localRotation;
        }

        private void OnEnable() => SetModelVisible(true);

        private void OnDisable() => SetModelVisible(false);

        private void Update()
        {
            bool isAttackPressed = _inputReader.IsAttackPressed;

            if (isAttackPressed && !_wasAttackPressed && CursorLocker.IsLocked && Time.time >= _nextAttackTime)
                Attack();

            _wasAttackPressed = isAttackPressed;
        }

        private void Attack()
        {
            _nextAttackTime = Time.time + _cooldown;

            if (_model != null)
                StartCoroutine(Swing());

            var hits = Physics.SphereCastAll(_aimTransform.position, _radius, _aimTransform.forward, _range, _hitMask, QueryTriggerInteraction.Ignore);

            foreach (RaycastHit hit in hits)
            {
                Enemy enemy = hit.collider.GetComponentInParent<Enemy>();

                if (enemy != null)
                {
                    enemy.TakeDamage(_damage);
                    continue;
                }

                SecurityCamera securityCamera = hit.collider.GetComponentInParent<SecurityCamera>();

                if (securityCamera != null)
                    securityCamera.TakeHit();
            }
        }

        private IEnumerator Swing()
        {
            const float duration = 0.25f;
            // Клинок вдоль локальной оси Y: рубим сверху-справа вниз-влево
            Quaternion swung = _modelRestRotation * Quaternion.Euler(85f, 0f, -35f);

            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = time / duration;
                _model.localRotation = Quaternion.Slerp(_modelRestRotation, swung, Mathf.Sin(t * Mathf.PI));
                yield return null;
            }

            _model.localRotation = _modelRestRotation;
        }

        private void SetModelVisible(bool visible)
        {
            if (_model != null)
                _model.gameObject.SetActive(visible);
        }
    }
}
