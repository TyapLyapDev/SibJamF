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
        bool CanStart(IEnemyTarget target);
        void Begin(IEnemyTarget target);
        void Tick(float deltaTime);
        void Cancel();
    }
}
