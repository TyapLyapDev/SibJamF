using System;
using UnityEngine;

namespace SibJam.Locations
{
    public class PassageZone : MonoBehaviour
    {
        [SerializeField] private string _playerTag = "Player";

        public event Action Entered;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
                Entered?.Invoke();
        }
    }
}
