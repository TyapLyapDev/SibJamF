using UnityEngine;

namespace SibJam.Waves
{
    public class WavesTrigger : MonoBehaviour
    {
        [SerializeField] private WavesLifeCycle _wavesLifeCycle;
        [SerializeField] private string _playerTag = "Player";

        private void OnTriggerEnter(Collider other) => Trigger(other.gameObject);

        private void OnTriggerEnter2D(Collider2D other) => Trigger(other.gameObject);

        private void Trigger(GameObject other)
        {
            if (!other.CompareTag(_playerTag)) return;
            if (_wavesLifeCycle.IsRunning) return;

            _wavesLifeCycle.LaunchWaves();
            gameObject.SetActive(false);
        }
    }
}
