using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class ContinueGameButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private GameButtonsShower _gameButtonsShower;

        [Inject]
        public void Construct(GameButtonsShower gameButtonsShower)
        {
            _gameButtonsShower = gameButtonsShower != null ? gameButtonsShower : throw new ArgumentNullException(nameof(gameButtonsShower));
        }

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            _gameButtonsShower.Hide();
        }
    }
}