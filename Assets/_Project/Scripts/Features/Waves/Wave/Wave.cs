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

        public int TotalCount => _enemies.Count;

        public bool IsCompleted => AliveCount == 0;

        // Недобитые враги прошлой волны переходят в новую, чтобы их по-прежнему ждали и добивали
        public void Absorb(Wave other)
        {
            if (other == null) return;

            _enemies.AddRange(other._enemies.Where(enemy => enemy != null && !enemy.IsDead));
        }

        public void KillAll()
        {
            foreach (Enemy enemy in _enemies.ToList())
            {
                if (enemy != null && !enemy.IsDead)
                    enemy.Die();
            }
        }
    }
}
