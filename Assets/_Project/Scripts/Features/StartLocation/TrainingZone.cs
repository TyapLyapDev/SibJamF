using System;
using SibJam.Combat;
using SibJam.Locations;
using UnityEngine;

namespace SibJam.StartLocation
{
    // Где заканчивается тренировка: рядом с манекенами, на их этаже и за первой дверью
    public sealed class TrainingZone : MonoBehaviour
    {
        [SerializeField] private PlayerTarget _player;
        [SerializeField] private Transform[] _anchors = new Transform[0];
        [Tooltip("Первая дверь стоит поперёк оси Z, зона начинается за ней в +Z")]
        [SerializeField] private LocationDoor _entranceDoor;
        [SerializeField, Min(0f)] private float _entranceOffset = 0.15f;
        [SerializeField, Min(0f)] private float _radius = 20f;
        [SerializeField, Min(0f)] private float _belowAnchor = 1.5f;
        [SerializeField, Min(0f)] private float _aboveAnchor = 2.5f;

        private float _entranceZ;

        public event Action InsideChanged;

        public bool IsPlayerInside { get; private set; }

        // Дверь уезжает вверх, но z запоминаем заранее: откатная сместила бы границу
        private void Awake() => _entranceZ = _entranceDoor.transform.position.z + _entranceOffset;

        private void Update()
        {
            bool inside = Check();
            if (inside == IsPlayerInside)
                return;

            IsPlayerInside = inside;
            InsideChanged?.Invoke();
        }

        private bool Check()
        {
            if (!_player.isActiveAndEnabled)
                return false;

            Vector3 position = _player.Position;
            if (position.z < _entranceZ)
                return false;

            foreach (Transform anchor in _anchors)
            {
                Vector3 offset = anchor.position - position;
                bool near = new Vector2(offset.x, offset.z).sqrMagnitude <= _radius * _radius;
                bool sameFloor = position.y >= anchor.position.y - _belowAnchor
                    && position.y < anchor.position.y + _aboveAnchor;

                if (near && sameFloor)
                    return true;
            }

            return false;
        }
    }
}
