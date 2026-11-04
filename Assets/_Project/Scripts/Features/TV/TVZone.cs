using UnityEngine;

namespace SibJam.TV
{
    // Зона комнаты с теликами: коллайдер-триггер на этом объекте сообщает комнате о входе и выходе игрока
    [RequireComponent(typeof(Collider))]
    public class TVZone : MonoBehaviour
    {
        [SerializeField] private TVRoom _room;
        [SerializeField] private string _playerTag = "Player";

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            _room = GetComponentInParent<TVRoom>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _room.OnPlayerEntered();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _room.OnPlayerExited();
        }
    }
}
