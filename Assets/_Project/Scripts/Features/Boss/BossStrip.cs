using System.Collections;
using SibJam.Combat;
using UnityEngine;

namespace SibJam.Boss
{
    // Полоса арены для атаки прыжком. Зона — куб этого объекта (его масштаб задаёт размер), сам куб не рисуется:
    // видно накинутую на пол сетку _visual — сначала мигает рамкой, потом искрит и бьёт током всех, кто внутри
    public class BossStrip : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private Renderer _visual;
        [SerializeField] private ParticleSystem _sparks;
        [SerializeField] private Color _color = new Color(1f, 0.12f, 0.08f, 1f);
        [SerializeField, Range(0f, 1f)] private float _telegraphAlpha = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _activeAlpha = 0.6f;
        [SerializeField, Min(0.05f)] private float _tickInterval = 0.5f;

        [Header("Звуки")]
        [SerializeField] private SfxEmitter _sfx;
        [SerializeField] private SoundSet _warningLoop;
        [SerializeField] private SoundSet _electricLoop;

        private MaterialPropertyBlock _block;
        private Coroutine _running;

        // Точка приземления босса: на средней линии полосы напротив игрока, на уровне низа зоны
        public Vector3 LandingPoint(Vector3 playerPosition)
        {
            Vector3 local = transform.InverseTransformPoint(playerPosition);
            return transform.TransformPoint(new Vector3(Mathf.Clamp(local.x, -0.45f, 0.45f), -0.5f, 0f));
        }

        public bool IsInside(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return Mathf.Abs(local.x) <= 0.5f && Mathf.Abs(local.y) <= 0.5f && Mathf.Abs(local.z) <= 0.5f;
        }

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
            _visual.enabled = false;
        }

        public void Telegraph()
        {
            Stop();
            _running = StartCoroutine(Pulse());
        }

        // Удар при приземлении + урон каждые _tickInterval секунд, пока полоса под током. Блоком не снимается
        public void Activate(float duration, float landDamage, float tickDamage, Transform player, IPlayerHitReceiver receiver, Object source)
        {
            Stop();
            _running = StartCoroutine(Electrify(duration, landDamage, tickDamage, player, receiver, source));
        }

        public void Stop()
        {
            if (_running != null)
                StopCoroutine(_running);

            _running = null;
            _visual.enabled = false;
            _sfx.StopLoop();

            if (_sparks != null)
                _sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private IEnumerator Pulse()
        {
            _visual.enabled = true;
            _sfx.PlayLoop(_warningLoop);

            for (float time = 0f; ; time += Time.deltaTime)
            {
                SetAlpha(_telegraphAlpha * (0.55f + 0.45f * Mathf.Sin(time * 14f)));
                yield return null;
            }
        }

        private IEnumerator Electrify(float duration, float landDamage, float tickDamage, Transform player, IPlayerHitReceiver receiver, Object source)
        {
            _visual.enabled = true;
            _sfx.PlayLoop(_electricLoop);

            if (_sparks != null)
                _sparks.Play();

            if (player != null && receiver != null && IsInside(player.position + Vector3.up))
                receiver.ReceiveHit(new PlayerHit(landDamage, source: source));

            float nextTick = _tickInterval;

            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                SetAlpha(_activeAlpha * Random.Range(0.6f, 1f));

                if (time >= nextTick)
                {
                    nextTick += _tickInterval;

                    if (player != null && receiver != null && IsInside(player.position + Vector3.up))
                        receiver.ReceiveHit(new PlayerHit(tickDamage, source: source));
                }

                yield return null;
            }

            _visual.enabled = false;
            _running = null;
            _sfx.StopLoop();

            if (_sparks != null)
                _sparks.Stop();
        }

        private void SetAlpha(float alpha)
        {
            Color color = _color;
            color.a = alpha;
            _block.SetColor(BaseColorId, color);
            _visual.SetPropertyBlock(_block);
        }
    }
}
