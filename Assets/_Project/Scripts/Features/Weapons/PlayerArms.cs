using System;
using UnityEngine;

namespace SibJam.Weapons
{
    // Руки от первого лица: проигрывают атаку и блок, держат модель оружия в сокете на кисти
    public class PlayerArms : MonoBehaviour
    {
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int BlockId = Animator.StringToHash("Block");

        [SerializeField] private Animator _animator;
        [SerializeField] private ArmsAnimationEvents _animationEvents;
        [SerializeField] private Transform _weaponSocket;
        [Tooltip("Кости, которые не нужны от первого лица (голова, ноги): сжимаются в ноль каждый кадр")]
        [SerializeField] private Transform[] _hiddenBones;

        private RuntimeAnimatorController _defaultController;
        private GameObject _weaponModel;

        public event Action AttackHit;

        private void OnEnable() => _animationEvents.AttackHit += OnAttackHit;

        private void OnDisable() => _animationEvents.AttackHit -= OnAttackHit;

        private void LateUpdate()
        {
            foreach (Transform bone in _hiddenBones)
            {
                if (bone != null)
                    bone.localScale = Vector3.zero;
            }
        }

        public void Equip(WeaponConfig config)
        {
            if (_weaponModel != null)
                Destroy(_weaponModel);

            // Equip может прийти до Awake, пока руки выключены, поэтому контроллер по умолчанию запоминаем здесь
            if (_defaultController == null)
                _defaultController = _animator.runtimeAnimatorController;

            _animator.runtimeAnimatorController = config.AnimatorOverride != null ? config.AnimatorOverride : _defaultController;

            if (config.ModelPrefab == null)
                return;

            _weaponModel = Instantiate(config.ModelPrefab, _weaponSocket);
            _weaponModel.transform.SetLocalPositionAndRotation(config.ModelPosition, Quaternion.Euler(config.ModelRotation));
            _weaponModel.transform.localScale = config.ModelScale;
        }

        public void PlayAttack() => _animator.SetTrigger(AttackId);

        public void SetBlocking(bool isBlocking) => _animator.SetBool(BlockId, isBlocking);

        private void OnAttackHit() => AttackHit?.Invoke();
    }
}
