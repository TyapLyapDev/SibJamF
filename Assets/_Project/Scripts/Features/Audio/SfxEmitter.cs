using System;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

namespace SibJam
{
    // Набор вариантов одного звука: берётся случайный клип со случайной высотой и громкостью из диапазонов
    [Serializable]
    public class SoundSet
    {
        public AudioClip[] Clips;
        public Vector2 Volume = new Vector2(0.9f, 1f);
        public Vector2 Pitch = new Vector2(0.95f, 1.05f);

        public bool IsEmpty => Clips == null || Clips.Length == 0;

        public AudioClip Pick() => IsEmpty ? null : Clips[Random.Range(0, Clips.Length)];
    }

    // Объёмный звук объекта: несколько источников по кругу, чтобы у каждого выстрела была своя высота,
    // и отдельный источник для зацикленного звука (гул, треск)
    public class SfxEmitter : MonoBehaviour
    {
        [SerializeField] private AudioMixerGroup _output;
        [SerializeField, Range(1, 8)] private int _voices = 4;
        [SerializeField, Range(0f, 1f)] private float _spatialBlend = 1f;
        [SerializeField, Min(1f)] private float _minDistance = 3f;
        [SerializeField, Min(1f)] private float _maxDistance = 45f;

        private AudioSource[] _sources;
        private AudioSource _loop;
        private int _next;

        private void Awake()
        {
            _sources = new AudioSource[_voices];

            for (int i = 0; i < _voices; i++)
                _sources[i] = CreateSource();

            _loop = CreateSource();
            _loop.loop = true;
        }

        public void Play(SoundSet sound, float delay = 0f)
        {
            if (sound == null || sound.IsEmpty || _sources == null)
                return;

            AudioSource source = _sources[_next];
            _next = (_next + 1) % _sources.Length;
            source.Stop();
            source.clip = sound.Pick();
            source.volume = Random.Range(sound.Volume.x, sound.Volume.y);
            source.pitch = Random.Range(sound.Pitch.x, sound.Pitch.y);
            source.PlayDelayed(delay);
        }

        public void PlayLoop(SoundSet sound)
        {
            if (sound == null || sound.IsEmpty || _loop == null)
                return;

            _loop.clip = sound.Pick();
            _loop.volume = Random.Range(sound.Volume.x, sound.Volume.y);
            _loop.pitch = Random.Range(sound.Pitch.x, sound.Pitch.y);
            _loop.Play();
        }

        public void StopLoop()
        {
            if (_loop != null)
                _loop.Stop();
        }

        public void StopAll()
        {
            StopLoop();

            if (_sources == null)
                return;

            foreach (AudioSource source in _sources)
                source.Stop();
        }

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.outputAudioMixerGroup = _output;
            source.spatialBlend = _spatialBlend;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = _minDistance;
            source.maxDistance = _maxDistance;
            source.dopplerLevel = 0f;
            return source;
        }
    }
}
