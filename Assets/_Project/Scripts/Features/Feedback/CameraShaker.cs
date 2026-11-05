using UnityEngine;

namespace SibJam.Feedback
{
    // Тряска и толчки камеры для отдачи. Копит «травму» (тряска ~ травма²) и пружинный толчок поворота.
    // Висит на камере и сам накладывает результат поверх позы, которую на этом кадре поставили
    // CameraFollow или катсцена — им про тряску знать не нужно
    [DefaultExecutionOrder(300)]
    public class CameraShaker : MonoBehaviour
    {
        private const float MaxAngle = 4f;
        private const float TraumaDecay = 1.6f;
        private const float KickStiffness = 180f;
        private const float KickDamping = 18f;
        private const float DipStiffness = 140f;
        private const float DipDamping = 20f;
        private const float MaxDip = 0.6f;

        private float _trauma;
        private float _seed;
        private Vector3 _kick;
        private Vector3 _kickVelocity;
        private float _dip;
        private float _dipVelocity;

        // Поза без тряски и поза с ней: если камеру на кадре никто не двигал, трясём от прежней позы, а не копим сдвиг
        private Pose _base;
        private Pose _applied;
        private bool _hasApplied;

        private void Awake() => _seed = Random.value * 100f;

        // Травма от 0 до 1: тряска растёт как её квадрат и сама затухает
        public void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        // Мгновенный толчок поворота в градусах (x — кивок вниз/вверх, y — вбок, z — крен), пружиной возвращается обратно
        public void Kick(Vector3 eulerImpulse) => _kickVelocity += eulerImpulse * KickStiffness * 0.1f;

        // Присед камеры вниз (в метрах скорости), пружиной быстро поднимается обратно — приземление на колени
        public void Dip(float downwardSpeed) => _dipVelocity -= downwardSpeed;

        private void LateUpdate()
        {
            Transform cameraTransform = transform;
            bool untouched = _hasApplied
                && cameraTransform.position == _applied.position && cameraTransform.rotation == _applied.rotation;

            if (!untouched)
                _base = new Pose(cameraTransform.position, cameraTransform.rotation);

            // Пружины считаются явным шагом: на подвисании кадра (первый удар грузит эффекты) большой шаг их «взрывает».
            // Ограничиваем шаг — тряска на лаге просто чуть замедлится
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 1f / 30f);
            _applied = new Pose(_base.position + SampleOffset(deltaTime), _base.rotation * SampleRotation(deltaTime));
            cameraTransform.SetPositionAndRotation(_applied.position, _applied.rotation);
            _hasApplied = true;
        }

        private Vector3 SampleOffset(float deltaTime)
        {
            float acceleration = -_dip * DipStiffness - _dipVelocity * DipDamping;
            _dipVelocity += acceleration * deltaTime;
            _dip = Mathf.Max(_dip + _dipVelocity * deltaTime, -MaxDip);

            return new Vector3(0f, _dip, 0f);
        }

        private Quaternion SampleRotation(float deltaTime)
        {
            _trauma = Mathf.Max(0f, _trauma - TraumaDecay * deltaTime);

            Vector3 acceleration = -_kick * KickStiffness - _kickVelocity * KickDamping;
            _kickVelocity += acceleration * deltaTime;
            _kick += _kickVelocity * deltaTime;

            float shake = _trauma * _trauma * MaxAngle;
            float time = Time.unscaledTime * 25f;
            Vector3 noise = new Vector3(
                Mathf.PerlinNoise(_seed, time) * 2f - 1f,
                Mathf.PerlinNoise(_seed + 1f, time) * 2f - 1f,
                Mathf.PerlinNoise(_seed + 2f, time) * 2f - 1f);

            return Quaternion.Euler(_kick + noise * shake);
        }
    }
}
