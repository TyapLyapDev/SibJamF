using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Combat
{
    // Удар по игроку. BlockedDamage < 0 — блоком не снимается (например, полоса под током)
    public readonly struct PlayerHit
    {
        public PlayerHit(float damage, float blockedDamage = -1f, bool canParry = false, Object source = null)
        {
            Damage = damage;
            BlockedDamage = blockedDamage;
            CanParry = canParry;
            Source = source;
        }

        public float Damage { get; }
        public float BlockedDamage { get; }
        public bool CanParry { get; }
        public Object Source { get; }

        public bool CanBlock => BlockedDamage >= 0f;
    }

    // Точка подключения системы здоровья игрока: бой вызывает ReceiveHit, а как считать здоровье и смерть — решает реализация.
    // Компонент с этим интерфейсом вешается на объект игрока, атаки находят его через GetComponent
    public interface IPlayerHitReceiver
    {
        // Возвращает, чем закончился удар: от этого зависит реакция атакующего (парирование оглушает босса)
        IncomingHitResult ReceiveHit(PlayerHit hit);
    }
}
