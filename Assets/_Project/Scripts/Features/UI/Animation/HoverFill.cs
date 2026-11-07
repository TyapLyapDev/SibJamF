using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SibJam
{
    // Кнопка в стиле «мониторов наблюдения»: при наведении заливается цветом тревоги, текст темнеет.
    // Главная кнопка экрана залита всегда (_isPrimary), при наведении чуть светлеет
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public class HoverFill : MonoBehaviour
    {
        [SerializeField] private Image _fill;
        [SerializeField] private Graphic _frame;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private bool _isPrimary;
        [SerializeField] private Color _normalFill = new Color(0.055f, 0.06f, 0.075f, 0.6f);
        [SerializeField] private Color _hoverFill = new Color(0.898f, 0.282f, 0.302f, 1f);
        [SerializeField] private Color _primaryHoverFill = new Color(0.96f, 0.42f, 0.44f, 1f);
        [SerializeField] private Color _lightText = new Color(0.925f, 0.906f, 0.867f, 1f);
        [SerializeField] private Color _darkText = new Color(0.043f, 0.047f, 0.059f, 1f);
        [SerializeField, Min(0f)] private float _duration = 0.12f;

        [SerializeField] private Color _disabledText = new Color(0.416f, 0.427f, 0.463f, 1f);

        private readonly List<RaycastResult> _raycastResults = new();
        private Selectable _selectable;
        private bool _isHovered;
        private bool _isInteractable = true;
        private Tween _fillTween;
        private Tween _textTween;

        private void OnEnable()
        {
            _selectable = GetComponent<Selectable>();
            _isHovered = false;
            _isInteractable = IsInteractable();
            Apply(false, instant: true);
        }

        private bool IsInteractable() => _selectable == null || _selectable.IsInteractable();

        private void OnDisable()
        {
            _fillTween?.Kill();
            _textTween?.Kill();
        }

        private void Update()
        {
            bool interactable = IsInteractable();
            bool hovered = interactable && UiPointer.IsOver(transform, _raycastResults);

            if (hovered == _isHovered && interactable == _isInteractable)
                return;

            _isHovered = hovered;
            _isInteractable = interactable;
            Apply(hovered, instant: false);
        }

        private void Apply(bool hovered, bool instant)
        {
            Color fill = _isPrimary ? (hovered ? _primaryHoverFill : _hoverFill) : (hovered ? _hoverFill : _normalFill);
            Color text = _isPrimary || hovered ? _darkText : _lightText;
            bool framed = !_isPrimary && !hovered;

            // Недоступная кнопка (например, «Продолжить» без сохранения): без заливки, приглушённый текст, рамка
            if (!_isInteractable)
            {
                fill = _normalFill;
                text = _disabledText;
                framed = true;
            }

            if (_frame != null)
                _frame.enabled = framed;

            _fillTween?.Kill();
            _textTween?.Kill();

            if (instant || _duration <= 0f)
            {
                if (_fill != null) _fill.color = fill;
                if (_text != null) _text.color = text;
                return;
            }

            if (_fill != null) _fillTween = _fill.DOColor(fill, _duration).SetUpdate(true);
            if (_text != null) _textTween = _text.DOColor(text, _duration).SetUpdate(true);
        }
    }
}
