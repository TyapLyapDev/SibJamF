using TMPro;
using UnityEngine;

namespace SibJam.SecurityCameras.UI
{
    public class SecurityCamerasView : MonoBehaviour
    {
        [SerializeField] private CurrentLocationInfo _currentLocation;
        [SerializeField] private TMP_Text _camerasText;

        private ICameraDestructionGoalInfo _cameras;

        private void Start() => Bind();

        private void OnEnable()
        {
            _currentLocation.LocationChanged += Bind;

            if (_cameras != null)
                Bind();
        }

        private void OnDisable()
        {
            _currentLocation.LocationChanged -= Bind;
            Unbind();
        }

        private void Bind()
        {
            Unbind();

            _cameras = _currentLocation.Cameras;
            _cameras.Changed += Refresh;
            Refresh();
        }

        private void Unbind()
        {
            if (_cameras != null)
                _cameras.Changed -= Refresh;

            _cameras = null;
        }

        private void Refresh()
        {
            _camerasText.text = _cameras.LeftCount > 0
                ? $"Камер осталось: {_cameras.LeftCount}/{_cameras.TotalCount}"
                : "Все камеры сломаны";
        }
    }
}
