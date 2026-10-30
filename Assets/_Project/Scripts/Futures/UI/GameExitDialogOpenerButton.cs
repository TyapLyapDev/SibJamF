using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class GameExitDialogOpenerButton : MonoBehaviour
    {
        [SerializeField] private Button _button;
 
        private GameExitDialog _gameExitDialog;
        private PopUp _menuButtons;

        [Inject]
        public void Construct(GameExitDialog gameExitDialog, PopUp menuButtons)
        {
            _gameExitDialog = gameExitDialog != null ? gameExitDialog : throw new ArgumentNullException(nameof(gameExitDialog));
            _menuButtons = menuButtons != null ? menuButtons : throw new ArgumentNullException(nameof(menuButtons));
        }

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            _gameExitDialog.Show();
            _menuButtons.Hide();
        }
    }
}