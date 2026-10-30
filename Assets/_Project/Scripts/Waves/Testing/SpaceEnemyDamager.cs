using _Project.Scripts.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam.Waves.Testing
{
    public class SpaceEnemyDamager : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _damage = 100f;

        private Enemy _target;

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame) return;

            if (_target == null || _target.IsDead)
                _target = FindAliveEnemy();

            if (_target != null)
                _target.TakeDamage(_damage);
        }

        private static Enemy FindAliveEnemy()
        {
            foreach (var enemy in FindObjectsByType<Enemy>())
            {
                if (!enemy.IsDead)
                    return enemy;
            }

            return null;
        }
    }
}
