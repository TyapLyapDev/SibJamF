using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SibJam.SecurityCameras.UI
{
    // Ряд иконок камер: целые — яркие, сломанные — тусклые с крестом
    public class SecurityCamerasView : MonoBehaviour
    {
        [SerializeField] private CurrentLocationInfo _currentLocation;
        [SerializeField] private HudPanel _panel;
        [SerializeField] private TMP_Text _camerasText;
        [SerializeField] private Transform _iconsParent;
        [SerializeField] private Image _iconPrefab;
        [SerializeField] private Sprite _aliveSprite;
        [SerializeField] private Sprite _brokenSprite;
        [SerializeField] private Color _aliveColor = Color.white;
        [SerializeField] private Color _brokenColor = new(1f, 0.3f, 0.25f, 0.6f);

        private readonly List<Image> _icons = new();
        private ICameraDestructionGoalInfo _cameras;

        private void Start() => Bind();

        private void OnEnable()
        {
            _currentLocation.LocationChanged += Bind;
            _currentLocation.ActiveChanged += RefreshVisibility;

            Bind();
        }

        private void OnDisable()
        {
            _currentLocation.LocationChanged -= Bind;
            _currentLocation.ActiveChanged -= RefreshVisibility;
            Unbind();
        }

        private void Bind()
        {
            Unbind();

            _cameras = _currentLocation.Cameras;
            if (_cameras == null)
            {
                _panel.SetVisible(false);
                return;
            }
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
            _camerasText.text = _cameras.LeftCount > 0 ? $"{_cameras.LeftCount}/{_cameras.TotalCount}" : "Чисто";

            EnsureIcons(_cameras.TotalCount);
            for (var i = 0; i < _icons.Count; i++)
            {
                var broken = i < _cameras.BrokenCount;
                _icons[i].sprite = broken && _brokenSprite != null ? _brokenSprite : _aliveSprite;
                _icons[i].color = broken ? _brokenColor : _aliveColor;
            }

            RefreshVisibility();
        }

        private void RefreshVisibility()
        {
            _panel.SetVisible(_cameras != null && _cameras.TotalCount > 0 && _currentLocation.IsActive);
        }

        private void EnsureIcons(int count)
        {
            while (_icons.Count < count)
            {
                var icon = Instantiate(_iconPrefab, _iconsParent);
                icon.gameObject.SetActive(true);
                _icons.Add(icon);
            }

            while (_icons.Count > count)
            {
                Destroy(_icons[^1].gameObject);
                _icons.RemoveAt(_icons.Count - 1);
            }
        }
    }
}
