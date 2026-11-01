using System;
using UnityEngine;

namespace SibJam
{
    public class HoverSoundSpamDetector : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private HoverSound[] _sources;

        [Header("Detection")]
        [Tooltip("Сколько срабатываний должно накопиться, чтобы детектор выстрелил")]
        [SerializeField, Min(1)] private int _requiredCount = 30;

        [Tooltip("Окно времени в секундах. Счётчик, не набравший нужное число за это время, обнуляется")]
        [SerializeField, Min(0.01f)] private float _resetTime = 2f;

        public event Action Triggered;

        public int CurrentCount { get; private set; }

        private float _resetAt;

        private void OnEnable()
        {
            if (_sources == null)
                return;

            foreach (HoverSound source in _sources)
                if (source != null)
                    source.Played += OnSourcePlayed;
        }

        private void OnDisable()
        {
            if (_sources == null)
                return;

            foreach (HoverSound source in _sources)
                if (source != null)
                    source.Played -= OnSourcePlayed;
        }

        private void Update()
        {
            if (CurrentCount == 0)
                return;

            if (Time.unscaledTime < _resetAt)
                return;

            CurrentCount = 0;
            _resetAt = 0f;
        }

        private void OnSourcePlayed()
        {
            _resetAt = Time.unscaledTime + _resetTime;

            CurrentCount++;

            if (CurrentCount < _requiredCount)
                return;

            CurrentCount = 0;
            _resetAt = 0f;

            Debug.Log(
                $"[HoverSoundSpamDetector] Порог {_requiredCount} срабатываний достигнут за {_resetTime:0.##} с на '{name}'.",
                this);

            Triggered?.Invoke();
        }
    }
}