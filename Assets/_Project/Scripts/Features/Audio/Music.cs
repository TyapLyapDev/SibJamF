using DG.Tweening;
using SibJam.Waves;
using UnityEngine;

namespace SibJam
{
    public class Music : MonoBehaviour
    {
        private enum Mode { None, Menu, Game, Boss }

        [SerializeField] private AudioSource _source;
        [SerializeField] private AudioClip _menu;
        [Tooltip("Спокойствие: волн нет")]
        [SerializeField] private AudioClip _calm;
        [Tooltip("Бой: идут волны")]
        [SerializeField] private AudioClip _game;
        [SerializeField] private AudioClip _boss;
        [SerializeField, Min(0f)] private float _fadeTime = 1.2f;

        [Header("Эмбиент корабля под музыкой в игре")]
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private AudioClip _ambience;
        [SerializeField, Range(0f, 1f)] private float _ambienceCombatVolume = 0.5f;

        private Mode _mode;
        private bool _combat;
        private float _musicVolume = -1f;
        private float _ambienceVolume = -1f;
        private WavesLoop[] _loops;

        public void PlayMenu()
        {
            _mode = Mode.Menu;
            Play(_menu);
            SetAmbience(false);
        }

        public void PlayGame()
        {
            _mode = Mode.Game;
            _loops = FindObjectsByType<WavesLoop>(FindObjectsInactive.Include);
            _combat = AnyWavesRunning();
            Play(GameClip());
            SetAmbience(true);
        }

        public void PlayBoss()
        {
            _mode = Mode.Boss;
            Play(_boss);
            DuckAmbience(true);
        }

        public void Pause()
        {
            _source.Pause();
        }

        private void Update()
        {
            if (_mode != Mode.Game)
                return;

            bool combat = AnyWavesRunning();
            if (combat == _combat)
                return;

            _combat = combat;
            Play(GameClip());
            DuckAmbience(_combat);
        }

        private AudioClip GameClip() =>
            _combat || _calm == null ? _game : _calm;

        private bool AnyWavesRunning()
        {
            if (_loops == null)
                return false;

            foreach (WavesLoop loop in _loops)
                if (loop != null && loop.Info != null && loop.IsRunning)
                    return true;

            return false;
        }

        private void Play(AudioClip clip)
        {
            if (_musicVolume < 0f)
                _musicVolume = _source.volume;

            if (_source.clip == clip && _source.isPlaying)
                return;

            _source.DOKill();

            if (!_source.isPlaying || _fadeTime <= 0f)
            {
                _source.clip = clip;
                _source.volume = _musicVolume;
                _source.Play();
                return;
            }

            float half = _fadeTime * 0.5f;
            _source.DOFade(0f, half).SetUpdate(true).OnComplete(() =>
            {
                _source.clip = clip;
                _source.Play();
                _source.DOFade(_musicVolume, half).SetUpdate(true);
            });
        }

        private void SetAmbience(bool isOn)
        {
            if (_ambienceSource == null || _ambience == null)
                return;

            if (_ambienceVolume < 0f)
                _ambienceVolume = _ambienceSource.volume;

            if (!isOn)
            {
                _ambienceSource.DOKill();
                _ambienceSource.Stop();
                return;
            }

            DuckAmbience(_combat);

            if (_ambienceSource.isPlaying && _ambienceSource.clip == _ambience)
                return;

            _ambienceSource.clip = _ambience;
            _ambienceSource.loop = true;
            _ambienceSource.Play();
        }

        // По таблице: в бою эмбиент корабля приглушается до 0.5
        private void DuckAmbience(bool duck)
        {
            if (_ambienceSource == null)
                return;

            if (_ambienceVolume < 0f)
                _ambienceVolume = _ambienceSource.volume;

            float target = duck ? _ambienceVolume * _ambienceCombatVolume : _ambienceVolume;
            _ambienceSource.DOKill();
            _ambienceSource.DOFade(target, _fadeTime).SetUpdate(true);
        }
    }
}
