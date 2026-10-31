using System;
using System.Collections.Generic;
using UnityEngine;

namespace SibJam
{
    public class UiSfxService : MonoBehaviour
    {
        [SerializeField] private AudioSource _sourcePrefab;
        [SerializeField] private int _poolSize = 4;
        [SerializeField] private AudioClip _menuOpen;
        [SerializeField] private AudioClip _menuClose;

        private readonly List<AudioSource> _sources = new();
        private int _nextIndex;

        private void Awake()
        {
            for (int i = 0; i < _poolSize; i++)
            {
                AudioSource source = Instantiate(_sourcePrefab, transform);
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                source.loop = false;
                _sources.Add(source);
            }
        }

        public void Play(AudioClip clip)
        {
            if (clip == null) 
                return;

            AudioSource target = null;

            for (int i = 0; i < _sources.Count; i++)
            {
                int index = (_nextIndex + i) % _sources.Count;

                if (_sources[index].isPlaying == false)
                {
                    target = _sources[index];
                    _nextIndex = (index + 1) % _sources.Count;

                    break;
                }
            }

            if (target == null)
            {
                target = _sources[_nextIndex];
                _nextIndex = (_nextIndex + 1) % _sources.Count;
            }

            target.PlayOneShot(clip);
        }

        public void PlayMenuOpen() =>
            Play(_menuOpen);

        public void PlayMenuClose() =>
            Play(_menuClose);
    }
}