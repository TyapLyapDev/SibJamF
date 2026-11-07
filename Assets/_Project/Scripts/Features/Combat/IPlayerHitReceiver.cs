using SibJam.Weapons;

namespace SibJam.Combat
{
    // Удар по игроку. BlockedDamage < 0 — блоком не снимается (например, полоса под током)

    // Точка подключения системы здоровья игрока: бой вызывает ReceiveHit, а как считать здоровье и смерть — решает реализация.
    // Компонент с этим интерфейсом вешается на объект игрока, атаки находят его через GetComponent
    public interface IPlayerHitReceiver
    {
        // Возвращает, чем закончился удар: от этого зависит реакция атакующего (парирование оглушает босса)
        IncomingHitResult ReceiveHit(PlayerHit hit);
    }
}
