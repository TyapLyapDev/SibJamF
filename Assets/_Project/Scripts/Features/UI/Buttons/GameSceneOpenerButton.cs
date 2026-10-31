using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class GameSceneOpenerButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private ISceneLoader _sceneLoader;

        [Inject]
        public void Construct(ISceneLoader sceneLoader) =>
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick() =>
            _sceneLoader.Load(SceneNames.Game);
    }
}