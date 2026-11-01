using System;
using System.Collections;
using UnityEngine;

namespace SibJam
{
    public class HoverSpamEasterEgg : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private SequentialHoverSound _detector;

        [Header("View")]
        [SerializeField] private PopUp _window;
        [SerializeField, Min(0f)] private float _visibleDuration = 5f;

        private Coroutine _hideRoutine;
        private bool _fired;

        private void Awake()
        {
            if (_detector == null)
                throw new InvalidOperationException(
                    $"[HoverSpamEasterEgg] HoverSoundSpamDetector не назначен на '{name}'.");

            if (_window == null)
                throw new InvalidOperationException(
                    $"[HoverSpamEasterEgg] PopUp не назначен на '{name}'.");
        }

        private void OnEnable()
        {
            if (_fired)
                return;

            _detector.Triggered += OnTriggered;
        }

        private void OnDisable()
        {
            _detector.Triggered -= OnTriggered;

            StopHideRoutine();

            if (_window != null)
                _window.Hide();
        }

        private void OnTriggered()
        {
            _detector.Triggered -= OnTriggered;
            _fired = true;

            _window.Show();

            StopHideRoutine();
            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(_visibleDuration);

            _hideRoutine = null;

            if (_window != null)
                _window.Hide();
        }

        private void StopHideRoutine()
        {
            if (_hideRoutine == null)
                return;

            StopCoroutine(_hideRoutine);
            _hideRoutine = null;
        }
    }
}