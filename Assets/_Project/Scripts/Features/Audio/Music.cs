using UnityEngine;

namespace SibJam
{
    public class Music : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _menu;
        [SerializeField] private AudioClip _game;
        [SerializeField] private AudioClip _boss;

        [Header("Эмбиент корабля под музыкой в игре")]
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private AudioClip _ambience;

        public void PlayMenu()
        {
            Play(_menu);
            SetAmbience(false);
        }

        public void PlayGame()
        {
            Play(_game);
            SetAmbience(true);
        }

        public void PlayBoss()
        {
            Play(_boss);
        }

        public void Pause()
        {
            _source.Pause();
        }

        private void Play(AudioClip clip)
        {
            _source.clip = clip;
            _source.Play();
        }

        private void SetAmbience(bool isOn)
        {
            if (_ambienceSource == null || _ambience == null)
                return;

            if (!isOn)
            {
                _ambienceSource.Stop();
                return;
            }

            if (_ambienceSource.isPlaying && _ambienceSource.clip == _ambience)
                return;

            _ambienceSource.clip = _ambience;
            _ambienceSource.loop = true;
            _ambienceSource.Play();
        }
    }
}