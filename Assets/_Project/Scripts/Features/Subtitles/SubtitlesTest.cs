using System.Collections;
using UnityEngine;
using VContainer;

namespace SibJam
{
    public class SubtitlesTest : MonoBehaviour
    {
        [Header("Subtitles")]
        [SerializeField] private SubtitleConfig[] _configs;

        [Header("Timing")]
        [Tooltip("Задержка перед показом первого субтитра")]
        [SerializeField, Min(0f)] private float _startDelay = 1f;

        [Tooltip("Сколько субтитр держится на экране ПОСЛЕ завершения анимации")]
        [SerializeField, Min(0f)] private float _holdDuration = 3f;

        [Tooltip("Пауза между субтитрами (после скрытия)")]
        [SerializeField, Min(0f)] private float _showInterval = 2f;

        [Header("Behaviour")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _loop = false;
        [SerializeField] private bool _useUnscaledTime = false;

        private Subtitles _subtitles;
        private Coroutine _routine;

        [Inject]
        public void Construct(Subtitles subtitles)
        {
            _subtitles = subtitles;
        }

        private void Start()
        {
            if (_playOnStart)
                Play();
        }

        public void Play()
        {
            Stop();
            _routine = StartCoroutine(PlayRoutine());
        }

        public void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }

            if (_subtitles != null)
                _subtitles.Hide();
        }

        private IEnumerator PlayRoutine()
        {
            if (_configs == null || _configs.Length == 0)
                yield break;

            if (_startDelay > 0f)
                yield return Wait(_startDelay);

            do
            {
                foreach (SubtitleConfig config in _configs)
                {
                    if (config == null)
                        continue;

                    _subtitles.Show(config);

                    while (_subtitles != null && _subtitles.IsAnimating)
                        yield return null;

                    yield return Wait(_holdDuration);

                    _subtitles.Hide();
                    yield return Wait(_showInterval);
                }
            }
            while (_loop);
        }

        private IEnumerator Wait(float seconds)
        {
            if (_useUnscaledTime)
                yield return new WaitForSecondsRealtime(seconds);
            else
                yield return new WaitForSeconds(seconds);
        }
    }
}