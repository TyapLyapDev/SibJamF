using UnityEngine;
using UnityEngine.Audio;

namespace SibJam
{
    // Зацикленный объёмный звук предмета окружения: пар из труб, гул вентиляции, жужжание камер.
    // Стартует со случайного места и высоты, чтобы одинаковые объекты не звучали в унисон
    [DisallowMultipleComponent]
    public class AmbientLoop : MonoBehaviour
    {
        [SerializeField] private AudioClip _clip;
        [SerializeField] private AudioMixerGroup _output;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.5f;
        [SerializeField] private Vector2 _pitch = new Vector2(0.92f, 1.08f);
        [SerializeField, Min(0.1f)] private float _minDistance = 1.5f;
        [SerializeField, Min(1f)] private float _maxDistance = 12f;
        [Tooltip("Срез верхов, Гц: убирает шипение и треск записи. 22000 — без среза")]
        [SerializeField, Range(500f, 22000f)] private float _lowPass = 2500f;

        private AudioSource _source;

        private void Awake()
        {
            // Свой дочерний объект: фильтр не должен задевать другие звуки этого объекта (удар по камере и т.п.)
            var holder = new GameObject("AmbientLoopSource");
            holder.transform.SetParent(transform, false);
            _source = holder.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.clip = _clip;
            _source.outputAudioMixerGroup = _output;
            _source.volume = _volume;
            _source.pitch = Random.Range(_pitch.x, _pitch.y);
            _source.spatialBlend = 1f;
            _source.rolloffMode = AudioRolloffMode.Linear;
            _source.minDistance = _minDistance;
            _source.maxDistance = _maxDistance;
            _source.dopplerLevel = 0f;

            if (_lowPass < 21000f)
                holder.AddComponent<AudioLowPassFilter>().cutoffFrequency = _lowPass;
        }

        private void OnEnable()
        {
            if (_source == null || _clip == null)
                return;

            _source.time = Random.Range(0f, _clip.length * 0.95f);
            _source.Play();
        }

        private void OnDisable()
        {
            if (_source != null)
                _source.Stop();
        }

        public void Setup(AudioClip clip, AudioMixerGroup output, float volume, float minDistance, float maxDistance)
        {
            _clip = clip;
            _output = output;
            _volume = volume;
            _minDistance = minDistance;
            _maxDistance = maxDistance;
        }
    }
}
