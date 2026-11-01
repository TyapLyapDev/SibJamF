using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Мост между логикой врага и Animator: ходьба по скорости агента, атака по команде
    public class EnemyAnimator : MonoBehaviour
    {
        private static readonly int MovingId = Animator.StringToHash("Moving");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int AttackEndId = Animator.StringToHash("AttackEnd");
        private static readonly int AttackSpeedId = Animator.StringToHash("AttackSpeed");

        [SerializeField] private Animator _animator;
        [SerializeField] private EnemyMotor _motor;
        [SerializeField, Min(0f)] private float _movingThreshold = 0.1f;

        private void Update()
        {
            if (_animator == null || _motor == null) return;

            _animator.SetBool(MovingId, _motor.Speed > _movingThreshold);
        }

        public void PlayAttack(float speed)
        {
            if (_animator == null) return;

            _animator.ResetTrigger(AttackEndId);
            _animator.SetFloat(AttackSpeedId, speed);
            _animator.SetTrigger(AttackId);
        }

        public void EndAttack()
        {
            if (_animator == null) return;

            _animator.ResetTrigger(AttackId);
            _animator.SetTrigger(AttackEndId);
        }
    }
}
