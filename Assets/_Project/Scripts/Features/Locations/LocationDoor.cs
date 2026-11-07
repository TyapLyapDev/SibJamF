using System;
using System.Collections;
using UnityEngine;

namespace SibJam.Locations
{
    public class LocationDoor : MonoBehaviour
    {
        private const float MinBlockerThickness = 0.4f;

        [SerializeField, Min(0f)] private float _liftHeight = 4.5f;
        [Tooltip("Куда уезжает дверь, в осях родителя: вверх — подъёмная, вбок — откатная")]
        [SerializeField] private Vector3 _openDirection = Vector3.up;
        [SerializeField, Min(0.01f)] private float _duration = 1.2f;

        private Vector3 _closedPosition;
        private Coroutine _moving;
        private Collider _blocker;

        public bool IsOpen { get; private set; }

        private void Awake()
        {
            _closedPosition = transform.localPosition;
            _blocker = CreateBlocker();
        }

        public void Open(Action opened = null)
        {
            SetBlocked(false);
            MoveTo(true, opened);
        }

        // Невидимая стена встаёт сразу: пока дверь едет вниз, под ней не проскочить назад
        public void Close(Action closed = null)
        {
            SetBlocked(true);
            MoveTo(false, closed);
        }

        private void SetBlocked(bool blocked)
        {
            if (_blocker != null)
                _blocker.enabled = blocked;
        }

        // Стена — сосед двери в закрытой позиции, размером с её меш (толщина не меньше MinBlockerThickness)
        private Collider CreateBlocker()
        {
            Bounds bounds;

            if (TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null)
                bounds = filter.sharedMesh.bounds;
            else if (TryGetComponent(out MeshCollider meshCollider) && meshCollider.sharedMesh != null)
                bounds = meshCollider.sharedMesh.bounds;
            else if (TryGetComponent(out BoxCollider box))
                bounds = new Bounds(box.center, box.size);
            else
                return null;

            var blocker = new GameObject(name + "_Blocker") { layer = gameObject.layer };
            blocker.transform.SetParent(transform.parent, false);
            blocker.transform.localPosition = _closedPosition;
            blocker.transform.localRotation = transform.localRotation;
            blocker.transform.localScale = transform.localScale;

            Vector3 scale = transform.lossyScale;
            Vector3 size = bounds.size;
            size.x = Mathf.Max(size.x, MinBlockerThickness / Mathf.Max(Mathf.Abs(scale.x), 0.0001f));
            size.y = Mathf.Max(size.y, MinBlockerThickness / Mathf.Max(Mathf.Abs(scale.y), 0.0001f));
            size.z = Mathf.Max(size.z, MinBlockerThickness / Mathf.Max(Mathf.Abs(scale.z), 0.0001f));

            var collider = blocker.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = size;
            collider.enabled = false;
            return collider;
        }

        private void MoveTo(bool open, Action finished)
        {
            IsOpen = open;

            if (_moving != null)
                StopCoroutine(_moving);

            Vector3 target = open ? _closedPosition + _openDirection.normalized * _liftHeight : _closedPosition;
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
