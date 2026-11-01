using System;
using SibJam.Checkpoints;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class GameSceneOpenerButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private ISceneLoader _sceneLoader;
        private ICheckpoint _checkpoint;

        [Inject]
        public void Construct(ISceneLoader sceneLoader, ICheckpoint checkpoint)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
            _checkpoint = checkpoint ?? throw new ArgumentNullException(nameof(checkpoint));
        }

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        // Новая игра: старый чекпоинт стирается, начинаем с катсцены
        private void OnClick()
        {
            _checkpoint.RequestNewGame();
            _sceneLoader.Load(SceneNames.Main);
        }
    }
}