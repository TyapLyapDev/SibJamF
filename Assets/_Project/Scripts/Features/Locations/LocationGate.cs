using System;
using SibJam.Player;
using SibJam.SecurityCameras;
using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Locations
{
    // Проход из пройденной локации (_fromLocation) в следующую (_toLocation). Двери двух видов:
    //
    // Входные (_door, _extraDoors) — в следующую локацию. Пока проход разблокирован, открываются,
    // когда игрок подошёл, и закрываются, когда отошёл. Игрок вошёл в комнату → локация запускается,
    // входные двери закрываются насовсем.
    //
    // Выходные (_exitDoors) — из пройденной локации. Открываются только при настоящем прохождении
    // _fromLocation (не при старте с чекпоинта): сразу или при подходе. Игрок вышел из локации
    // (зашёл в LeftZone за дверью) → дверь закрывается насовсем.
    //
    // _passThroughFor: при старте с чекпоинта этой локации входные двери работают как транзит
    // через уже пройденную комнату — открываются при подходе, закрываются за игроком, локация не запускается.
    public class LocationGate : MonoBehaviour
    {
        [Serializable]
        private class ExitDoor
        {
            public LocationDoor Door;
            [Tooltip("Выкл — дверь открывается сразу после прохождения локации и стоит открытой, пока игрок не выйдет")]
            public bool OpenOnApproach;
            [Tooltip("Зона сразу за дверью снаружи пройденной локации: игрок в ней → дверь закрывается насовсем")]
            public PassageZone LeftZone;

            [NonSerialized] public bool IsActive;
            [NonSerialized] public bool IsLocked;
            [NonSerialized] public bool IsOpenedByGate;
            [NonSerialized] public Vector3 ClosedPosition;
        }

        [SerializeField] private GameLoop _gameLoop;
        [Tooltip("Пусто — стартовая комната: проход откроется сразу или после подбора _requiredPickup")]
        [SerializeField] private LocationLoop _fromLocation;
        [SerializeField] private WeaponPickup _requiredPickup;
        [Tooltip("Для стартовой комнаты: проход откроется, когда сломаны эти камеры")]
        [SerializeField] private CameraDestructionGoal _requiredCameras;
        [SerializeField] private LocationLoop _toLocation;
        [Tooltip("Входная дверь в _toLocation. Можно оставить пустым, если комнаты соединены открыто")]
        [SerializeField] private LocationDoor _door;
        [Tooltip("Ещё входные двери — ведут себя как _door")]
        [SerializeField] private LocationDoor[] _extraDoors;
        [Tooltip("Выходные двери из _fromLocation")]
        [SerializeField] private ExitDoor[] _exitDoors;
        [SerializeField] private PassageZone _roomEntryZone;

        [Header("Открытие при подходе")]
        [Tooltip("Дверь открывается, когда игрок ближе этого расстояния (по горизонтали) к её закрытому положению")]
        [SerializeField, Min(0.5f)] private float _openDistance = 3.5f;
        [Tooltip("Максимальная разница по высоте между игроком и дверью, чтобы она реагировала")]
        [SerializeField, Min(0.5f)] private float _maxHeightDifference = 3f;

        [Header("Транзит при старте с чекпоинта")]
        [Tooltip("Старт с чекпоинта этой локации идёт через уже пройденную комнату этого прохода")]
        [SerializeField] private LocationLoop _passThroughFor;

        private LocationDoor[] _entryDoors;
        private Vector3[] _entryClosedPositions;
        private bool[] _entryOpenedByGate;
        private Transform _player;

        private bool _isUnlocked;
        private bool _isPassThrough;
        private bool _isEntered;

        private void Awake()
        {
            int count = (_door != null ? 1 : 0) + (_extraDoors != null ? _extraDoors.Length : 0);
            _entryDoors = new LocationDoor[count];
            int index = 0;

            if (_door != null)
                _entryDoors[index++] = _door;

            if (_extraDoors != null)
            {
                foreach (LocationDoor door in _extraDoors)
                    _entryDoors[index++] = door;
            }

            _entryClosedPositions = new Vector3[count];
            _entryOpenedByGate = new bool[count];

            for (int i = 0; i < count; i++)
                _entryClosedPositions[i] = _entryDoors[i] != null ? _entryDoors[i].transform.position : transform.position;

            _exitDoors ??= Array.Empty<ExitDoor>();

            foreach (ExitDoor exit in _exitDoors)
            {
                if (exit.Door != null)
                    exit.ClosedPosition = exit.Door.transform.position;
            }
        }

        private void OnEnable()
        {
            if (_fromLocation != null)
                _fromLocation.Completed += OnFromLocationCompleted;

            if (_requiredPickup != null)
                _requiredPickup.PickedUp += OnPickedUp;

            if (_requiredCameras != null)
                _requiredCameras.Completed += OnCamerasBroken;

            _roomEntryZone.Entered += OnRoomEntered;
            _gameLoop.LocationChanged += OnLocationChanged;

            foreach (ExitDoor exit in _exitDoors ?? Array.Empty<ExitDoor>())
            {
                if (exit.LeftZone != null)
                    exit.LeftZone.Entered += OnAnyLeftZoneEntered;
            }
        }

        private void OnDisable()
        {
            if (_fromLocation != null)
                _fromLocation.Completed -= OnFromLocationCompleted;

            if (_requiredPickup != null)
                _requiredPickup.PickedUp -= OnPickedUp;

            if (_requiredCameras != null)
                _requiredCameras.Completed -= OnCamerasBroken;

            _roomEntryZone.Entered -= OnRoomEntered;
            _gameLoop.LocationChanged -= OnLocationChanged;

            foreach (ExitDoor exit in _exitDoors ?? Array.Empty<ExitDoor>())
            {
                if (exit.LeftZone != null)
                    exit.LeftZone.Entered -= OnAnyLeftZoneEntered;
            }
        }

        private void Start()
        {
            PlayerHealth player = FindAnyObjectByType<PlayerHealth>(FindObjectsInactive.Include);

            if (player != null)
                _player = player.transform;

            if (_fromLocation == null && _requiredPickup == null && _requiredCameras == null)
                _isUnlocked = true;
        }

        private void Update()
        {
            if (_player == null)
                return;

            Vector3 player = _player.position;

            if (_isUnlocked && !_isEntered)
            {
                for (int i = 0; i < _entryDoors.Length; i++)
                {
                    if (_entryDoors[i] == null)
                        continue;

                    bool isNear = IsNear(player, _entryClosedPositions[i]);

                    if (isNear == _entryOpenedByGate[i])
                        continue;

                    _entryOpenedByGate[i] = isNear;
                    SetOpen(_entryDoors[i], isNear);
                }
            }

            foreach (ExitDoor exit in _exitDoors)
            {
                if (!exit.IsActive || exit.IsLocked || exit.Door == null || !exit.OpenOnApproach)
                    continue;

                bool isNear = IsNear(player, exit.ClosedPosition);

                if (isNear == exit.IsOpenedByGate)
                    continue;

                exit.IsOpenedByGate = isNear;
                SetOpen(exit.Door, isNear);
            }
        }

        private bool IsNear(Vector3 player, Vector3 door)
        {
            Vector3 offset = player - door;

            return Mathf.Abs(offset.y) <= _maxHeightDifference
                   && new Vector2(offset.x, offset.z).sqrMagnitude <= _openDistance * _openDistance;
        }

        private static void SetOpen(LocationDoor door, bool open)
        {
            if (open)
                door.Open();
            else
                door.Close();
        }

        // Настоящее прохождение предыдущей локации: разблокируем вход и активируем выходные двери
        private void OnFromLocationCompleted(LocationLoop location)
        {
            _isUnlocked = true;

            foreach (ExitDoor exit in _exitDoors)
            {
                if (exit.IsLocked || exit.Door == null)
                    continue;

                exit.IsActive = true;

                if (!exit.OpenOnApproach)
                {
                    exit.IsOpenedByGate = true;
                    exit.Door.Open();
                }
            }
        }

        private void OnPickedUp() => _isUnlocked = true;

        private void OnCamerasBroken() => _isUnlocked = true;

        // Старт с чекпоинта: открываются только входные двери, выходные из пройденных локаций остаются закрытыми
        private void OnLocationChanged(LocationLoop location)
        {
            if (_isEntered)
                return;

            if (location == _toLocation)
            {
                _isPassThrough = false;
                _isUnlocked = true;
            }
            else if (_passThroughFor != null && location == _passThroughFor)
            {
                _isPassThrough = true;
                _isUnlocked = true;
            }
        }

        private void OnAnyLeftZoneEntered()
        {
            foreach (ExitDoor exit in _exitDoors)
            {
                if (!exit.IsActive || exit.IsLocked || exit.LeftZone == null)
                    continue;

                // Зона шлёт события каждый физкадр; выясняем, в какой из зон стоит игрок
                if (!IsInside(exit.LeftZone))
                    continue;

                exit.IsLocked = true;

                if (exit.IsOpenedByGate)
                {
                    exit.IsOpenedByGate = false;
                    exit.Door.Close();
                }
            }
        }

        private bool IsInside(PassageZone zone)
        {
            if (_player == null || !zone.TryGetComponent(out Collider area))
                return false;

            Vector3 point = area.ClosestPoint(_player.position);
            return (point - _player.position).sqrMagnitude < 0.0001f;
        }

        private void OnRoomEntered()
        {
            if (_isEntered || !_isUnlocked)
                return;

            if (!_isPassThrough)
            {
                if (_gameLoop.CurrentLocationLoop != _toLocation)
                    return;

                if (_door != null && !_door.IsOpen)
                    return;

                _gameLoop.LaunchLocation(_toLocation);
            }

            _isEntered = true;

            for (int i = 0; i < _entryDoors.Length; i++)
            {
                if (_entryDoors[i] == null || !_entryOpenedByGate[i])
                    continue;

                _entryOpenedByGate[i] = false;
                _entryDoors[i].Close();
            }
        }
    }
}
