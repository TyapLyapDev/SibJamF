using System.Collections.Generic;
using SibJam.Combat;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Снаряд стрелка: летит по прямой, проверка попадания свипом сферы (без Rigidbody, не пролетает сквозь стены).
    // Отбивается мечом по нажатию удара (см. MeleeWeapon): разворачивается обратно в стрелка и бьёт уже его.
    // В блок снаряд просто упирается и снимает прочность блока
    public class Projectile : MonoBehaviour
    {
        private static readonly List<Projectile> _active = new();

        public static IReadOnlyList<Projectile> Active => _active;

        [SerializeField, Min(0.01f)] private float _radius = 0.15f;
        [SerializeField, Min(0.1f)] private float _lifetime = 5f;
        [SerializeField] private LayerMask _hitMask = ~0;
        [Tooltip("Во сколько раз быстрее летит отбитый снаряд")]
        [SerializeField, Min(1f)] private float _reflectSpeedMultiplier = 1.5f;

        private readonly RaycastHit[] _hits = new RaycastHit[8];

        private float _damage;
        private float _speed;
        private Vector3 _direction;
        private GameObject _source;
        private float _timer;
        private bool _isReflected;

        public bool IsReflected => _isReflected;
        public Vector3 Direction => _direction;
        public float Speed => _speed;

        private void OnEnable() => _active.Add(this);

        private void OnDisable() => _active.Remove(this);

        public void Launch(Vector3 direction, float speed, float damage, GameObject source)
        {
            _direction = direction.normalized;
            _speed = speed;
            _damage = damage;
            _source = source;
            _timer = _lifetime;
            _isReflected = false;
            transform.rotation = Quaternion.LookRotation(_direction);
        }

        private void Update()
        {
            var deltaTime = Time.deltaTime;

            _timer -= deltaTime;
            if (_timer <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            var step = _speed * deltaTime;

            if (TryFindHit(step, out var hit))
            {
                OnHit(hit);
                return;
            }

            transform.position += _direction * step;
        }

        private bool TryFindHit(float distance, out RaycastHit closest)
        {
            closest = default;
            var count = Physics.SphereCastNonAlloc(transform.position, _radius, _direction, _hits, distance, _hitMask, QueryTriggerInteraction.Ignore);
            var best = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var hit = _hits[i];

                // Сам стрелок не мешает своему выстрелу, а отбитый снаряд не цепляет игрока
                if (IsOwn(hit.collider) || hit.distance >= best) continue;

                best = hit.distance;
                closest = hit;
            }

            return best < float.MaxValue;
        }

        private bool IsOwn(Collider collider)
        {
            if (_isReflected)
                return collider.GetComponentInParent<PlayerTarget>() != null;

            return _source != null && collider.transform.IsChildOf(_source.transform);
        }

        private void OnHit(RaycastHit hit)
        {
            transform.position = hit.point;

            if (_isReflected)
            {
                var enemy = hit.collider.GetComponentInParent<Enemy>();
                if (enemy != null && !enemy.IsDead)
                    enemy.TakeDamage(_damage);

                Destroy(gameObject);
                return;
            }

            // Окном парирования снаряд не отбить — только ударом; блок его просто держит
            hit.collider.GetComponentInParent<IDamageable>()?.TakeHit(new HitInfo(_damage, _source, _direction, parryable: false));

            Destroy(gameObject);
        }

        public void Reflect(float damageMultiplier)
        {
            if (_isReflected)
                return;

            _isReflected = true;
            _damage *= damageMultiplier;
            _speed *= _reflectSpeedMultiplier;
            _timer = _lifetime;

            var target = _source != null ? _source.transform.position + Vector3.up : transform.position - _direction;
            _direction = (target - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(_direction);
        }
    }
}
