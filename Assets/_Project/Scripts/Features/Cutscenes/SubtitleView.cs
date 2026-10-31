using System.Collections;
using TMPro;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Субтитр внизу экрана: имя говорящего цветом и текст, плавно появляется и гаснет
    public class SubtitleView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private Color _speakerColor = new Color(1f, 0.75f, 0.35f);
        [SerializeField, Min(0.01f)] private float _fadeDuration = 0.2f;

        private Coroutine _showing;

        private void Awake() => _group.alpha = 0f;

        public void Show(string speaker, string text, float duration)
        {
            _text.text = string.IsNullOrEmpty(speaker)
                ? text
                : $"<color=#{ColorUtility.ToHtmlStringRGB(_speakerColor)}>{speaker}:</color> {text}";

            if (_showing != null)
                StopCoroutine(_showing);

            _showing = StartCoroutine(Showing(duration));
        }

        public void Hide()
        {
            if (_showing != null)
                StopCoroutine(_showing);

            _showing = null;
            _group.alpha = 0f;
        }

        private IEnumerator Showing(float duration)
        {
            yield return Fade(1f);
            yield return new WaitForSeconds(duration);
            yield return Fade(0f);
            _showing = null;
        }

        private IEnumerator Fade(float target)
        {
            float start = _group.alpha;

            for (float time = 0f; time < _fadeDuration; time += Time.deltaTime)
            {
                _group.alpha = Mathf.Lerp(start, target, time / _fadeDuration);
                yield return null;
            }

            _group.alpha = target;
        }
    }
}
