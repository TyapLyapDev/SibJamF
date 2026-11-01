using SibJam.Player;
using UnityEngine;

namespace SibJam.Combat
{
    // Вешается на игрока: точка, за которой охотятся враги. Урон передаёт в PlayerHealth, парирование подключим позже
    public class PlayerTarget : MonoBehaviour, IEnemyTarget
    {
        [SerializeField] private Transform _aimPoint;
        [SerializeField] private PlayerHealth _health;

        public Vector3 Position => transform.position;
        public Vector3 AimPoint => _aimPoint != null ? _aimPoint.position : transform.position + Vector3.up;
        public bool IsAlive => isActiveAndEnabled && !_health.IsDead;

        public HitResult TakeHit(HitInfo hit)
        {
            _health.TakeDamage(hit.Damage);
            return HitResult.Hit;
        }
    }
}
