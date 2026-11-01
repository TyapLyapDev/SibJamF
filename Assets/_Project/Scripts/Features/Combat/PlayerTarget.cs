using SibJam.Player;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Combat
{
    // Вешается на игрока: точка, за которой охотятся враги. Удар сперва проверяется мечом (парирование), остаток урона идёт в PlayerHealth
    public class PlayerTarget : MonoBehaviour, IEnemyTarget, IPlayerHitReceiver
    {
        [SerializeField] private Transform _aimPoint;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private MeleeWeapon _weapon;
        [Tooltip("Парировать можно только то, что летит спереди: угол от взгляда до источника удара")]
        [SerializeField, Range(0f, 180f)] private float _parryAngle = 75f;
        [Tooltip("Доля урона, которая проходит сквозь блок. 0 — блок держит удар полностью (пока его не пробьют)")]
        [SerializeField, Range(0f, 1f)] private float _blockDamageMultiplier;

        public Vector3 Position => transform.position;
        public Vector3 AimPoint => _aimPoint != null ? _aimPoint.position : transform.position + Vector3.up;
        public bool IsAlive => isActiveAndEnabled && !_health.IsDead;

        public HitResult TakeHit(HitInfo hit)
        {
            if (TryParry(hit))
                return HitResult.Parried;

            var damage = hit.Damage;

            // Блок держит удар, но каждый попавший в него удар расшатывает его (см. MeleeWeapon)
            if (_weapon != null && _weapon.isActiveAndEnabled && _weapon.IsBlocking && IsFacing(hit.Direction)
                && _weapon.ResolveIncomingHit() == IncomingHitResult.Blocked)
                damage *= _blockDamageMultiplier;

            _health.TakeDamage(damage);
            return HitResult.Hit;
        }

        // Удары босса: блок/парирование мечом, остаток урона в PlayerHealth
        public IncomingHitResult ReceiveHit(PlayerHit hit)
        {
            if (_health.IsDead) return IncomingHitResult.Hit;

            var result = IncomingHitResult.Hit;
            var facing = hit.Source is not Component source || IsFacing(transform.position - source.transform.position);

            if (hit.CanBlock && facing && _weapon != null && _weapon.isActiveAndEnabled
                && (_weapon.IsBlocking || _weapon.IsInParryWindow))
                result = _weapon.ResolveIncomingHit();

            // Нельзя парировать — значит, в окне парирования удар просто блокируется
            if (result == IncomingHitResult.Parried && !hit.CanParry)
                result = IncomingHitResult.Blocked;

            var damage = result switch
            {
                IncomingHitResult.Parried => 0f,
                IncomingHitResult.Blocked => hit.BlockedDamage,
                _ => hit.Damage
            };

            _health.TakeDamage(damage);
            return result;
        }

        // Отбит ли удар окном парирования. Снаряды зовут это заранее, на подлёте, чтобы попасть в окно было реально
        public bool TryParry(HitInfo hit)
        {
            if (_weapon == null || !_weapon.isActiveAndEnabled || !hit.Parryable || !_weapon.IsInParryWindow || !IsFacing(hit.Direction))
                return false;

            return _weapon.ResolveIncomingHit() == IncomingHitResult.Parried;
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
