using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace SibJam
{
    [DisallowMultipleComponent]
    public class SliderSfxPreview : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Sound")]
        [SerializeField] private AudioClip[] _clips;
        [SerializeField, Min(0.02f)] private float _interval = 0.15f;

        private UiSfxService _uiSfxService;
        private bool _isPressed;
        private float _nextPlayTime;
        private int _lastIndex = -1;

        [Inject]
        public void Construct(UiSfxService uiSfxService)
        {
            _uiSfxService = uiSfxService;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isPressed = true;
            _nextPlayTime = Time.unscaledTime;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            _isPressed = false;
        }

        private void OnDisable()
        {
            _isPressed = false;
        }

        private void Update()
        {
            if (!_isPressed)
                return;

            if (Time.unscaledTime < _nextPlayTime)
                return;

            _nextPlayTime = Time.unscaledTime + _interval;
            PlayRandomClip();
        }

        private void PlayRandomClip()
        {
            if (_clips == null || _clips.Length == 0)
                return;

            int index;

            if (_clips.Length == 1)
            {
                index = 0;
            }
            else
            {
                do
                {
                    index = Random.Range(0, _clips.Length);
                }
                while (index == _lastIndex);
            }

            _lastIndex = index;
            _uiSfxService.Play(_clips[index]);
        }
    }
}