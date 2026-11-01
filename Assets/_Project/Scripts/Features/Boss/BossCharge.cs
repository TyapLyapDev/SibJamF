using SibJam.Combat;
using UnityEngine;

namespace SibJam.Boss
{
    // Заряд из кольца: летит по прямой, бьёт игрока при касании, гаснет о стены и коробки или по времени
    public class BossCharge : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float _speed = 8f;
        [SerializeField, Min(0.1f)] private float _lifetime = 4f;
        [SerializeField, Min(0.1f)] private float _hitRadius = 1f;
        [SerializeField, Min(0.05f)] private float _wallRadius = 0.3f;
        [SerializeField] private LayerMask _wallMask = ~0;

        private static readonly Collider[] Overlaps = new Collider[8];

        private Transform _player;
        private IPlayerHitReceiver _receiver;
        private Transform _owner;
        private float _damage;
        private float _blockedDamage;
        private float _age;

        public void Launch(Vector3 direction, Transform player, IPlayerHitReceiver receiver, Transform owner, float damage, float blockedDamage)
        {
            transform.rotation = Quaternion.LookRotation(direction);
            _player = player;
            _receiver = receiver;
            _owner = owner;
            _damage = damage;
            _blockedDamage = blockedDamage;
        }

        private void Update()
        {
            _age += Time.deltaTime;
            transform.position += transform.forward * (_speed * Time.deltaTime);

            if (_age >= _lifetime || HitWall())
            {
                Destroy(gameObject);
                return;
            }

            if (_player == null || _receiver == null)
                return;

            // Игрок — капсула ростом около 2 м: сравниваем с её центром
            Vector3 playerCenter = _player.position + Vector3.up;

            if ((playerCenter - transform.position).sqrMagnitude <= _hitRadius * _hitRadius)
            {
                _receiver.ReceiveHit(new PlayerHit(_damage, _blockedDamage, canParry: false, source: _owner));
                Destroy(gameObject);
            }
        }

        private bool HitWall()
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, _wallRadius, Overlaps, _wallMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Transform other = Overlaps[i].transform;

                if ((_owner == null || !other.IsChildOf(_owner)) && (_player == null || !other.IsChildOf(_player)))
                    return true;
            }

            return false;
        }
    }
}
