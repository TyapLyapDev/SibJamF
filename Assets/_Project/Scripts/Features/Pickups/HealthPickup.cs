using SibJam.Player;
using UnityEngine;

namespace SibJam.Pickups
{
    // Аптечка: лечит игрока при входе в триггер. Если хп полное — не подбирается
    [RequireComponent(typeof(Collider))]
    public class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _healAmount = 25f;
        [Tooltip("Скорость вращения визуала, град/с. 0 — не вращать")]
        [SerializeField] private float _spinSpeed = 90f;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Update()
        {
            if (_spinSpeed != 0f)
                transform.Rotate(0f, _spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other) => TryPick(other);

        private void OnTriggerStay(Collider other) => TryPick(other);

        private void TryPick(Collider other)
        {
            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null || health.IsDead || health.Current >= health.Max) return;

            health.Heal(_healAmount);
            Destroy(gameObject);
        }
    }
}
