using UnityEngine;

namespace SibJam.Weapons
{
    // Всё, по чему можно ударить мечом, кроме врагов и камер: генераторы, щит босса
    public interface IMeleeHitTarget
    {
        void TakeMeleeHit(Vector3 point);
    }
}
