using UnityEngine;

namespace SibJam.Weapons
{
    // Покой рук: пока аниматор в Idle, руки с оружием опущены ниже позы блока и «дышат».
    // На удар и блок смещение плавно убирается, и анимации играют как есть
    public class ArmsIdleSway : MonoBehaviour
    {
        private static readonly int IdleStateId = Animator.StringToHash("Idle");

        [SerializeField] private Animator _animator;

        [Header("Поза покоя относительно позы блока")]
        [SerializeField] private Vector3 _idlePosition = new Vector3(0.04f, -0.12f, 0f);
        [SerializeField] private Vector3 _idleRotation = new Vector3(12f, 0f, -6f);
        [Tooltip("Скорость перехода между покоем и действием")]
        [SerializeField, Min(0.1f)] private float _blendSpeed = 10f;

        [Header("Дыхание")]
        [SerializeField, Min(0.1f)] private float _breathPeriod = 3.2f;
        [SerializeField] private Vector3 _breathPosition = new Vector3(0f, 0.008f, 0f);
        [SerializeField] private Vector3 _breathRotation = new Vector3(1.2f, 0f, 0.6f);

        private Vector3 _basePosition;
        private Quaternion _baseRotation;
        private float _weight;

        private void Awake()
        {
            _basePosition = transform.localPosition;
            _baseRotation = transform.localRotation;
        }

        private void OnDisable()
        {
            _weight = 0f;
            transform.SetLocalPositionAndRotation(_basePosition, _baseRotation);
        }

        private void LateUpdate()
        {
            var target = IsIdle() ? 1f : 0f;
            _weight = Mathf.MoveTowards(_weight, target, _blendSpeed * Time.deltaTime);

            var breath = Mathf.Sin(Time.time * Mathf.PI * 2f / _breathPeriod);
            var position = _idlePosition + _breathPosition * breath;
            var rotation = _idleRotation + _breathRotation * breath;

            var weight = Mathf.SmoothStep(0f, 1f, _weight);
            transform.SetLocalPositionAndRotation(
                _basePosition + position * weight,
                _baseRotation * Quaternion.Euler(rotation * weight));
        }

        private bool IsIdle() =>
            _animator != null && !_animator.IsInTransition(0) && _animator.GetCurrentAnimatorStateInfo(0).shortNameHash == IdleStateId;
    }
}
