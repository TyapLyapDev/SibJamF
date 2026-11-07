using System;
using SibJam.Combat;
using UnityEngine;

namespace SibJam.StartLocation
{
    // Триггер за первой дверью: сообщает, что игрок вошёл на тренировку.
    [RequireComponent(typeof(Collider))]
    public sealed class TrainingEntryTrigger : MonoBehaviour
    {
        public event Action Entered;

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerTarget>();
            if (player != null && player.isActiveAndEnabled)
                Entered?.Invoke();
        }
    }
}
