using System.Collections.Generic;
using System.Linq;
using _Project.Scripts.Enemies;

namespace SibJam.Waves
{
    public class Wave
    {
        private readonly List<Enemy> _enemies = new();

        public void BindEnemy(Enemy enemy) => _enemies.Add(enemy);

        public int AliveCount => _enemies.Count(enemy => enemy != null && !enemy.IsDead);

        public bool IsCompleted => AliveCount == 0;
    }
}
