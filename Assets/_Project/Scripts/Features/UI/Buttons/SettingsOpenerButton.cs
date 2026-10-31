using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class SettingsOpenerButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private PopUp _buttonsPopUp;
        private Settings _setting;

        [Inject]
        public void Construct(PopUp buttonsPopUp, Settings settings)
        {
            _setting = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
            _buttonsPopUp = buttonsPopUp != null ? buttonsPopUp : throw new ArgumentNullException(nameof(buttonsPopUp));
        }

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            _buttonsPopUp.Hide();
            _setting.Show();
        }
    }
}