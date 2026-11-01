using System;
using UnityEngine;

namespace SibJam.Weapons
{
    // Принимает события из клипов рук: должен висеть на том же объекте, что и Animator
    public class ArmsAnimationEvents : MonoBehaviour
    {
        public event Action AttackHit;

        // Вызывается событием "AttackHit" в клипе атаки, в кадр, когда клинок проходит перед камерой
        public void OnAttackHit() => AttackHit?.Invoke();
    }
}
