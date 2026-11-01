using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace SibJam.Cutscenes
{
    // Сломанный робот: раз за разом отходит и врезается вперёд (по своей оси Z) в стену.
    // Либо стартует сам, либо ждёт StartBanging (например, из события катсцены)
    public class WallBangingRobot : MonoBehaviour
    {
        private const float WindUpDuration = 0.35f;
        private const float LungeDuration = 0.08f;

        [SerializeField] private bool _bangOnStart = true;
        [SerializeField] private Transform _body;
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Удар головой о стену, берётся случайный")]
        [SerializeField] private AudioClip[] _hitClips;
        [Tooltip("Сколько секунд в начале клипов удара до самого удара: звук запускается заранее, чтобы совпасть с касанием")]
        [SerializeField, Min(0f)] private float _hitClipLead = 0.18f;
        [Tooltip("Скрип и лязг корпуса на замахе, берётся случайный")]
        [SerializeField] private AudioClip[] _bodyClips;
        [Tooltip("Глаз мигает при ударе")]
        [SerializeField] private Light _eyeLight;

        [Header("Ритм")]
        [SerializeField] private Vector2 _pauseRange = new Vector2(0.8f, 2f);
        [SerializeField, Min(0f)] private float _windUpDistance = 0.15f;
        [SerializeField, Min(0f)] private float _hitDistance = 0.12f;
        [SerializeField, Min(0f)] private float _hitTilt = 12f;
        [SerializeField, Range(0f, 0.5f)] private float _pitchJitter = 0.1f;

        [SerializeField] private UnityEvent _hit;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _eyeIntensity;

        private void Awake()
        {
            _restPosition = _body.localPosition;
            _restRotation = _body.localRotation;

            if (_eyeLight != null)
                _eyeIntensity = _eyeLight.intensity;
        }

        private void Start()
        {
            if (_bangOnStart)
                StartBanging();
        }

        // Выключение объекта обрывает корутину — после включения робота снова можно запустить
        private void OnDisable() => IsBanging = false;

        public bool IsBanging { get; private set; }

        public void StartBanging()
        {
            if (IsBanging)
                return;

            IsBanging = true;
            StartCoroutine(Banging());
        }

        private IEnumerator Banging()
        {
            for (bool first = true; ; first = false)
            {
                // Первый удар сразу, чтобы старт по событию катсцены читался
                if (!first)
                    yield return new WaitForSeconds(Random.Range(_pauseRange.x, _pauseRange.y));

                PlayRandom(_bodyClips);
                StartCoroutine(PlayHitSound(WindUpDuration + LungeDuration - _hitClipLead));

                yield return MoveTo(-_windUpDistance, -_hitTilt * 0.3f, WindUpDuration);
                yield return MoveTo(_hitDistance, _hitTilt, LungeDuration);
                Hit();
                yield return MoveTo(0f, 0f, 0.45f);
            }
        }

        private IEnumerator MoveTo(float offset, float tilt, float duration)
        {
            Vector3 startPosition = _body.localPosition;
            Quaternion startRotation = _body.localRotation;
            Vector3 targetPosition = _restPosition + _restRotation * Vector3.forward * offset;
            Quaternion targetRotation = _restRotation * Quaternion.Euler(tilt, 0f, 0f);

            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                float t = Mathf.SmoothStep(0f, 1f, time / duration);
                _body.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Slerp(startRotation, targetRotation, t));
                yield return null;
            }

            _body.SetLocalPositionAndRotation(targetPosition, targetRotation);
        }

        private IEnumerator PlayHitSound(float delay)
        {
            if (delay > 0f)
                yield return new WaitForSeconds(delay);

            PlayRandom(_hitClips);
        }

        private void PlayRandom(AudioClip[] clips)
        {
            if (_audioSource == null || clips == null || clips.Length == 0)
                return;

            _audioSource.pitch = 1f + Random.Range(-_pitchJitter, _pitchJitter);
            _audioSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }

        private void Hit()
        {
            if (_eyeLight != null)
                StartCoroutine(BlinkEye());

            _hit?.Invoke();
        }

        private IEnumerator BlinkEye()
        {
            _eyeLight.intensity = 0f;
            yield return new WaitForSeconds(0.12f);
            _eyeLight.intensity = _eyeIntensity;
        }
    }
}
