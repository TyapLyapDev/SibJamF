using UnityEngine;
using UnityEngine.UI;

namespace SibJam
{
    public class GameExitAcceptButton : MonoBehaviour
    {
        [SerializeField] private Button _button;

        private void OnEnable() =>
            _button.onClick.AddListener(OnClick);

        private void OnDisable() =>
            _button.onClick.RemoveListener(OnClick);

        private void OnClick()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}