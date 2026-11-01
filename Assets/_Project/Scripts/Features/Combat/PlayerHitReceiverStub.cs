using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Combat
{
    // Временная заглушка, пока нет системы здоровья: считает блок и парирование мечом и пишет удар в лог.
    // Заменить на компонент здоровья, реализующий IPlayerHitReceiver
    public class PlayerHitReceiverStub : MonoBehaviour, IPlayerHitReceiver
    {
        [SerializeField] private MeleeWeapon _weapon;
        [SerializeField] private bool _log = true;

        public IncomingHitResult ReceiveHit(PlayerHit hit)
        {
            IncomingHitResult result = IncomingHitResult.Hit;

            if (hit.CanBlock && _weapon != null && _weapon.enabled)
                result = _weapon.ResolveIncomingHit();

            // Нельзя парировать — значит, в окне парирования удар просто блокируется
            if (result == IncomingHitResult.Parried && !hit.CanParry)
                result = IncomingHitResult.Blocked;

            if (_log)
            {
                float damage = result switch
                {
                    IncomingHitResult.Parried => 0f,
                    IncomingHitResult.Blocked => hit.BlockedDamage,
                    _ => hit.Damage
                };

                Debug.Log($"[PlayerHit stub] {(hit.Source != null ? hit.Source.name : "?")}: {result}, damage {damage}", this);
            }

            return result;
        }
    }
}
