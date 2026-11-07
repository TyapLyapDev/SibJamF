using System.Collections;
using _Project.Scripts.Enemies;
using SibJam.Feedback;
using UnityEngine;

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

        [Header("Здоровье")]
        [Tooltip("Если не задано, робот получает стандартные 100 HP")]
        [SerializeField] private EnemyConfig _healthConfig;

        [Header("Ритм")]
        [SerializeField] private Vector2 _pauseRange = new Vector2(0.8f, 2f);
        [SerializeField, Min(0f)] private float _windUpDistance = 0.15f;
        [SerializeField, Min(0f)] private float _hitDistance = 0.12f;
        [SerializeField, Min(0f)] private float _hitTilt = 12f;
        [SerializeField, Range(0f, 0.5f)] private float _pitchJitter = 0.1f;

        [Header("Тряска камеры")]
        [Tooltip("Удары трясут камеру. Включается и выключается и с таймлайна катсцены")]
        [SerializeField] private bool _cameraShakeEnabled;
        [Tooltip("Травма CameraShaker от удара рядом с роботом, от 0 до 1")]
        [SerializeField, Range(0f, 1f)] private float _cameraTrauma = 0.35f;
        [Tooltip("Дальше этого от камеры не трясёт, метры. 0 — одинаково на любом расстоянии")]
        [SerializeField, Min(0f)] private float _cameraShakeRadius;

        private Vector3 _restPosition;
        private Quaternion _restRotation;
        private float _eyeIntensity;
        private CameraShaker _shaker;
        private Enemy _enemy;
        private EnemyConfig _runtimeHealthConfig;

        private void Awake()
        {
            // Используем обычный урон и смерть врагов, включая попадания мечом и снарядами.
            if (!TryGetComponent(out _enemy))
                _enemy = gameObject.AddComponent<Enemy>();

            if (_healthConfig == null)
            {
                _runtimeHealthConfig = ScriptableObject.CreateInstance<EnemyConfig>();
                _healthConfig = _runtimeHealthConfig;
            }
            _enemy.Init(_healthConfig, null);
            _enemy.Died += OnDied;

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

        private void OnDied(Enemy enemy)
        {
            StopAllCoroutines();
            IsBanging = false;
            if (_audioSource != null) _audioSource.Stop();
            if (_eyeLight != null) _eyeLight.enabled = false;
        }

        private void OnDestroy()
        {
            if (_enemy != null) _enemy.Died -= OnDied;
            if (_runtimeHealthConfig != null) Destroy(_runtimeHealthConfig);
        }

        public bool IsBanging { get; private set; }

        public bool CameraShakeEnabled
        {
            get => _cameraShakeEnabled;
            set => _cameraShakeEnabled = value;
        }

        // Для «Действия» на таймлайне: оно вызывает только методы без параметров
        public void EnableCameraShake() => CameraShakeEnabled = true;

        public void DisableCameraShake() => CameraShakeEnabled = false;

        public void StartBanging()
        {
            if (IsBanging || (_enemy != null && _enemy.IsDead))
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

            ShakeCamera();
        }

        private void ShakeCamera()
        {
            if (!_cameraShakeEnabled)
                return;

            Camera camera = Camera.main;

            // Робот в DI не попадает — тряску берём с главной камеры
            if (_shaker == null && (camera == null || !camera.TryGetComponent(out _shaker)))
                return;

            float falloff = _cameraShakeRadius > 0f
                ? 1f - Mathf.Clamp01(Vector3.Distance(transform.position, _shaker.transform.position) / _cameraShakeRadius)
                : 1f;

            if (falloff > 0f)
                _shaker.AddTrauma(_cameraTrauma * falloff);
        }

        private IEnumerator BlinkEye()
        {
            _eyeLight.intensity = 0f;
            yield return new WaitForSeconds(0.12f);
            _eyeLight.intensity = _eyeIntensity;
        }
    }
}
