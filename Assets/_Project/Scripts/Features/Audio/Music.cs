using UnityEngine;

namespace SibJam
{
    public class Music : MonoBehaviour
    {
        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _menu;
        [SerializeField] private AudioClip _game;

        public void PlayMenu()
        {
            Play(_menu);
        }

        public void PlayGame()
        {
            Play(_game);
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
    }
}