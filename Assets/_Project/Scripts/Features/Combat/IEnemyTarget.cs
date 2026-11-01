using UnityEngine;

namespace SibJam.Combat
{
    // То, за чем охотятся враги: позиция, точка прицеливания для стрелков и приём урона
    public interface IEnemyTarget : IDamageable
    {
        Vector3 Position { get; }
        Vector3 AimPoint { get; }
        bool IsAlive { get; }
    }
}
