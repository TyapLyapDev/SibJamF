using UnityEngine;

namespace SibJam.Feedback
{
    // Тряска и толчки камеры для отдачи. Копит «травму» (тряска ~ травма²) и пружинный толчок поворота;
    // CameraFollow применяет результат поверх цели
    public static class CameraShake
    {
        private const float MaxAngle = 4f;
        private const float TraumaDecay = 1.6f;
        private const float KickStiffness = 180f;
        private const float KickDamping = 18f;
        private const float DipStiffness = 140f;
        private const float DipDamping = 20f;
        private const float MaxDip = 0.6f;

        private static float _trauma;
        private static float _seed = Random.value * 100f;
        private static Vector3 _kick;
        private static Vector3 _kickVelocity;
        private static float _dip;
        private static float _dipVelocity;

        public static void AddTrauma(float amount) => _trauma = Mathf.Clamp01(_trauma + amount);

        // Мгновенный толчок поворота в градусах (x — кивок вниз/вверх, y — вбок, z — крен), пружиной возвращается обратно
        public static void Kick(Vector3 eulerImpulse) => _kickVelocity += eulerImpulse * KickStiffness * 0.1f;

        // Присед камеры вниз (в метрах скорости), пружиной быстро поднимается обратно — приземление на колени
        public static void Dip(float downwardSpeed) => _dipVelocity -= downwardSpeed;

        // Вертикальное смещение камеры от приседа, вызывается раз в кадр вместе с Sample
        public static Vector3 SampleOffset(float deltaTime)
        {
            float acceleration = -_dip * DipStiffness - _dipVelocity * DipDamping;
            _dipVelocity += acceleration * deltaTime;
            _dip = Mathf.Max(_dip + _dipVelocity * deltaTime, -MaxDip);

            return new Vector3(0f, _dip, 0f);
        }

        public static Quaternion Sample(float deltaTime)
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
