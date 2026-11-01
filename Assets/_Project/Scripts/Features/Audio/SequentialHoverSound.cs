using System;
using UnityEngine;
using VContainer;

namespace SibJam
{
    public class SequentialHoverSound : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private HoverNotifier[] _sources;

        [Header("Sound")]
        [SerializeField] private AudioClip[] _clips;

        [Header("Detection")]
        [Tooltip("Окно времени в секундах. Если за него не успели проиграть все клипы, индекс сбрасывается")]
        [SerializeField, Min(0.01f)] private float _resetTime = 2f;

        public event Action Triggered;

        public int CurrentIndex { get; private set; }

        private UiSfxService _uiSfxService;
        private float _resetAt;

        [Inject]
        public void Construct(UiSfxService uiSfxService)
        {
            _uiSfxService = uiSfxService;
        }

        private void OnEnable()
        {
            if (_sources == null)
                return;

            foreach (HoverNotifier source in _sources)
                if (source != null)
                    source.Hovered += OnSourceHovered;
        }

        private void OnDisable()
        {
            if (_sources == null)
                return;

            foreach (HoverNotifier source in _sources)
                if (source != null)
                    source.Hovered -= OnSourceHovered;
        }

        private void Update()
        {
            if (CurrentIndex == 0)
                return;

            if (Time.unscaledTime < _resetAt)
                return;

            CurrentIndex = 0;
            _resetAt = 0f;
        }

        private void OnSourceHovered()
        {
            if (_clips == null || _clips.Length == 0)
                return;

            _resetAt = Time.unscaledTime + _resetTime;

            _uiSfxService.Play(_clips[CurrentIndex]);
            CurrentIndex++;

            if (CurrentIndex < _clips.Length)
                return;

            CurrentIndex = 0;
            _resetAt = 0f;

            Triggered?.Invoke();
        }
    }
}