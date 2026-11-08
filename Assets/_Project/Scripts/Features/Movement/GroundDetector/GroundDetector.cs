using _Project.Scripts.Enemies;
using UnityEngine;

namespace SibJam
{
    // Земля = то, что под ногами: SphereCast вниз узкой сферой (уже капсулы) + проверка нормали
    // контакта против slopeLimit контроллера. Стены и склоны круче slopeLimit землёй не считаются.
    [RequireComponent(typeof(CharacterController))]
    public class GroundDetector : MonoBehaviour, IGroundDetector
    {
        private const int MaxHits = 10;

        [Tooltip("Доля радиуса капсулы для сферы проверки (<1, чтобы не цеплять стены)")]
        [SerializeField, Range(0.3f, 0.95f)] private float _radiusScale = 0.8f;
        [Tooltip("Как далеко под подошвой ещё считаем, что стоим на земле")]
        [SerializeField, Min(0.01f)] private float _groundDistance = 0.12f;
        [Tooltip("Насколько круче slopeLimit поверхность ещё годится, чтобы от неё оттолкнуться (не стоять)")]
        [SerializeField, Range(0f, 30f)] private float _jumpExtraAngle = 15f;
        [Tooltip("Сколько секунд после касания опоры снизу (по контакту контроллера) она ещё засчитывается")]
        [SerializeField, Min(0f)] private float _contactMemory = 0.06f;
#if UNITY_EDITOR
        [SerializeField] private bool _isOnGizmos;
        [SerializeField] private Color _sphereColor = Color.green;
#endif

        private CharacterController _controller;
        private readonly RaycastHit[] _hits = new RaycastHit[MaxHits];

        private int _cachedFrame = -1;
        private bool _cachedGround;
        private bool _cachedSupport;
        private bool _cachedSteep;
        private Vector3 _cachedNormal = Vector3.up;
        private Vector3 _cachedSteepNormal = Vector3.up;
        private float _lastContactSupportTime = float.NegativeInfinity;

        private void Awake() =>
            _controller = GetComponent<CharacterController>();

        public bool HasGround()
        {
            Evaluate();
            return _cachedGround;
        }

        // Прыжок мягче, чем «стоять»: годится и каст, и реальный контакт контроллера снизу.
        // Стена рядом этому не мешает — она просто не даёт опоры, но и не отнимает её у пола.
        public bool HasJumpSupport()
        {
            Evaluate();
            return _cachedSupport || Time.time - _lastContactSupportTime <= _contactMemory;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.collider == null || IsOwn(hit.collider))
                return;

            // Только контакт нижней частью капсулы с достаточно пологой поверхностью — не стенки
            float bottomY = transform.TransformPoint(_controller.center).y
                            - _controller.height * 0.5f + _controller.radius;
            if (hit.point.y > bottomY)
                return;

            if (Vector3.Angle(hit.normal, Vector3.up) <= _controller.slopeLimit + _jumpExtraAngle)
                _lastContactSupportTime = Time.time;
        }

        public bool TryGetGroundNormal(out Vector3 normal)
        {
            Evaluate();
            normal = _cachedGround ? _cachedNormal : Vector3.up;
            return _cachedGround;
        }

        public bool TryGetSteepSlopeNormal(out Vector3 normal)
        {
            Evaluate();
            normal = _cachedSteepNormal;
            return _cachedSteep;
        }

        private float CastRadius => _controller.radius * _radiusScale;

        // Центр сферы — чуть выше нижней полусферы капсулы, чтобы каст начинался изнутри персонажа
        private Vector3 CastOrigin
        {
            get
            {
                Vector3 bottom = transform.TransformPoint(_controller.center)
                                 + Vector3.down * (_controller.height * 0.5f);
                return bottom + Vector3.up * (_controller.radius + 0.05f);
            }
        }

        private float CastDistance => _controller.radius - CastRadius + 0.05f + _controller.skinWidth + _groundDistance;

        private void Evaluate()
        {
            if (_cachedFrame == Time.frameCount)
                return;

            _cachedFrame = Time.frameCount;
            _cachedGround = _cachedSupport = _cachedSteep = false;
            _cachedNormal = _cachedSteepNormal = Vector3.up;

            if (_controller == null)
                return;

            int count = Physics.SphereCastNonAlloc(CastOrigin, CastRadius, Vector3.down, _hits,
                CastDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

            float bestGround = float.MaxValue;
            float bestSteep = float.MaxValue;
            float maxAngle = _controller.slopeLimit;

            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _hits[i];

                // distance == 0 — сфера уже пересекалась на старте, нормаль недостоверна
                if (hit.collider == null || hit.distance <= 0f || IsOwn(hit.collider))
                    continue;

                Vector3 surfaceNormal = SurfaceNormal(hit);
                float angle = Vector3.Angle(surfaceNormal, Vector3.up);
                bool enemy = IsEnemy(hit.collider);

                if (angle <= maxAngle + _jumpExtraAngle)
                    _cachedSupport = true;

                if (angle <= maxAngle)
                {

                    if (enemy == false && hit.distance < bestGround)
                    {
                        bestGround = hit.distance;
                        _cachedGround = true;
                        _cachedNormal = surfaceNormal;
                    }
                }
                else if (enemy == false && angle < 89f && hit.distance < bestSteep)
                {
                    bestSteep = hit.distance;
                    _cachedSteep = true;
                    _cachedSteepNormal = surfaceNormal;
                }
            }

            // Стоим на земле — крутой край рядом уже не важен
            if (_cachedGround)
                _cachedSteep = false;
        }

        // Нормаль SphereCast на рёбрах сглажена. Сначала смотрим вертикальным лучом сверху в точку
        // контакта — так верх тонких перил/бордюров считается опорой, даже если сфера задела их ребро.
        // Боковая грань стены сверху лучом не находится, поэтому стены опорой не становятся.
        private Vector3 SurfaceNormal(RaycastHit hit)
        {
            Ray down = new(hit.point + Vector3.up * 0.1f, Vector3.down);

            if (hit.collider.Raycast(down, out RaycastHit top, 0.2f)
                && Vector3.Angle(top.normal, Vector3.up) <= _controller.slopeLimit)
                return top.normal;

            Vector3 origin = hit.point + hit.normal * 0.05f;

            if (hit.collider.Raycast(new Ray(origin, -hit.normal), out RaycastHit precise, 0.1f))
                return precise.normal;

            return hit.normal;
        }

        private bool IsOwn(Collider other) =>
            other.transform == transform || other.transform.IsChildOf(transform);

        // Враги не считаются землёй — по головам не стоим
        private static bool IsEnemy(Collider collider) =>
            collider.GetComponentInParent<EnemyMotor>() != null;

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (_isOnGizmos == false)
                return;

            CharacterController controller = GetComponent<CharacterController>();
            if (controller == null)
                return;

            _controller = controller;
            Gizmos.color = _sphereColor;
            Gizmos.DrawWireSphere(CastOrigin, CastRadius);
            Gizmos.DrawWireSphere(CastOrigin + Vector3.down * CastDistance, CastRadius);
        }
#endif
    }
}
