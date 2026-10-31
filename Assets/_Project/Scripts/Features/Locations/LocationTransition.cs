using SibJam.Waves;
using UnityEngine;

namespace SibJam.Locations
{
    public class LocationTransition : MonoBehaviour
    {
        [SerializeField] private WavesLoop _fromLocation;
        [SerializeField] private Transform _player;
        [SerializeField] private Transform _targetPoint;

        private void OnEnable() => _fromLocation.Completed += MovePlayer;

        private void OnDisable() => _fromLocation.Completed -= MovePlayer;

        private void MovePlayer()
        {
            if (_player.TryGetComponent(out Rigidbody body))
                body.position = _targetPoint.position;

            _player.position = _targetPoint.position;
            Physics.SyncTransforms();
        }
    }
}
