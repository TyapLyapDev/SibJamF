using UnityEngine;

namespace SibJam.TV
{
    // Колонки телика пульсируют, как в мультиках, пока он говорит: в такт громкости голоса, а без звука — просто ритмично
    public class TVSpeakers : MonoBehaviour
    {
        [SerializeField] private TVScreen _screen;
        [SerializeField] private Transform[] _speakers;
        [Tooltip("Насколько колонка раздувается на громком звуке")]
        [SerializeField, Range(0f, 0.5f)] private float _amount = 0.18f;
        [Tooltip("Громкость голоса (RMS), на которой пульс максимальный")]
        [SerializeField, Min(0.001f)] private float _fullLoudness = 0.12f;
        [Tooltip("Частота пульса, когда звука нет или он тихий")]
        [SerializeField, Min(0f)] private float _fallbackRate = 9f;
        [SerializeField, Min(0.1f)] private float _response = 25f;

        private readonly float[] _samples = new float[256];
        private Vector3[] _restScales;
        private float _pulse;

        private void Awake()
        {
            _restScales = new Vector3[_speakers.Length];

            for (int i = 0; i < _speakers.Length; i++)
                _restScales[i] = _speakers[i].localScale;
        }

        private void Update()
        {
            float target = 0f;

            if (_screen.IsTalking)
            {
                float loudness = ReadLoudness();
                target = loudness > 0.005f
                    ? Mathf.Clamp01(loudness / _fullLoudness)
                    : 0.5f + 0.5f * Mathf.Sin(Time.time * _fallbackRate * Mathf.PI * 2f);
            }

            _pulse = Mathf.Lerp(_pulse, target, 1f - Mathf.Exp(-_response * Time.deltaTime));

            // Мультяшный «бум»: раздувается вширь, чуть сплющивается по высоте
            float grow = _pulse * _amount;
            var squash = new Vector3(1f + grow, 1f - grow * 0.5f, 1f + grow);

            for (int i = 0; i < _speakers.Length; i++)
                _speakers[i].localScale = Vector3.Scale(_restScales[i], squash);
        }

        private float ReadLoudness()
        {
            AudioSource voice = _screen.Voice;

            if (voice == null)
                return 0f;

            voice.GetOutputData(_samples, 0);
            float sum = 0f;

            foreach (float sample in _samples)
                sum += sample * sample;

            return Mathf.Sqrt(sum / _samples.Length);
        }
    }
}
