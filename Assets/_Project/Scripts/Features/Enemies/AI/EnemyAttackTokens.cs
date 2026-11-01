using System.Collections.Generic;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Ограничивает число врагов, атакующих одновременно, чтобы толпа не била разом
    public static class EnemyAttackTokens
    {
        private static readonly HashSet<object> Holders = new();

        public static int MaxAttackers { get; set; } = 2;

        public static bool TryTake(object holder)
        {
            if (Holders.Contains(holder)) return true;
            if (Holders.Count >= MaxAttackers) return false;

            Holders.Add(holder);
            return true;
        }

        public static void Release(object holder) => Holders.Remove(holder);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Holders.Clear();
    }
}
