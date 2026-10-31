using System;
using UnityEngine;

namespace SibJam.Weapons
{
    public class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private Behaviour _weapon;
        [SerializeField] private string _playerTag = "Player";
        [SerializeField, Min(0f)] private float _spinSpeed = 90f;

        public event Action PickedUp;

        public bool IsPickedUp { get; private set; }

        private void Awake() => _weapon.enabled = false;

        private void Update() => transform.Rotate(Vector3.up, _spinSpeed * Time.deltaTime, Space.World);

        private void OnTriggerEnter(Collider other)
        {
            if (IsPickedUp || !other.CompareTag(_playerTag)) return;

            IsPickedUp = true;
            _weapon.enabled = true;
            PickedUp?.Invoke();
            Destroy(gameObject);
        }
    }
}
