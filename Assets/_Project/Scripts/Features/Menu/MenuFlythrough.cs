using UnityEngine;

namespace SibJam.MainMenu
{
    // Бесконечный пролёт по мосту в главном меню: камера плывёт вдоль оси, а пройдя один зал,
    // перескакивает назад ровно на его длину. Залы одинаковые, поэтому скачка не видно.
    // Лёгкое покачивание и дыхание камеры — шумом Перлина, чтобы пролёт не выглядел как рельса
    public class MenuFlythrough : MonoBehaviour
    {
        [SerializeField] private Vector3 _start = new Vector3(31f, 5f, 46.3f);
        [Tooltip("Направление полёта (мост идёт по X)")]
        [SerializeField] private Vector3 _direction = Vector3.right;
        [Tooltip("Длина одного зала: на столько камера перескакивает назад")]
        [SerializeField, Min(1f)] private float _segmentLength = 31.4f;
        [SerializeField, Min(0f)] private float _speed = 1.6f;

        [Header("Покачивание")]
        [SerializeField] private Vector3 _swayAmplitude = new Vector3(0f, 0.25f, 0.35f);
        [SerializeField, Min(0f)] private float _swaySpeed = 0.12f;
        [Tooltip("Наклон и рыскание, градусы")]
        [SerializeField] private Vector3 _tiltAmplitude = new Vector3(2.5f, 4f, 1.5f);
        [Tooltip("Взгляд чуть вниз, на мост")]
        [SerializeField] private float _pitch = 6f;
        [Tooltip("Взгляд вбок, градусы: мост уходит по диагонали, справа остаётся место под меню")]
        [SerializeField] private float _yawOffset = -14f;

        private float _distance;
        private float _seed;

        private void Awake() => _seed = Random.value * 100f;

        private void Update()
        {
            _distance = Mathf.Repeat(_distance + _speed * Time.unscaledDeltaTime, _segmentLength);

            float t = Time.unscaledTime * _swaySpeed + _seed;
            Vector3 dir = _direction.normalized;
            Quaternion baseRotation = Quaternion.LookRotation(dir, Vector3.up);

            Vector3 sway = new Vector3(
                Noise(t, 0f) * _swayAmplitude.x,
                Noise(t, 1f) * _swayAmplitude.y,
                Noise(t, 2f) * _swayAmplitude.z);

            transform.position = _start + dir * _distance + baseRotation * sway;
            transform.rotation = baseRotation * Quaternion.Euler(
                _pitch + Noise(t, 3f) * _tiltAmplitude.x,
                _yawOffset + Noise(t, 4f) * _tiltAmplitude.y,
                Noise(t, 5f) * _tiltAmplitude.z);
        }

        private static float Noise(float t, float channel) => Mathf.PerlinNoise(t, channel * 7.3f) * 2f - 1f;
    }
}
