using SibJam.Cutscenes;
using SibJam.Locations;
using SibJam.Player;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Boss
{
    // Вход на арену босса: игрок подошёл к двери, когда босс — текущая локация, → управление забирается,
    // игрока подводят к центру проёма лицом к двери, ждут, пока дверь поднимется, и заводят внутрь.
    // Дверь открывает сам проход (LocationGate) при подходе; вход в EntryZone запускает локацию как обычно.
    // Пропуск — игрок сразу оказывается в конечной точке
    public class BossEntranceCutscene : Cutscene
    {
        [Header("Вход к боссу")]
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private LocationLoop _bossLocation;
        [SerializeField] private LocationDoor _door;
        [Tooltip("Точка перед дверью, по центру проёма")]
        [SerializeField] private Transform _approachPoint;
        [Tooltip("Точка за дверью, куда игрока заводят")]
        [SerializeField] private Transform _insidePoint;
        [Tooltip("С какого расстояния до двери (по горизонтали) начинается катсцена")]
        [SerializeField, Min(0.5f)] private float _triggerDistance = 5f;
        [SerializeField, Min(0.1f)] private float _walkSpeed = 3f;
        [Tooltip("Скорость поворота лицом к двери, град/с")]
        [SerializeField, Min(1f)] private float _turnSpeed = 240f;
        [Tooltip("Сколько ждать после начала открытия двери, прежде чем идти в проём")]
        [SerializeField, Min(0f)] private float _doorWait = 1.3f;
        [Tooltip("Страховка: катсцена завершается не позже этого времени")]
        [SerializeField, Min(1f)] private float _maxDuration = 10f;

        private const float ReachDistance = 0.1f;

        private Transform _player;
        private CharacterController _controller;
        private Behaviour[] _playerControls;
        private bool[] _wasEnabled;
        private bool _played;
        private bool _reachedApproach;
        private float _doorOpenTime = -1f;
        private bool _done;

        public override float Length => _maxDuration;

        protected override void Start()
        {
            base.Start();

            PlayerHealth player = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);
            if (player == null)
                return;

            _player = player.transform;
            _controller = player.GetComponent<CharacterController>();
            _playerControls = new Behaviour[]
            {
                player.GetComponent<CharacterControllerMover>(),
                player.GetComponent<Jumper>(),
                player.GetComponent<Rotator>(),
                player.GetComponentInChildren<MeleeWeapon>(true),
            };
        }

        protected override void Update()
        {
            if (!_played && !IsPlaying && ShouldStart())
            {
                _played = true;
                Play();
            }

            base.Update();
        }

        private bool ShouldStart()
        {
            if (_player == null || !_player.gameObject.activeInHierarchy || _door == null)
                return false;

            if (_gameLoop.CurrentLocationLoop != _bossLocation || _bossLocation.State != LocationLoopState.Idle)
                return false;

            Vector3 offset = _player.position - _door.transform.position;
            return Mathf.Abs(offset.y) < 3f
                   && new Vector2(offset.x, offset.z).sqrMagnitude <= _triggerDistance * _triggerDistance;
        }

        protected override void OnPlay()
        {
            _reachedApproach = false;
            _doorOpenTime = -1f;
            _done = false;
            DisableControls();
        }

        protected override void OnTick(float time)
        {
            if (_done || _player == null)
                return;

            if (!_reachedApproach)
            {
                _reachedApproach = MoveTowards(_approachPoint.position, faceTarget: _insidePoint.position);
                return;
            }

            if (_doorOpenTime < 0f)
            {
                Face(_insidePoint.position);

                if (_door.IsOpen)
                    _doorOpenTime = time;

                return;
            }

            if (time - _doorOpenTime < _doorWait)
            {
                Face(_insidePoint.position);
                return;
            }

            if (MoveTowards(_insidePoint.position, faceTarget: _insidePoint.position + (_insidePoint.position - _approachPoint.position)))
            {
                _done = true;
                Skip();
            }
        }

        protected override void OnFinish()
        {
            // Пропуск или страховка по времени: ставим игрока в конечную точку
            if (!_done && _player != null && _insidePoint != null)
            {
                if (_door != null && !_door.IsOpen)
                    _door.Open();

                if (_controller != null)
                    _controller.enabled = false;

                Vector3 direction = _insidePoint.position - _approachPoint.position;
                direction.y = 0f;
                _player.SetPositionAndRotation(_insidePoint.position,
                    direction.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(direction) : _player.rotation);

                if (_controller != null)
                    _controller.enabled = true;
            }

            _done = true;
            RestoreControls();
        }

        // Идём по горизонтали к точке, вертикаль оставляем гравитации
        private bool MoveTowards(Vector3 target, Vector3 faceTarget)
        {
            Vector3 offset = target - _player.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            Face(distance > 0.5f ? target : faceTarget);

            if (distance <= ReachDistance)
                return true;

            Vector3 step = offset / distance * Mathf.Min(distance, _walkSpeed * Time.deltaTime);

            if (_controller != null && _controller.enabled)
                _controller.Move(step);
            else
                _player.position += step;

            return false;
        }

        private void Face(Vector3 target)
        {
            Vector3 direction = target - _player.position;
            direction.y = 0f;

            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion look = Quaternion.LookRotation(direction);
            _player.rotation = Quaternion.RotateTowards(_player.rotation, look, _turnSpeed * Time.deltaTime);
        }

        // Запоминаем, что было включено: например, оружие могло быть ещё не подобрано
        private void DisableControls()
        {
            if (_playerControls == null)
                return;

            _wasEnabled = new bool[_playerControls.Length];

            for (int i = 0; i < _playerControls.Length; i++)
            {
                if (_playerControls[i] == null)
                    continue;

                _wasEnabled[i] = _playerControls[i].enabled;
                _playerControls[i].enabled = false;
            }
        }

        private void RestoreControls()
        {
            if (_playerControls == null || _wasEnabled == null)
                return;

            for (int i = 0; i < _playerControls.Length; i++)
            {
                if (_playerControls[i] != null && _wasEnabled[i])
                    _playerControls[i].enabled = true;
            }

            _wasEnabled = null;
        }
    }
}
