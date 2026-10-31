using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Locations
{
    // Коридор-сейфзона между локациями:
    // предыдущая пройдена (или её нет — стартовая сейф-комната, выход открывается после подбора оружия);
    // игрок зашёл в коридор → выход закрывается;
    // подошёл к дальней двери → следующая локация запускается (враги уже на месте), дверь открывается;
    // игрок вошёл в комнату → дверь за ним закрывается
    public class LocationPassage : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;
        [Tooltip("Пусто — стартовая сейф-комната: выход открыт сразу или после подбора _requiredPickup")]
        [SerializeField] private LocationLoop _fromLocation;
        [Tooltip("Для стартовой сейф-комнаты: выход откроется, только когда подобрано оружие")]
        [SerializeField] private WeaponPickup _requiredPickup;
        [SerializeField] private LocationLoop _toLocation;
        [SerializeField] private LocationDoor _exitDoor;
        [SerializeField] private LocationDoor _entryDoor;
        [SerializeField] private PassageZone _safeZone;
        [SerializeField] private PassageZone _entryApproachZone;
        [SerializeField] private PassageZone _roomEntryZone;

        private bool _isPlayerInside;
        private bool _isEntered;

        private void OnEnable()
        {
            if (_fromLocation != null)
                _fromLocation.Completed += OnFromLocationCompleted;

            if (_requiredPickup != null)
                _requiredPickup.PickedUp += OnPickedUp;

            _safeZone.Entered += OnSafeZoneEntered;
            _entryApproachZone.Entered += OnEntryApproached;
            _roomEntryZone.Entered += OnRoomEntered;
        }

        private void OnDisable()
        {
            if (_fromLocation != null)
                _fromLocation.Completed -= OnFromLocationCompleted;

            if (_requiredPickup != null)
                _requiredPickup.PickedUp -= OnPickedUp;

            _safeZone.Entered -= OnSafeZoneEntered;
            _entryApproachZone.Entered -= OnEntryApproached;
            _roomEntryZone.Entered -= OnRoomEntered;
        }

        private void Start()
        {
            if (_fromLocation == null && _requiredPickup == null)
                _exitDoor.Open();
        }

        private void OnFromLocationCompleted(LocationLoop location) => _exitDoor.Open();

        private void OnPickedUp() => _exitDoor.Open();

        private void OnSafeZoneEntered()
        {
            if (_isPlayerInside || !_exitDoor.IsOpen) return;

            _isPlayerInside = true;
            _exitDoor.Close();
        }

        private void OnEntryApproached()
        {
            if (!_isPlayerInside || _entryDoor.IsOpen || _isEntered) return;

            _gameLoop.LaunchLocation(_toLocation);
            _entryDoor.Open();
        }

        private void OnRoomEntered()
        {
            if (_isEntered || !_entryDoor.IsOpen) return;

            _isEntered = true;
            _entryDoor.Close();
        }
    }
}
