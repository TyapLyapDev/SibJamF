using System;
using SibJam.Combat;

namespace _Project.Scripts.Enemies
{
    public enum AttackPhase
    {
        None,
        Windup,
        Active,
        Recovery
    }

    // Атака врага: Windup (подсказка игроку) -> Active (урон) -> Recovery
    public interface IEnemyAttack
    {
        event Action Parried;

        AttackPhase Phase { get; }
        bool IsRunning { get; }

        void Init(EnemyConfig config, float statsMultiplier);
        // Может ли атака достать цель с текущей позиции (для стрелка — прямая видимость). Если нет, враг подходит ближе
        bool CanReach(IEnemyTarget target);
        bool CanStart(IEnemyTarget target);
        void Begin(IEnemyTarget target);
        void Tick(float deltaTime);
        void Cancel();
    }
}
