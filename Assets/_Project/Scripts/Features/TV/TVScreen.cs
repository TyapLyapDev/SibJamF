using UnityEngine;

namespace SibJam.TV
{
    public enum TVFace
    {
        Off = -1,
        Evil,
        Bored,
        Scared,
        Smug,
        Angry,
        Sad,
        Sleepy,
        Crazy,
        Mad,
    }

    // Экран телевизора: выводит рожу из набора на материал экрана. Порядок _faces совпадает с TVFace
    public class TVScreen : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _renderer;
        [Tooltip("Индекс материала экрана в _renderer")]
        [SerializeField, Min(0)] private int _screenMaterial = 1;
        [SerializeField] private Texture2D[] _faces;
        [SerializeField] private TVFace _startFace = TVFace.Sleepy;
        [SerializeField, ColorUsage(false, true)] private Color _tint = new Color(1.6f, 1.6f, 1.6f);
        [Tooltip("Лёгкое мерцание яркости, как у старого кинескопа")]
        [SerializeField, Range(0f, 0.5f)] private float _flicker = 0.06f;

        private MaterialPropertyBlock _block;
        private bool _isOn;

        public TVFace Face { get; private set; }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            Show(_startFace);
        }

        private void Update()
        {
            if (_isOn && _flicker > 0f)
                Apply(1f - _flicker * Mathf.PerlinNoise(Time.time * 12f, 0f));
        }

        public void Show(TVFace face)
        {
            Face = face;
            int index = (int)face;
            _isOn = index >= 0 && index < _faces.Length && _faces[index] != null;

            _renderer.GetPropertyBlock(_block, _screenMaterial);

            if (_isOn)
                _block.SetTexture(BaseMapId, _faces[index]);

            _renderer.SetPropertyBlock(_block, _screenMaterial);
            Apply(1f);
        }

        public void TurnOff() => Show(TVFace.Off);

        private void Apply(float brightness)
        {
            _renderer.GetPropertyBlock(_block, _screenMaterial);
            _block.SetColor(BaseColorId, _isOn ? _tint * brightness : Color.black);
            _renderer.SetPropertyBlock(_block, _screenMaterial);
        }
    }
}
