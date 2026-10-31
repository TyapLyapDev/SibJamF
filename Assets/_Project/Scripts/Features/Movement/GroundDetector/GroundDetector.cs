using System.Collections.Generic;
using UnityEngine;

namespace SibJam
{
    public class GroundDetector : MonoBehaviour, IGroundDetector
    {
        private const int MaxGroundColliders = 10;
        private const float RaycastLengthOffset = 0.1f;

        [SerializeField] private float _verticalOffset;
        [SerializeField] private float _sphereRadius;
#if UNITY_EDITOR
        [SerializeField] private bool _isOnGizmos;
        [SerializeField] private Color _sphereColor;
#endif

        private HashSet<Collider> _ownColliders;

        private readonly Collider[] _hitColliders = new Collider[MaxGroundColliders];
        private readonly RaycastHit[] _hitRaycasts = new RaycastHit[MaxGroundColliders];

        private void Awake() =>
            _ownColliders = new HashSet<Collider>(transform.GetComponentsInChildren<Collider>());

        private Vector3 Center => transform.transform.position + Vector3.up * _verticalOffset;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_isOnGizmos == false)
                return;

            Vector3 center = transform.position + Vector3.up * _verticalOffset;

            Gizmos.color = _sphereColor;
            Gizmos.DrawWireSphere(center, _sphereRadius);
        }
#endif

        public bool TryGetGroundNormal(out Vector3 normal)
        {
            normal = Vector3.up;

            if (HasGround() == false)
                return false;

            float rayLength = _sphereRadius + RaycastLengthOffset;
            Ray ray = new(Center, Vector3.down);

            int hitCount = Physics.RaycastNonAlloc(
                ray,
                _hitRaycasts,
                rayLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitRaycasts[i];

                if (_ownColliders.Contains(hit.collider))
                    continue;

                normal = hit.normal;

                return true;
            }

            return false;
        }

        public bool HasGround()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                Center,
                _sphereRadius,
                _hitColliders,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = _hitColliders[i];

                if (_ownColliders.Contains(hit))
                    continue;

                return true;
            }

            return false;
        }
    }
}