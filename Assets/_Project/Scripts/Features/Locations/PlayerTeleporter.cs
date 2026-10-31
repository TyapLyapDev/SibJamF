using UnityEngine;

namespace SibJam.Locations
{
    public class PlayerTeleporter : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private CharacterController _player;

        private void OnEnable() => _gameLoop.LocationChanged += OnLocationChanged;

        private void OnDisable() => _gameLoop.LocationChanged -= OnLocationChanged;

        private void OnLocationChanged(LocationLoop location)
        {
            if (location.ArrivalPoint != null)
                Teleport(location.ArrivalPoint);
        }

        private void Teleport(Transform targetPoint)
        {
            _player.enabled = false;
            _player.transform.SetPositionAndRotation(targetPoint.position, targetPoint.rotation);
            _player.enabled = true;
        }
    }
}
