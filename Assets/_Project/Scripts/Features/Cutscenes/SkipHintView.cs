using UnityEngine;

namespace SibJam.Cutscenes
{
    // Подсказка «нажми ещё раз, чтобы пропустить» на сервисном канвасе. Показывает её сервис SkipHint
    public class SkipHintView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        public float Alpha
        {
            get => _canvasGroup.alpha;
            set => _canvasGroup.alpha = value;
        }

        private void Reset() => _canvasGroup = GetComponent<CanvasGroup>();
    }
}
