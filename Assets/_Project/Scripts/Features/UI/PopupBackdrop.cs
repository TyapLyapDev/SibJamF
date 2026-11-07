using UnityEngine;
using UnityEngine.UI;

namespace SibJam
{
    // Тёмная подложка со строками, как на мониторе наблюдения: видна, пока открыто любое из окон (пауза, настройки, выход)
    public class PopupBackdrop : MonoBehaviour
    {
        [SerializeField] private Graphic _view;
        [SerializeField] private GameObject[] _popups;

        private Graphic[] _graphics;

        private void Awake() => _graphics = GetComponentsInChildren<Graphic>(true);

        private void LateUpdate()
        {
            bool visible = false;

            foreach (GameObject popup in _popups)
            {
                if (popup != null && popup.activeInHierarchy)
                {
                    visible = true;
                    break;
                }
            }

            if (_view.enabled == visible)
                return;

            foreach (Graphic graphic in _graphics)
                graphic.enabled = visible;
        }
    }
}
