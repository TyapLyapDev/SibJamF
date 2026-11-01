using System;
using SibJam.Checkpoints;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    // «Продолжить» в главном меню: неактивна, пока игра ни разу не доходила до чекпоинта
    public class ContinueFromCheckpointButton : MonoBehaviour
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

        private void Start() =>
            _button.interactable = _checkpoint.HasSaved;

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
            _checkpoint.RequestContinue();
            _sceneLoader.Load(SceneNames.Main);
        }
    }
}
