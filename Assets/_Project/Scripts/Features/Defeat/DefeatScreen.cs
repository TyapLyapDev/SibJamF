using System;
using System.Collections;
using SibJam.Checkpoints;
using SibJam.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

namespace SibJam.Defeat
{
    // Смерть игрока: экран краснеет и темнеет, затем окно «Заново с чекпоинта» / «В меню»
    public class DefeatScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _fade;
        [SerializeField] private GameObject _window;
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _menuButton;
        [SerializeField, Min(0f)] private float _fadeDuration = 1.2f;

        private IPlayerHealthInfo _health;
        private ICheckpoint _checkpoint;
        private ISceneLoader _sceneLoader;
        private IPauseSwitcher _pauseSwitcher;
        private ICursorShower _cursorShower;
        private IPlayerInputReader _playerInput;
        private GameButtonsShower _gameButtonsShower;

        [Inject]
        public void Construct(
            IPlayerHealthInfo health,
            ICheckpoint checkpoint,
            ISceneLoader sceneLoader,
            IPauseSwitcher pauseSwitcher,
            ICursorShower cursorShower,
            IPlayerInputReader playerInput,
            GameButtonsShower gameButtonsShower)
        {
            _health = health ?? throw new ArgumentNullException(nameof(health));
            _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _pauseSwitcher = pauseSwitcher ?? throw new ArgumentNullException(nameof(pauseSwitcher));
            _cursorShower = cursorShower ?? throw new ArgumentNullException(nameof(cursorShower));
            _playerInput = playerInput ?? throw new ArgumentNullException(nameof(playerInput));
            _gameButtonsShower = gameButtonsShower ?? throw new ArgumentNullException(nameof(gameButtonsShower));
        }

        private void Awake()
        {
            _fade.alpha = 0f;
            _fade.blocksRaycasts = false;
            _window.SetActive(false);
        }

        private void OnEnable()
        {
            _health.Died += OnPlayerDied;
            _retryButton.onClick.AddListener(OnRetry);
            _menuButton.onClick.AddListener(OnMenu);
        }

        private void OnDisable()
        {
            _health.Died -= OnPlayerDied;
            _retryButton.onClick.RemoveListener(OnRetry);
            _menuButton.onClick.RemoveListener(OnMenu);
        }

        private void OnPlayerDied() => StartCoroutine(Show());

        private IEnumerator Show()
        {
            _gameButtonsShower.Hide();
            _gameButtonsShower.Lock();
            _playerInput.Disable();
            _fade.blocksRaycasts = true;

            for (float time = 0f; time < _fadeDuration; time += Time.unscaledDeltaTime)
            {
                _fade.alpha = time / _fadeDuration;
                yield return null;
            }

            _fade.alpha = 1f;
            _pauseSwitcher.Pause();
            _cursorShower.Show();
            _window.SetActive(true);
        }

        private void OnRetry()
        {
            _checkpoint.RequestContinue();
            Leave(SceneManager.GetActiveScene().name);
        }

        private void OnMenu() => Leave(SceneNames.Menu);

        private void Leave(string sceneName)
        {
            _pauseSwitcher.Unpause();
            _sceneLoader.Load(sceneName);
        }
    }
}
