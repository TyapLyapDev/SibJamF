using UnityEngine;

namespace SibJam.TV
{
    // Зона комнаты для телика: коллайдер-триггер на этом объекте сообщает телику о входе и выходе игрока
    [RequireComponent(typeof(Collider))]
    public class TVZone : MonoBehaviour
    {
        [SerializeField] private TVReactions _tv;
        [SerializeField] private string _playerTag = "Player";

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            _tv = GetComponentInParent<TVReactions>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _tv.OnPlayerEntered();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _tv.OnPlayerExited();
        }
    }
}
