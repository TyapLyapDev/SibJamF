using System;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace SibJam.Cutscenes
{
    // Катсцена в сцене, по аналогии с Animator и AnimationClip: играет клип-файл (CutsceneClip).
    // Здесь только ход времени, начало, конец и пропуск. Что делать в момент времени — решает клип (CutscenePlayback),
    // привязки к сцене — CutsceneBindings, подсказку и нажатия пропуска — сервис SkipHint.
    // Наследник может добавить своё через хуки On*, но анимация, камера и затемнение — элементы клипа.
    // После CameraFollow: камера катсцены ставится поверх камеры игрока
    [DefaultExecutionOrder(100)]
    public class Cutscene : MonoBehaviour, IExposedPropertyTable
    {
        [Header("Клип")]
        [SerializeField] private CutsceneClip _cutsceneClip;
        [Tooltip("2D-источник для звуков клипа без своего источника")]
        [SerializeField, FormerlySerializedAs("_audioSource"), FormerlySerializedAs("_voiceSource")] private AudioSource _baseAudioSource;
        [SerializeField, HideInInspector] private CutsceneBindings _bindings = new CutsceneBindings();

        [Header("Запуск и пропуск")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _canSkip = true;

        private VoiceLinePlayer _voiceLines;
        private Subtitles _subtitles;
        private SkipHint _skipHint;
        private CutscenePlayback _playback;
        private bool _skipRequested;

        public event Action Started;
        public event Action Finished;

        public bool IsPlaying { get; private set; }
        public float CurrentTime { get; private set; }
        public CutsceneClip Clip => _cutsceneClip;

        // Длина катсцены в секундах
        public virtual float Length => _cutsceneClip != null ? _cutsceneClip.Length : 0f;

        [Inject]
        public void Construct(VoiceLinePlayer voiceLines, Subtitles subtitles, SkipHint skipHint)
        {
            _voiceLines = voiceLines;
            _subtitles = subtitles;
            _skipHint = skipHint;
        }

        protected virtual void Start()
        {
            if (_playOnStart)
            {
                Play();
                return;
            }

            if (_cutsceneClip != null)
                CutscenePlayback.Cancel(_cutsceneClip, CreateContext());

            OnAutoPlayCancelled();
        }

        protected virtual void Update()
        {
            if (!IsPlaying)
                return;

            CurrentTime = ReadTime(CurrentTime);
            _playback?.Tick(CurrentTime);
            OnTick(CurrentTime);

            if (_canSkip && _skipHint.WantsSkip(Time.deltaTime))
                Skip();

            if (CurrentTime >= Length || _skipRequested)
                Finish();
        }

        protected virtual void LateUpdate()
        {
            if (IsPlaying)
                _playback?.LateTick();
        }

        // Старт с чекпоинта: катсцена не нужна. Вызывать до Start
        public void CancelAutoPlay() => _playOnStart = false;

        public void Play()
        {
            if (IsPlaying)
                return;

            _skipRequested = false;
            CurrentTime = 0f;
            _skipHint.Hide();

            OnPlay();
            _playback = _cutsceneClip != null
                ? new CutscenePlayback(_cutsceneClip, CreateContext())
                : null;
            IsPlaying = true;

            Started?.Invoke();
        }

        public void Skip()
        {
            if (IsPlaying)
                _skipRequested = true;
        }

        private CutsceneContext CreateContext() => new CutsceneContext(_voiceLines, _subtitles, _baseAudioSource, this);

        // Время на этом кадре. По умолчанию — свои часы; наследник может брать его, например, из анимации
        protected virtual float ReadTime(float previous) => previous + Time.deltaTime;

        protected virtual void OnPlay() { }

        protected virtual void OnTick(float time) { }

        // Катсцена закончилась или пропущена: отдать управление игре
        protected virtual void OnFinish() { }

        // Автозапуск отменён (старт с чекпоинта): убрать с экрана то, что сохранено в сцене включённым
        protected virtual void OnAutoPlayCancelled() { }

        private void Finish()
        {
            IsPlaying = false;

            // Пропущенные элементы всё равно выполняют обязательное (двери и т.п.), звучащее глушится
            _playback?.Finish();
            _playback = null;

            if (_baseAudioSource != null)
                _baseAudioSource.Stop();

            _skipHint.Hide();
            OnFinish();

            Finished?.Invoke();
        }

        // Таблица привязок для ExposedReference: Unity ищет её на самом компоненте, поэтому отдаём через него
        public UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid) => _bindings.GetReferenceValue(id, out idValid);

        public void SetReferenceValue(PropertyName id, UnityEngine.Object value) => _bindings.SetReferenceValue(id, value);

        public void ClearReferenceValue(PropertyName id) => _bindings.ClearReferenceValue(id);
    }
}
