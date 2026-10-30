using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class GameExitDeclineButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private GameExitDialog _gameExitDialog;
        private MenuButtons _menuButtons;

        [Inject]
        public void Construct(GameExitDialog gameExitDialog, MenuButtons menuButtons)
        {
            _gameExitDialog = gameExitDialog ?? throw new ArgumentNullException(nameof(gameExitDialog));
            _menuButtons = menuButtons ?? throw new ArgumentNullException(nameof(menuButtons));
        }

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            _gameExitDialog.Hide();
            _menuButtons.Show();
        }
    }
}