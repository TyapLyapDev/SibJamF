using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Запускает анимацию со случайной фазы и чуть разной скоростью, чтобы враги волны не двигались синхронно
    public class RandomAnimationStart : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField, Range(0f, 0.5f)] private float _speedSpread = 0.1f;

        private void Start()
        {
            if (_animator == null) return;

            _animator.speed = 1f + Random.Range(-_speedSpread, _speedSpread);
            _animator.Play(_animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, Random.value);
        }
    }
}
