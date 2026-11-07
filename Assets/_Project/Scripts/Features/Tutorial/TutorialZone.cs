using System;
using SibJam.Cutscenes;
using SibJam.SecurityCameras;
using UnityEngine;
using VContainer;

namespace SibJam.Tutorial
{
    // Зона обучения: игрок зашёл в триггер — у правого края всплывает окно со списком шагов
    // (после катсцены и после предыдущей зоны). Каждый выполненный шаг гаснет; окно пропадает,
    // когда выполнены все шаги, даже если игрок уже вышел из зоны
    [RequireComponent(typeof(Collider))]
    public class TutorialZone : MonoBehaviour, IInjectedFromScene
    {
        public enum Action
        {
            None,
            Move,
            Sprint,
            Jump,
            Attack,
            Block,
            CamerasBroken = 8,
        }

        [Serializable]
        public class Step
        {
            public string Key = "ЛКМ";
            public string Text = "Удар";
            [Tooltip("Что сделать, чтобы шаг засчитался. «Нет» — когда игрок ушёл из зоны")]
            public Action Until = Action.Attack;

            [NonSerialized] public bool Done;
        }

        private enum State { Waiting, Shown, Done }

        [SerializeField] private Step[] _steps = new Step[0];
        [Tooltip("Для «Камеры сломаны»: цель с камерами")]
        [SerializeField] private CameraDestructionGoal _cameras;
        [Tooltip("Окно висит не меньше этого времени, сек")]
        [SerializeField, Min(0f)] private float _minShowTime = 1.5f;
        [Tooltip("Сколько держать окно после последнего шага, сек")]
        [SerializeField, Min(0f)] private float _holdAfterDone = 0.8f;
        [SerializeField] private string _playerTag = "Player";

        private IPlayerInputReader _input;
        private TutorialHintView _view;
        private ActiveCutscenes _activeCutscenes;
        private State _state;
        private bool _inside;
        private float _shownAt;
        private float _doneAt = -1f;
        private bool _visible;
        private bool _unlocked;

        public event Action<TutorialZone> Completed;

        public bool IsDone => _state == State.Done;

        // Порядок зон задаёт StartLocationFlow
        public void Unlock() => _unlocked = true;

        [Inject]
        public void Construct(IPlayerInputReader input, TutorialHintView view, ActiveCutscenes activeCutscenes)
        {
            _input = input;
            _view = view;
            _activeCutscenes = activeCutscenes;
        }

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void OnDisable() => SetVisible(false);

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _inside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag(_playerTag))
                _inside = false;
        }

        private void Update()
        {
            switch (_state)
            {
                case State.Waiting:
                    if (_inside && _unlocked && !_activeCutscenes.IsAnyPlaying)
                    {
                        _state = State.Shown;
                        _shownAt = Time.unscaledTime;
                    }
                    break;

                case State.Shown:
                    UpdateSteps();

                    if (_doneAt < 0f && AllDone() && Time.unscaledTime - _shownAt >= _minShowTime)
                        _doneAt = Time.unscaledTime + _holdAfterDone;

                    if (_doneAt >= 0f && Time.unscaledTime >= _doneAt)
                    {
                        _state = State.Done;
                        Completed?.Invoke(this);
                    }
                    break;
            }

            // Во время катсцены окно прячем, после — возвращаем
            SetVisible(_state == State.Shown && !_activeCutscenes.IsAnyPlaying);
        }

        private void UpdateSteps()
        {
            for (int i = 0; i < _steps.Length; i++)
            {
                Step step = _steps[i];

                if (step.Done || !IsCompleted(step.Until))
                    continue;

                step.Done = true;

                if (_visible)
                    _view.MarkDone(this, i);
            }
        }

        private bool AllDone()
        {
            foreach (Step step in _steps)
                if (!step.Done)
                    return false;

            return true;
        }

        private bool IsCompleted(Action action)
        {
            switch (action)
            {
                case Action.None: return !_inside;
                case Action.CamerasBroken: return _cameras == null || _cameras.IsCompleted;
            }

            if (_input == null)
                return false;

            bool moving = _input.MoveDirection.sqrMagnitude > 0.1f;

            switch (action)
            {
                case Action.Move: return moving;
                case Action.Sprint: return _input.IsSprinting && moving;
                case Action.Jump: return _input.IsJumpPressed;
                case Action.Attack: return _input.IsAttackPressed;
                case Action.Block: return _input.IsBlockPressed;
                default: return false;
            }
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;

            _visible = visible;

            if (!visible)
            {
                _view.Hide(this);
                return;
            }

            _view.Show(this, _steps);

            for (int i = 0; i < _steps.Length; i++)
                if (_steps[i].Done)
                    _view.MarkDone(this, i);
        }

        private void OnDrawGizmos()
        {
            var box = GetComponent<BoxCollider>();

            if (box == null)
                return;

            Gizmos.color = new Color(0.3f, 0.9f, 1f, 0.15f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
    }
}
