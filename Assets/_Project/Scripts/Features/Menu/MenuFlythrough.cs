using System.Collections.Generic;
using UnityEngine;

namespace SibJam.MainMenu
{
    // Бесконечный пролёт по мосту в главном меню, «беговая дорожка»: камера висит на месте и только покачивается,
    // а залы моста едут ей навстречу. Зал, целиком ушедший за спину камеры, переставляется в начало очереди —
    // это происходит вне кадра, поэтому склейки не видно. Залы — одинаковые копии длиной _segmentLength
    public class MenuFlythrough : MonoBehaviour
    {
        [Tooltip("Родитель залов: дети — одинаковые копии, стоящие друг за другом вдоль _direction")]
        [SerializeField] private Transform _segmentsRoot;
        [SerializeField] private Vector3 _cameraPoint = new Vector3(62f, 5f, 46.3f);
        [Tooltip("Направление полёта (мост идёт по X)")]
        [SerializeField] private Vector3 _direction = Vector3.right;
        [Tooltip("Длина одного зала")]
        [SerializeField, Min(1f)] private float _segmentLength = 31.4f;
        [SerializeField, Min(0f)] private float _speed = 1.6f;
        [Tooltip("Зал переставляется, когда его конец ушёл за камеру дальше этого, м")]
        [SerializeField, Min(0f)] private float _behindMargin = 6f;

        [Header("Покачивание")]
        [SerializeField] private Vector3 _swayAmplitude = new Vector3(0f, 0.25f, 0.35f);
        [SerializeField, Min(0f)] private float _swaySpeed = 0.12f;
        [Tooltip("Наклон и рыскание, градусы")]
        [SerializeField] private Vector3 _tiltAmplitude = new Vector3(2.5f, 4f, 1.5f);
        [Tooltip("Взгляд чуть вниз, на мост")]
        [SerializeField] private float _pitch = 6f;
        [Tooltip("Взгляд вбок, градусы: мост уходит по диагонали, слева остаётся место под меню")]
        [SerializeField] private float _yawOffset = -14f;

        private readonly List<Transform> _segments = new List<Transform>();
        private float _seed;

        private void Awake()
        {
            _seed = Random.value * 100f;

            if (_segmentsRoot != null)
                foreach (Transform segment in _segmentsRoot)
                    _segments.Add(segment);

            SyncParticles();
        }

        private void Update()
        {
            Vector3 dir = _direction.normalized;
            MoveWorld(dir);

            float t = Time.unscaledTime * _swaySpeed + _seed;
            Quaternion baseRotation = Quaternion.LookRotation(dir, Vector3.up);

            Vector3 sway = new Vector3(
                Noise(t, 0f) * _swayAmplitude.x,
                Noise(t, 1f) * _swayAmplitude.y,
                Noise(t, 2f) * _swayAmplitude.z);

            transform.position = _cameraPoint + baseRotation * sway;
            transform.rotation = baseRotation * Quaternion.Euler(
                _pitch + Noise(t, 3f) * _tiltAmplitude.x,
                _yawOffset + Noise(t, 4f) * _tiltAmplitude.y,
                Noise(t, 5f) * _tiltAmplitude.z);
        }

        private void MoveWorld(Vector3 dir)
        {
            if (_segments.Count == 0)
                return;

            // Шаг ограничен: на первом кадре после загрузки сцены deltaTime огромный
            Vector3 step = -dir * (_speed * Mathf.Min(Time.unscaledDeltaTime, 0.1f));
            float cameraAlong = Vector3.Dot(_cameraPoint, dir);
            float farthest = float.MinValue;

            foreach (Transform segment in _segments)
            {
                segment.position += step;
                farthest = Mathf.Max(farthest, Vector3.Dot(segment.position, dir));
            }

            // Ушедший за спину зал встаёт в начало очереди, сразу за самым дальним
            foreach (Transform segment in _segments)
            {
                float end = Vector3.Dot(segment.position, dir) + _segmentLength * 0.5f;

                if (end >= cameraAlong - _behindMargin)
                    continue;

                float shift = farthest + _segmentLength - Vector3.Dot(segment.position, dir);
                segment.position += dir * shift;
                farthest += _segmentLength;
            }
        }

        // Пар во всех залах с одним зерном и одновременным стартом — облака одинаковые в каждой копии
        private static void SyncParticles()
        {
            foreach (ParticleSystem particles in FindObjectsByType<ParticleSystem>(FindObjectsInactive.Exclude))
            {
                if (particles.transform.parent != null && particles.transform.parent.GetComponentInParent<ParticleSystem>() != null)
                    continue;

                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                particles.useAutoRandomSeed = false;
                particles.randomSeed = (uint)Mathf.Abs(particles.name.GetHashCode());
                particles.Play(true);
            }
        }

        private static float Noise(float t, float channel) => Mathf.PerlinNoise(t, channel * 7.3f) * 2f - 1f;
    }
}
