using System.Collections;
using UnityEngine;

namespace SibJam.Feedback
{
    // Короткое замирание времени при попадании. Не трогает паузу: если кто-то сменил timeScale, пока длится стоп, его не перетираем
    public class HitStop : MonoBehaviour
    {
        private const float StopScale = 0.05f;

        private static HitStop _instance;
        private float _until;
        private bool _isRunning;

        public static void Play(float duration)
        {
            if (duration <= 0f || Time.timeScale <= 0f)
                return;

            if (_instance == null)
            {
                var host = new GameObject(nameof(HitStop));
                DontDestroyOnLoad(host);
                _instance = host.AddComponent<HitStop>();
            }

            _instance._until = Mathf.Max(_instance._until, Time.unscaledTime + duration);

            if (!_instance._isRunning)
                _instance.StartCoroutine(_instance.Run());
        }

        private IEnumerator Run()
        {
            _isRunning = true;
            Time.timeScale = StopScale;

            while (Time.unscaledTime < _until && Mathf.Approximately(Time.timeScale, StopScale))
                yield return null;

            if (Mathf.Approximately(Time.timeScale, StopScale))
                Time.timeScale = 1f;

            _isRunning = false;
        }
    }
}
