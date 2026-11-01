using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Locations
{
    // Проход в одну дверь между соседними комнатами (без коридора-сейфзоны):
    // предыдущая локация пройдена (или подобрано оружие для стартовой) → дверь открывается;
    // игрок вошёл в комнату → следующая локация запускается, дверь за ним закрывается.
    // Без двери — просто триггер запуска локации, срабатывает только когда локация стала текущей
    public class LocationGate : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;
        [Tooltip("Пусто — стартовая комната: дверь откроется сразу или после подбора _requiredPickup")]
        [SerializeField] private LocationLoop _fromLocation;
        [SerializeField] private WeaponPickup _requiredPickup;
        [SerializeField] private LocationLoop _toLocation;
        [Tooltip("Можно оставить пустым, если комнаты соединены открыто")]
        [SerializeField] private LocationDoor _door;
        [Tooltip("Ещё двери на этом же проходе — открываются и закрываются вместе с _door")]
        [SerializeField] private LocationDoor[] _extraDoors;
        [SerializeField] private PassageZone _roomEntryZone;

        private bool _isEntered;

        private void OnEnable()
        {
            if (_fromLocation != null)
                _fromLocation.Completed += OnFromLocationCompleted;

            if (_requiredPickup != null)
                _requiredPickup.PickedUp += OnPickedUp;

            _roomEntryZone.Entered += OnRoomEntered;
            _gameLoop.LocationChanged += OnLocationChanged;
        }

        private void OnDisable()
        {
            if (_fromLocation != null)
                _fromLocation.Completed -= OnFromLocationCompleted;

            if (_requiredPickup != null)
                _requiredPickup.PickedUp -= OnPickedUp;

            _roomEntryZone.Entered -= OnRoomEntered;
            _gameLoop.LocationChanged -= OnLocationChanged;
        }

        private void Start()
        {
            if (_fromLocation == null && _requiredPickup == null)
                OpenDoor();
        }

        private void OnFromLocationCompleted(LocationLoop location) => OpenDoor();

        private void OnPickedUp() => OpenDoor();

        // Старт с чекпоинта: предыдущая локация не проходилась в этой сессии, но дверь в текущую должна быть открыта
        private void OnLocationChanged(LocationLoop location)
        {
            if (location == _toLocation && !_isEntered)
                OpenDoor();
        }

        private void OpenDoor()
        {
            if (_door != null)
                _door.Open();

            foreach (LocationDoor door in _extraDoors)
                door.Open();
        }

        private void OnRoomEntered()
        {
            if (_isEntered || _gameLoop.CurrentLocationLoop != _toLocation) return;
            if (_door != null && !_door.IsOpen) return;

            _isEntered = true;
            _gameLoop.LaunchLocation(_toLocation);

            if (_door != null)
                _door.Close();

            foreach (LocationDoor door in _extraDoors)
                door.Close();
        }
    }
}
