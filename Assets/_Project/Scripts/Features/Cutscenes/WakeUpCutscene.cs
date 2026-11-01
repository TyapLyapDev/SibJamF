using System.Collections;
using SibJam.Locations;
using SibJam.Weapons;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using VContainer;

namespace SibJam.Cutscenes
{
    // Вступительная катсцена от первого лица: тело из анимации пробуждения, камера едет за костью-камерой на голове,
    // по таймлайну звучат звуки и реплики. В конце управление отдаётся игроку там, где встал герой
    [DefaultExecutionOrder(100)]
    public class WakeUpCutscene : MonoBehaviour
    {
        [Header("Анимация")]
        [SerializeField] private Animator _rig;
        [SerializeField] private AnimationClip _clip;
        [Tooltip("Камера из FBX на голове: катсцена смотрит её глазами")]
        [SerializeField] private Transform _eye;
        [Tooltip("Сдвиг камеры от кости-камеры в её осях, в метрах: Y — выше, Z — вперёд. Чтобы броня не лезла в кадр")]
        [SerializeField] private Vector3 _eyeOffset;
        [Tooltip("Кости, которые не должны лезть в кадр (голова): сжимаются в ноль")]
        [SerializeField] private Transform[] _hiddenBones;
        [SerializeField, Min(0.01f)] private float _nearClipPlane = 0.03f;

        [Header("Звук и реплики")]
        [SerializeField] private AudioSource _voiceSource;
        [SerializeField] private CutsceneCue[] _cues;

        [Header("Экран")]
        [Tooltip("Чёрный экран поверх всего: 1 — глаза закрыты")]
        [SerializeField] private CanvasGroup _eyelids;
        [SerializeField] private AnimationCurve _eyelidsCurve = new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1.2f, 1f), new Keyframe(1.7f, 0.35f), new Keyframe(2f, 0.9f),
            new Keyframe(2.6f, 0.15f), new Keyframe(2.9f, 0.55f), new Keyframe(3.6f, 0f));
        [SerializeField] private CanvasGroup _skipHint;

        [Header("Финал")]
        [SerializeField] private string _playerTag = "Player";
        [Tooltip("Герой в конце держит оружие — сразу выдаём его игроку")]
        [SerializeField] private bool _equipWeaponOnFinish = true;
        [SerializeField] private LocationDoor _exitDoor;
        [SerializeField, Min(0f)] private float _handOverDuration = 0.4f;
        [SerializeField] private UnityEvent _finished;

        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _canSkip = true;
        [Tooltip("Только в редакторе: начать просмотр с этой секунды. В билде катсцена всегда идёт с начала")]
        [SerializeField, Min(0f)] private float _editorStartTime;
        [SerializeField, Min(0f)] private float _shakeDecay = 6f;

        private PlayableGraph _graph;
        private AnimationClipPlayable _clipPlayable;
        private bool[] _firedCues;
        private VoiceLinePlayer _voiceLines;
        private GameObject _player;
        private Camera _camera;
        private CameraFollow _cameraFollow;
        private float _defaultNearClipPlane;
        private float _shake;
        private bool _skipRequested;
        private Pose _endPose;
        private float _handOverTime = -1f;

        public bool IsPlaying { get; private set; }

        [Inject]
        public void Construct(VoiceLinePlayer voiceLines)
        {
            _voiceLines = voiceLines;
        }

        private void Start()
        {
            if (_playOnStart)
            {
                Play();
                return;
            }

            // Без катсцены (старт с чекпоинта) веки в сцене сохранены закрытыми — открываем, иначе чёрный экран
            _rig.gameObject.SetActive(false);
            _eyelids.alpha = 0f;
            _skipHint.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (_graph.IsValid())
                _graph.Destroy();
        }

        private void Update()
        {
            if (!IsPlaying)
                return;

            float time = (float)_clipPlayable.GetTime();

            FireCues(time);
            _eyelids.alpha = _eyelidsCurve.Evaluate(time);

            if (_canSkip)
                HandleSkip();

            if (time >= _clip.length || _skipRequested)
                Finish();
        }

        private void LateUpdate()
        {
            if (IsPlaying)
            {
                FollowEye();
                return;
            }

            if (_handOverTime >= 0f)
                BlendToPlayer();
        }

        // Старт с чекпоинта: катсцена не нужна, игрока ставит CheckpointFlow. Вызывать до Start
        public void CancelAutoPlay() => _playOnStart = false;

        public void Play()
        {
            _player = GameObject.FindWithTag(_playerTag);

            if (_player != null)
                _player.SetActive(false);

            _camera = Camera.main;

            if (_camera == null)
                _camera = _eye.GetComponent<Camera>();

            _camera.enabled = true;
            _cameraFollow = _camera.GetComponent<CameraFollow>();

            if (_cameraFollow != null)
                _cameraFollow.enabled = false;

            _defaultNearClipPlane = _camera.nearClipPlane;
            _camera.nearClipPlane = _nearClipPlane;

            _rig.gameObject.SetActive(true);
            _rig.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _clipPlayable = AnimationPlayableUtilities.PlayClip(_rig, _clip, out _graph);

            _firedCues = new bool[_cues.Length];

            if (Application.isEditor && _editorStartTime > 0f)
                SkipTo(Mathf.Min(_editorStartTime, _clip.length));

            _eyelids.alpha = 1f;
            _skipHint.alpha = 0f;
            _skipRequested = false;
            IsPlaying = true;
        }

        // Тряска камеры: для ударов робота и прочих толчков, работает только пока идёт катсцена
        public void Shake(float amplitude)
        {
            if (IsPlaying)
                _shake = Mathf.Max(_shake, amplitude);
        }

        // Для просмотра с середины: перематываем анимацию, а у пройденных событий выполняем только действия (без звука и субтитров)
        private void SkipTo(float time)
        {
            _clipPlayable.SetTime(time);

            for (int i = 0; i < _cues.Length; i++)
            {
                if (_cues[i].Time >= time)
                    continue;

                _firedCues[i] = true;
                _cues[i].Event?.Invoke();
            }
        }

        private void FireCues(float time)
        {
            for (int i = 0; i < _cues.Length; i++)
            {
                if (_firedCues[i] || time < _cues[i].Time)
                    continue;

                _firedCues[i] = true;
                FireCue(_cues[i]);
            }
        }

        private void FireCue(CutsceneCue cue)
        {
            if (cue.Clip != null)
            {
                AudioSource source = cue.Source != null ? cue.Source : _voiceSource;
                source.PlayOneShot(cue.Clip);
            }

            if (cue.Line != null)
                _voiceLines.Play(cue.Line, cue.Source);

            cue.Event?.Invoke();
        }

        // Первое нажатие показывает подсказку, второе (пока подсказка видна) пропускает
        private void HandleSkip()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            {
                _skipHint.alpha = Mathf.MoveTowards(_skipHint.alpha, 0f, Time.deltaTime * 0.5f);
                return;
            }

            if (_skipHint.alpha > 0f)
                _skipRequested = true;
            else
                _skipHint.alpha = 1f;
        }

        private void FollowEye()
        {
            foreach (Transform bone in _hiddenBones)
                bone.localScale = Vector3.one;

            Vector3 position = _eye.position + _eye.rotation * _eyeOffset;
            Quaternion rotation = _eye.rotation;

            foreach (Transform bone in _hiddenBones)
                bone.localScale = Vector3.zero;

            if (_shake > 0f)
            {
                rotation *= Quaternion.Euler(Random.insideUnitSphere * _shake);
                _shake = Mathf.MoveTowards(_shake, 0f, _shakeDecay * Time.deltaTime);
            }

            _camera.transform.SetPositionAndRotation(position, rotation);
            _endPose = new Pose(position, rotation);
        }

        private void Finish()
        {
            IsPlaying = false;

            // Пропущенные события всё равно выполняем: в них может быть то, без чего игра не продолжится (двери и т.п.)
            for (int i = 0; i < _cues.Length; i++)
            {
                if (!_firedCues[i])
                    _cues[i].Event?.Invoke();
            }

            // При пропуске герой ещё лежит — отдаём игроку позу из последнего кадра анимации
            _shake = 0f;
            _clipPlayable.SetTime(_clip.length);
            _graph.Evaluate();
            FollowEye();

            StopVoices();
            _voiceLines.Stop();
            _eyelids.alpha = 0f;
            _skipHint.alpha = 0f;
            _shake = 0f;

            _graph.Destroy();
            _rig.gameObject.SetActive(false);

            HandOverToPlayer();

            if (_exitDoor != null && !_exitDoor.IsOpen)
                _exitDoor.Open();

            _finished?.Invoke();
        }

        private void StopVoices()
        {
            _voiceSource.Stop();

            foreach (CutsceneCue cue in _cues)
            {
                if (cue.Source != null)
                    cue.Source.Stop();
            }
        }

        private void HandOverToPlayer()
        {
            _camera.nearClipPlane = _defaultNearClipPlane;

            if (_player == null)
                return;

            // Ставим игрока ногами на пол под глазами героя и разворачиваем туда, куда герой смотрит
            Vector3 forward = Vector3.ProjectOnPlane(_endPose.forward, Vector3.up);
            Quaternion yaw = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : _player.transform.rotation;
            Vector3 feet = new Vector3(_endPose.position.x, transform.position.y, _endPose.position.z);

            if (_player.TryGetComponent(out CharacterController controller))
                feet = FindFreeSpot(feet, yaw * Vector3.forward, controller);

            _player.transform.SetPositionAndRotation(feet, yaw);

            if (_equipWeaponOnFinish && _player.TryGetComponent(out MeleeWeapon weapon))
                weapon.enabled = true;

            _player.SetActive(true);

            if (_cameraFollow != null)
            {
                _cameraFollow.enabled = true;
                _handOverTime = 0f;
            }
        }

        // Капсула игрока толще тела героя: если в точке финала она задевает мебель, сдвигаем вперёд, пока не встанет свободно
        private static Vector3 FindFreeSpot(Vector3 feet, Vector3 forward, CharacterController controller)
        {
            const float Step = 0.1f;
            const int MaxSteps = 15;
            const float FloorGap = 0.05f;

            float radius = controller.radius;
            Vector3 bottom = controller.center + Vector3.up * (FloorGap - controller.height * 0.5f + radius);
            Vector3 top = controller.center + Vector3.up * (controller.height * 0.5f - radius);

            for (int i = 0; i < MaxSteps; i++)
            {
                Vector3 candidate = feet + forward * (Step * i);

                if (!Physics.CheckCapsule(candidate + bottom, candidate + top, radius, ~0, QueryTriggerInteraction.Ignore))
                    return candidate;
            }

            return feet;
        }

        // CameraFollow уже поставил камеру к игроку (он выполняется раньше), плавно подводим её туда из последнего кадра катсцены
        private void BlendToPlayer()
        {
            _handOverTime += Time.deltaTime;
            float t = _handOverDuration > 0f ? Mathf.SmoothStep(0f, 1f, _handOverTime / _handOverDuration) : 1f;
            Transform cameraTransform = _camera.transform;

            cameraTransform.SetPositionAndRotation(
                Vector3.Lerp(_endPose.position, cameraTransform.position, t),
                Quaternion.Slerp(_endPose.rotation, cameraTransform.rotation, t));

            if (t >= 1f)
                _handOverTime = -1f;
        }
    }
}
