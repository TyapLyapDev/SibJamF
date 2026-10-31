using TMPro;
using UnityEngine;

namespace SibJam.Waves.UI
{
    public class WavesView : MonoBehaviour
    {
        [SerializeField] private CurrentLocationInfo _currentLocation;
        [SerializeField] private TMP_Text _enemiesText;
        [SerializeField] private TMP_Text _delayText;

        private IWavesLoopInfo _waves;

        private void Start() => Bind();

        private void OnEnable()
        {
            _currentLocation.LocationChanged += Bind;

            if (_waves != null)
                Bind();
        }

        private void OnDisable()
        {
            _currentLocation.LocationChanged -= Bind;
            Unbind();
        }

        private void Update()
        {
            if (_waves == null) return;

            switch (_waves.State)
            {
                case WavesLoopState.Wave:
                case WavesLoopState.WaitingForEnemiesKilled:
                    _enemiesText.text = $"Врагов осталось: {_waves.AliveEnemiesCount}";
                    break;

                case WavesLoopState.Delay:
                    _delayText.text = $"Следующая волна через: {Mathf.CeilToInt(_waves.DelayTimeLeft)}";
                    break;
            }
        }

        private void Bind()
        {
            Unbind();

            _waves = _currentLocation.Waves;
            _waves.StateChanged += OnStateChanged;
            OnStateChanged(_waves.State);
        }

        private void Unbind()
        {
            if (_waves != null)
                _waves.StateChanged -= OnStateChanged;

            _waves = null;
        }

        private void OnStateChanged(WavesLoopState state)
        {
            _enemiesText.gameObject.SetActive(state is WavesLoopState.Wave or WavesLoopState.WaitingForEnemiesKilled);
            _delayText.gameObject.SetActive(state == WavesLoopState.Delay);
        }
    }
}
