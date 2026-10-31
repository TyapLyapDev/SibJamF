using UnityEngine;

namespace SibJam.Waves
{
    public class WavesTrigger : MonoBehaviour
    {
        [SerializeField] private WavesLoop _wavesLoop;
        [SerializeField] private string _playerTag = "Player";

        private void OnTriggerEnter(Collider other) => Trigger(other.gameObject);

        private void OnTriggerEnter2D(Collider2D other) => Trigger(other.gameObject);

        private void Trigger(GameObject other)
        {
            if (!other.CompareTag(_playerTag)) return;
            if (_wavesLoop.IsRunning) return;

            _wavesLoop.LaunchWaves();
            gameObject.SetActive(false);
        }
    }
}
