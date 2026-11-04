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

    // Экран телевизора: выводит рожу из набора на материал экрана (шейдер SibJam/TVScreen). Порядок _faces совпадает с TVFace.
    // В простое показывает _idleFace; на реплику катсцены (TVLineAction) ставит лицо реплики и «говорит»
    public class TVScreen : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int GlitchId = Shader.PropertyToID("_Glitch");
        private static readonly int TalkId = Shader.PropertyToID("_Talk");

        [SerializeField] private Renderer _renderer;
        [Tooltip("Индекс материала экрана в _renderer")]
        [SerializeField, Min(0)] private int _screenMaterial = 1;
        [SerializeField] private Texture2D[] _faces;
        [Tooltip("Лицо в простое и после реплики")]
        [SerializeField, UnityEngine.Serialization.FormerlySerializedAs("_startFace")] private TVFace _idleFace = TVFace.Sleepy;
        [Tooltip("Откуда звучат реплики этого телика: 3D-источник на нём")]
        [SerializeField] private AudioSource _voice;
        [SerializeField, ColorUsage(false, true)] private Color _tint = new Color(1.6f, 1.6f, 1.6f);
        [Tooltip("Лёгкое мерцание яркости, как у старого кинескопа")]
        [SerializeField, Range(0f, 0.5f)] private float _flicker = 0.06f;
        [Tooltip("Сколько секунд идёт помеха при смене лица")]
        [SerializeField, Min(0f)] private float _glitchTime = 0.18f;

        private MaterialPropertyBlock _block;
        private bool _isOn;
        private float _glitch;
        private float _talk;

        public TVFace Face { get; private set; }
        public bool IsTalking { get; private set; }
        public AudioSource Voice => _voice;

        public TVFace IdleFace
        {
            get => _idleFace;
            set
            {
                _idleFace = value;

                if (!IsTalking && _block != null)
                    Show(_idleFace);
            }
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            Show(_idleFace, glitch: false);
        }

        private void Update()
        {
            _glitch = _glitchTime > 0f ? Mathf.MoveTowards(_glitch, 0f, Time.deltaTime / _glitchTime) : 0f;
            _talk = Mathf.MoveTowards(_talk, IsTalking ? 1f : 0f, Time.deltaTime * 6f);
            Apply();
        }

        public void Show(TVFace face, bool glitch = true)
        {
            if (glitch && face != Face)
                _glitch = 1f;

            Face = face;
            int index = (int)face;
            _isOn = index >= 0 && index < _faces.Length && _faces[index] != null;

            _renderer.GetPropertyBlock(_block, _screenMaterial);

            if (_isOn)
                _block.SetTexture(BaseMapId, _faces[index]);

            _renderer.SetPropertyBlock(_block, _screenMaterial);
            Apply();
        }

        public void TurnOff() => Show(TVFace.Off);

        // Реплика началась: своё лицо и покачивание, колонки пульсируют
        public void BeginTalk(TVFace face)
        {
            Show(face);
            IsTalking = true;
        }

        // Реплика кончилась: назад к лицу простоя
        public void EndTalk()
        {
            if (!IsTalking)
                return;

            IsTalking = false;
            Show(_idleFace);
        }

        private void Apply()
        {
            float brightness = 1f - _flicker * Mathf.PerlinNoise(Time.time * 12f, 0f);

            _renderer.GetPropertyBlock(_block, _screenMaterial);
            _block.SetColor(BaseColorId, _isOn ? _tint * brightness : Color.black);
            _block.SetFloat(GlitchId, _glitch);
            _block.SetFloat(TalkId, _talk);
            _renderer.SetPropertyBlock(_block, _screenMaterial);
        }
    }
}
