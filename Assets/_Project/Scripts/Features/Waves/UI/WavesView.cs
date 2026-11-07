using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SibJam.Waves.UI
{
    public class WavesView : MonoBehaviour
    {
        [SerializeField] private CurrentLocationInfo _currentLocation;
        [SerializeField] private HudPanel _panel;
        [SerializeField] private GameObject _enemiesGroup;
        [SerializeField] private TMP_Text _enemiesText;
        [SerializeField] private GameObject _delayGroup;
        [SerializeField] private TMP_Text _delayText;
        [SerializeField] private Image _delayFill;

        private IWavesLoopInfo _waves;
        private float _delayDuration;

        private void Start() => Bind();

        private void OnEnable()
        {
            _currentLocation.LocationChanged += Bind;
            _currentLocation.ActiveChanged += RefreshVisibility;

            Bind();
        }

        private void OnDisable()
        {
            _currentLocation.LocationChanged -= Bind;
            _currentLocation.ActiveChanged -= RefreshVisibility;
            Unbind();
        }

        private void Update()
        {
            if (_waves == null) return;

            switch (_waves.State)
            {
                case WavesLoopState.Wave:
                case WavesLoopState.WaitingForEnemiesKilled:
                    _enemiesText.text = $"× {_waves.AliveEnemiesCount}";
                    break;

                case WavesLoopState.Delay:
                    _delayText.text = $"Следующая волна через {Mathf.CeilToInt(_waves.DelayTimeLeft)}";
                    if (_delayFill != null && _delayDuration > 0f)
                        _delayFill.fillAmount = _waves.DelayTimeLeft / _delayDuration;
                    break;
            }
        }

        private void Bind()
        {
            Unbind();

            _waves = _currentLocation.Waves;
            if (_waves == null)
            {
                _panel.SetVisible(false);
                return;
            }
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
            _enemiesGroup.SetActive(state is WavesLoopState.Wave or WavesLoopState.WaitingForEnemiesKilled);
            _delayGroup.SetActive(state == WavesLoopState.Delay);

            if (state == WavesLoopState.Delay)
                _delayDuration = Mathf.Max(_waves.DelayTimeLeft, 0.01f);

            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            var hasContent = _waves != null && _waves.State is WavesLoopState.Wave or WavesLoopState.WaitingForEnemiesKilled or WavesLoopState.Delay;
            _panel.SetVisible(hasContent && _currentLocation.IsActive);
        }
    }
}
