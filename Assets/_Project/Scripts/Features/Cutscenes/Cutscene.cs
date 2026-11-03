using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using VContainer;

namespace SibJam.Cutscenes
{
    // Катсцена в сцене, по аналогии с Animator и AnimationClip: играет клип-файл (CutsceneClip)
    // и держит то, чего в файле быть не может, — привязки клипа к объектам сцены.
    // Сама по себе играет только клип (звуки, реплики, субтитры, вызовы).
    // Наследник может добавить то, что происходит на экране (камера, анимация, финал), и взять длину оттуда
    public class Cutscene : MonoBehaviour, IExposedPropertyTable
    {
        [Header("Клип")]
        [SerializeField] private CutsceneClip _cutsceneClip;
        [Tooltip("2D-источник для звуков клипа без своего источника")]
        [SerializeField, FormerlySerializedAs("_voiceSource")] private AudioSource _audioSource;

        // Объекты сцены для ExposedReference в клипе: источники звука, цели вызовов и т.п.
        [SerializeField, HideInInspector] private List<int> _bindingIds = new List<int>();
        [SerializeField, HideInInspector] private List<UnityEngine.Object> _bindingValues = new List<UnityEngine.Object>();

        [Header("Запуск и пропуск")]
        [SerializeField] private bool _playOnStart = true;
        [SerializeField] private bool _canSkip = true;
        [Tooltip("Подсказка «нажми ещё раз, чтобы пропустить». Пусто — пропуск с первого нажатия")]
        [SerializeField] private CanvasGroup _skipHint;
        [Tooltip("Только в редакторе: начать просмотр с этой секунды. В билде катсцена всегда идёт с начала")]
        [SerializeField, Min(0f)] private float _editorStartTime;
        [SerializeField] private UnityEvent _finished;

        private VoiceLinePlayer _voiceLines;
        private Subtitles _subtitles;
        private bool _skipRequested;
        private CutscenePlayback _playback;

        public event Action Started;
        public event Action Finished;

        public bool IsPlaying { get; private set; }
        public float CurrentTime { get; private set; }
        public CutsceneClip Clip => _cutsceneClip;

        // Длина катсцены в секундах
        public virtual float Length => _cutsceneClip != null ? _cutsceneClip.Length : 0f;

        [Inject]
        public void Construct(VoiceLinePlayer voiceLines, Subtitles subtitles)
        {
            _voiceLines = voiceLines;
            _subtitles = subtitles;
        }

        protected virtual void Start()
        {
            if (_playOnStart)
            {
                Play();
                return;
            }

            SetSkipHint(0f);
            OnAutoPlayCancelled();
        }

        protected virtual void Update()
        {
            if (!IsPlaying)
                return;

            CurrentTime = ReadTime(CurrentTime);
            _playback?.Tick(CurrentTime);
            OnTick(CurrentTime);

            if (_canSkip)
                HandleSkip();

            if (CurrentTime >= Length || _skipRequested)
                Finish();
        }

        // Старт с чекпоинта: катсцена не нужна. Вызывать до Start
        public void CancelAutoPlay() => _playOnStart = false;

        public void Play()
        {
            if (IsPlaying)
                return;

            _skipRequested = false;
            CurrentTime = 0f;
            SetSkipHint(0f);

            OnPlay();
            _playback = _cutsceneClip != null
                ? new CutscenePlayback(_cutsceneClip, new CutsceneContext(_voiceLines, _subtitles, _audioSource, this))
                : null;
            IsPlaying = true;

            // Для просмотра с середины: у пройденных меток выполняем только действия (без звука и субтитров)
            if (Application.isEditor && _editorStartTime > 0f)
            {
                CurrentTime = Mathf.Min(_editorStartTime, Length);
                Seek(CurrentTime);
                _playback?.SkipTo(CurrentTime);
            }

            Started?.Invoke();
        }

        public void Skip()
        {
            if (IsPlaying)
                _skipRequested = true;
        }

        // Время на этом кадре. По умолчанию — свои часы; наследник может брать его, например, из анимации
        protected virtual float ReadTime(float previous) => previous + Time.deltaTime;

        // Перемотка (просмотр с середины в редакторе)
        protected virtual void Seek(float time) { }

        protected virtual void OnPlay() { }

        protected virtual void OnTick(float time) { }

        // Катсцена закончилась или пропущена: отдать управление игре
        protected virtual void OnFinish() { }

        // Автозапуск отменён (старт с чекпоинта): убрать с экрана то, что сохранено в сцене включённым
        protected virtual void OnAutoPlayCancelled() { }

        private void Finish()
        {
            IsPlaying = false;

            // Пропущенные действия всё равно выполняем: в них может быть то, без чего игра не продолжится (двери и т.п.)
            _playback?.Finish();
            _playback = null;

            if (_audioSource != null)
                _audioSource.Stop();

            SetSkipHint(0f);
            OnFinish();

            Finished?.Invoke();
            _finished?.Invoke();
        }

        public UnityEngine.Object GetReferenceValue(PropertyName id, out bool idValid)
        {
            int index = FindBinding(id);
            idValid = index >= 0;
            return idValid ? _bindingValues[index] : null;
        }

        public void SetReferenceValue(PropertyName id, UnityEngine.Object value)
        {
            int index = FindBinding(id);

            if (index >= 0)
            {
                _bindingValues[index] = value;
                return;
            }

            _bindingIds.Add(id.GetHashCode());
            _bindingValues.Add(value);
        }

        public void ClearReferenceValue(PropertyName id)
        {
            int index = FindBinding(id);

            if (index < 0)
                return;

            _bindingIds.RemoveAt(index);
            _bindingValues.RemoveAt(index);
        }

        private int FindBinding(PropertyName id)
        {
            for (int i = 0; i < _bindingIds.Count; i++)
            {
                if (_bindingIds[i] == id.GetHashCode())
                    return i;
            }

            return -1;
        }

        // Первое нажатие показывает подсказку, второе (пока подсказка видна) пропускает
        private void HandleSkip()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || !keyboard.spaceKey.wasPressedThisFrame)
            {
                if (_skipHint != null)
                    _skipHint.alpha = Mathf.MoveTowards(_skipHint.alpha, 0f, Time.deltaTime * 0.5f);

                return;
            }

            if (_skipHint == null || _skipHint.alpha > 0f)
                _skipRequested = true;
            else
                _skipHint.alpha = 1f;
        }

        private void SetSkipHint(float alpha)
        {
            if (_skipHint != null)
                _skipHint.alpha = alpha;
        }
    }
}
