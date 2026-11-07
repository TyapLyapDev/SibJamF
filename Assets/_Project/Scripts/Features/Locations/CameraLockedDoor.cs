using SibJam.SecurityCameras;
using UnityEngine;

namespace SibJam.Locations
{
    // Дверь, которую держат камеры: открывается, когда сломаны все камеры цели _cameras
    [RequireComponent(typeof(LocationDoor))]
    public class CameraLockedDoor : MonoBehaviour
    {
        [SerializeField] private CameraDestructionGoal _cameras;

        private LocationDoor _door;

        private void Awake() => _door = GetComponent<LocationDoor>();

        private void OnEnable()
        {
            if (_cameras != null)
                _cameras.Completed += Open;
        }

        private void OnDisable()
        {
            if (_cameras != null)
                _cameras.Completed -= Open;
        }

        private void Start()
        {
            if (_cameras == null || _cameras.IsCompleted)
                Open();
        }

        private void Open()
        {
            if (!_door.IsOpen)
                _door.Open();
        }
    }
}
