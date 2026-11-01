using System;
using UnityEngine;

namespace SibJam.Boss
{
    // Висит на объекте с Animator: принимает события из клипов и движение из анимации (root motion) и отдаёт их боссу
    public class BossAnimationEvents : MonoBehaviour
    {
        [SerializeField] private Animator _animator;

        public event Action MeleeHit;
        public event Action Shoot;
        public event Action JumpLanded;
        public event Action<Vector3> Moved;

        public void OnMeleeHit() => MeleeHit?.Invoke();

        public void OnShoot() => Shoot?.Invoke();

        public void OnJumpLand() => JumpLanded?.Invoke();

        // Сам Animator модель не двигает: смещение забирает BossController и двигает весь объект босса
        private void OnAnimatorMove() => Moved?.Invoke(_animator.deltaPosition);
    }
}
