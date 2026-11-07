using System;
using _Project.Scripts.Enemies;
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
    public class TutorialZone : MonoBehaviour
    {
        public enum Action
        {
            None,
            Move,
            Sprint,
            Jump,
            Attack,
            Block,
            HitTarget,
            ParryTarget,
            CamerasBroken,
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
        [Tooltip("Для «Ударить цель» / «Отпарировать цель»: пугала или враги")]
        [SerializeField] private Enemy[] _targets = new Enemy[0];
        [Tooltip("Для «Камеры сломаны»: цель с камерами")]
        [SerializeField] private CameraDestructionGoal _cameras;
        [Tooltip("Показать только после того, как выполнена эта зона")]
        [SerializeField] private TutorialZone _after;
        [Tooltip("Окно висит не меньше этого времени, сек")]
        [SerializeField, Min(0f)] private float _minShowTime = 1.5f;
        [Tooltip("Сколько держать окно после последнего шага, сек")]
        [SerializeField, Min(0f)] private float _holdAfterDone = 0.8f;
        [SerializeField] private string _playerTag = "Player";

        private static TutorialHintView _view;
        private static Cutscene[] _cutscenes;

        private IPlayerInputReader _input;
        private State _state;
        private bool _inside;
        private bool _hit;
        private bool _parried;
        private float _shownAt;
        private float _doneAt = -1f;
        private bool _visible;

        public bool IsDone => _state == State.Done;

        [Inject]
        public void Construct(IPlayerInputReader input) => _input = input;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private static TutorialHintView View
        {
            get
            {
                if (_view == null)
                    _view = FindAnyObjectByType<TutorialHintView>(FindObjectsInactive.Include);

                return _view;
            }
        }

        private static bool CutscenePlaying
        {
            get
            {
                if (_cutscenes == null || _cutscenes.Length == 0 || _cutscenes[0] == null)
                    _cutscenes = FindObjectsByType<Cutscene>(FindObjectsInactive.Include);

                foreach (Cutscene cutscene in _cutscenes)
                    if (cutscene != null && cutscene.IsPlaying)
                        return true;

                return false;
            }
        }

        private void OnEnable()
        {
            foreach (Enemy target in _targets)
            {
                if (target == null)
                    continue;

                target.Damaged += OnTargetDamaged;

                if (target.TryGetComponent(out IEnemyAttack attack))
                    attack.Parried += OnTargetParried;
            }
        }

        private void OnDisable()
        {
            foreach (Enemy target in _targets)
            {
                if (target == null)
                    continue;

                target.Damaged -= OnTargetDamaged;

                if (target.TryGetComponent(out IEnemyAttack attack))
                    attack.Parried -= OnTargetParried;
            }

            SetVisible(false);
        }

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
                    if (_inside && (_after == null || _after.IsDone) && !CutscenePlaying)
                    {
                        _state = State.Shown;
                        _shownAt = Time.unscaledTime;
                        _hit = _parried = false;
                    }
                    break;

                case State.Shown:
                    UpdateSteps();

                    if (_doneAt < 0f && AllDone() && Time.unscaledTime - _shownAt >= _minShowTime)
                        _doneAt = Time.unscaledTime + _holdAfterDone;

                    if (_doneAt >= 0f && Time.unscaledTime >= _doneAt)
                        _state = State.Done;
                    break;
            }

            // Во время катсцены окно прячем, после — возвращаем
            SetVisible(_state == State.Shown && !CutscenePlaying);
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
                    View?.MarkDone(this, i);
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
                case Action.HitTarget: return _hit;
                case Action.ParryTarget: return _parried;
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

        // Удар засчитывается только пока окно висит, иначе его «выполнят» заранее
        private void OnTargetDamaged(float amount)
        {
            if (_state == State.Shown)
                _hit = true;
        }

        private void OnTargetParried()
        {
            if (_state == State.Shown)
                _parried = true;
        }

        private void SetVisible(bool visible)
        {
            if (_visible == visible)
                return;

            _visible = visible;

            if (!visible)
            {
                View?.Hide(this);
                return;
            }

            View?.Show(this, _steps);

            for (int i = 0; i < _steps.Length; i++)
                if (_steps[i].Done)
                    View?.MarkDone(this, i);
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
