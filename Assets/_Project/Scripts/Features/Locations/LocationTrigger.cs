using UnityEngine;

namespace SibJam.Locations
{
    public class LocationTrigger : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private LocationLoop _locationLoop;
        [SerializeField] private string _playerTag = "Player";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(_playerTag)) return;
            if (_locationLoop.State != LocationLoopState.Idle) return;
            _gameLoop.LaunchLocation(_locationLoop);
            gameObject.SetActive(false);
        }
    }
}
