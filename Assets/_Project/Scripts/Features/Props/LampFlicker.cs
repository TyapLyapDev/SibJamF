using UnityEngine;

namespace SibJam.Props
{
    // Мерцание лампы: редкие короткие провалы яркости, свет и свечение панели гаснут вместе
    public class LampFlicker : MonoBehaviour
    {
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Light _light;
        [SerializeField] private Renderer _panel;
        [SerializeField] private Color _panelEmission = new Color(1f, 0.85f, 0.65f) * 2f;
        [SerializeField] private Vector2 _calmTime = new Vector2(1.5f, 6f);
        [SerializeField] private Vector2 _flickerTime = new Vector2(0.15f, 0.6f);
        [SerializeField, Range(0f, 1f)] private float _minBrightness = 0.05f;

        private MaterialPropertyBlock _block;
        private float _baseIntensity;
        private float _timer;
        private bool _isFlickering;

        private void Start()
        {
            _baseIntensity = _light != null ? _light.intensity : 1f;
            _timer = Random.Range(_calmTime.x, _calmTime.y);
        }

        private void Update()
        {
            _timer -= Time.deltaTime;

            if (_timer <= 0f)
            {
                _isFlickering = !_isFlickering;
                _timer = _isFlickering ? Random.Range(_flickerTime.x, _flickerTime.y) : Random.Range(_calmTime.x, _calmTime.y);
            }

            float brightness = _isFlickering && Random.value < 0.6f ? Random.Range(_minBrightness, 0.5f) : 1f;
            SetBrightness(brightness);
        }

        private void SetBrightness(float brightness)
        {
            if (_light != null)
                _light.intensity = _baseIntensity * brightness;

            if (_panel == null)
                return;

            _block ??= new MaterialPropertyBlock();
            _panel.GetPropertyBlock(_block);
            _block.SetColor(EmissionColorId, _panelEmission * brightness);
            _panel.SetPropertyBlock(_block);
        }
    }
}
