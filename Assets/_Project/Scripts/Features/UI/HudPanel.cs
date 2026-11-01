using UnityEngine;

namespace SibJam
{
    // Плавно показывает и прячет блок HUD через CanvasGroup
    [RequireComponent(typeof(CanvasGroup))]
    public class HudPanel : MonoBehaviour
    {
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.25f;

        private CanvasGroup _group;
        private bool _visible;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = _visible ? 1f : 0f;
        }

        private void Update()
        {
            var target = _visible ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / _fadeDuration);
        }

        public void SetVisible(bool visible) => _visible = visible;
    }
}
