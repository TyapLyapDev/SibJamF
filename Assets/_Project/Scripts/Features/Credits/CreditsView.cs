using System.Collections;
using UnityEngine;

namespace SibJam.Credits
{
    // Финальные титры: экран темнеет, текст плывёт снизу вверх, в конце остаётся последняя строка
    public class CreditsView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _content;
        [SerializeField, Min(0.01f)] private float _fadeDuration = 1.5f;
        [SerializeField, Min(1f)] private float _scrollSpeed = 70f;

        public bool IsShowing { get; private set; }

        private void Awake()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _content.gameObject.SetActive(false);
        }

        public void Show()
        {
            if (IsShowing)
                return;

            IsShowing = true;
            StartCoroutine(Showing());
        }

        private IEnumerator Showing()
        {
            for (float time = 0f; time < _fadeDuration; time += Time.deltaTime)
            {
                _group.alpha = time / _fadeDuration;
                yield return null;
            }

            _group.alpha = 1f;
            _content.gameObject.SetActive(true);

            // Текст стартует под нижним краем экрана и останавливается, когда его низ дойдёт до середины
            var parent = (RectTransform)_content.parent;
            float start = -parent.rect.height * 0.5f - _content.rect.height * 0.5f;
            float end = _content.rect.height * 0.5f;
            _content.anchoredPosition = new Vector2(0f, start);

            while (_content.anchoredPosition.y < end)
            {
                _content.anchoredPosition += Vector2.up * (_scrollSpeed * Time.deltaTime);
                yield return null;
            }
        }
    }
}
