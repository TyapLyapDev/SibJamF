using SibJam.Player;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Combat
{
    // Вешается на игрока: точка, за которой охотятся враги. Удар сперва проверяется мечом (парирование), остаток урона идёт в PlayerHealth
    public class PlayerTarget : MonoBehaviour, IEnemyTarget
    {
        [SerializeField] private Transform _aimPoint;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private MeleeWeapon _weapon;
        [Tooltip("Парировать можно только то, что летит спереди: угол от взгляда до источника удара")]
        [SerializeField, Range(0f, 180f)] private float _parryAngle = 75f;
        [Tooltip("Доля урона, которая проходит сквозь блок после окна парирования. 1 — долгий блок не защищает")]
        [SerializeField, Range(0f, 1f)] private float _blockDamageMultiplier = 1f;

        public Vector3 Position => transform.position;
        public Vector3 AimPoint => _aimPoint != null ? _aimPoint.position : transform.position + Vector3.up;
        public bool IsAlive => isActiveAndEnabled && !_health.IsDead;

        public HitResult TakeHit(HitInfo hit)
        {
            var damage = hit.Damage;

            if (_weapon != null && _weapon.isActiveAndEnabled && hit.Parryable && IsFacing(hit.Direction))
            {
                switch (_weapon.ResolveIncomingHit())
                {
                    case IncomingHitResult.Parried:
                        return HitResult.Parried;
                    case IncomingHitResult.Blocked:
                        damage *= _blockDamageMultiplier;
                        break;
                }
            }

            _health.TakeDamage(damage);
            return HitResult.Hit;
        }

        // Direction — куда летит удар, значит источник в обратной стороне
        private bool IsFacing(Vector3 hitDirection)
        {
            var toSource = -hitDirection;
            var forward = _aimPoint != null ? _aimPoint.forward : transform.forward;
            toSource.y = 0f;
            forward.y = 0f;
            return Vector3.Angle(forward, toSource) <= _parryAngle;
        }
    }
}
