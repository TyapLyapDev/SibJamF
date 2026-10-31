using System;
using System.Collections;
using UnityEngine;

namespace SibJam.Locations
{
    public class LocationDoor : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _liftHeight = 4.5f;
        [SerializeField, Min(0.01f)] private float _duration = 1.2f;

        private Vector3 _closedPosition;
        private Coroutine _moving;

        public bool IsOpen { get; private set; }

        private void Awake() => _closedPosition = transform.localPosition;

        public void Open(Action opened = null) => MoveTo(true, opened);

        public void Close(Action closed = null) => MoveTo(false, closed);

        private void MoveTo(bool open, Action finished)
        {
            IsOpen = open;

            if (_moving != null)
                StopCoroutine(_moving);

            Vector3 target = open ? _closedPosition + Vector3.up * _liftHeight : _closedPosition;
            _moving = StartCoroutine(Move(target, finished));
        }

        private IEnumerator Move(Vector3 target, Action finished)
        {
            Vector3 start = transform.localPosition;

            for (float time = 0f; time < _duration; time += Time.deltaTime)
            {
                transform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, time / _duration));
                yield return null;
            }

            transform.localPosition = target;
            _moving = null;
            finished?.Invoke();
        }
    }
}
