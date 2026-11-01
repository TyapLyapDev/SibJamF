using System.Collections;
using _Project.Scripts.Enemies;
using UnityEngine;

namespace SibJam.Feedback
{
    // Вспышка белым и «сплющивание» врага при получении урона
    [RequireComponent(typeof(Enemy))]
    public class EnemyHitFlash : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private Color _flashColor = Color.white;
        [SerializeField, Min(0f)] private float _flashDuration = 0.1f;
        [SerializeField, Min(0f)] private float _punchScale = 0.15f;
        [SerializeField, Min(0.01f)] private float _punchDuration = 0.2f;
        [Tooltip("Искры/брызги в точке врага при попадании, необязательно")]
        [SerializeField] private ParticleSystem _hitParticlesPrefab;

        private Enemy _enemy;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _block;
        private Vector3 _restScale;
        private Coroutine _routine;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _renderers = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
            _restScale = transform.localScale;
        }

        private void OnEnable() => _enemy.Damaged += OnDamaged;
        private void OnDisable() => _enemy.Damaged -= OnDamaged;

        private void OnDamaged(float amount)
        {
            if (_hitParticlesPrefab != null)
            {
                ParticleSystem particles = Instantiate(_hitParticlesPrefab, transform.position + Vector3.up, Quaternion.identity);
                Destroy(particles.gameObject, particles.main.duration + particles.main.startLifetime.constantMax);
            }

            if (_routine != null)
                StopCoroutine(_routine);

            _routine = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            SetFlash(true);

            float duration = Mathf.Max(_flashDuration, _punchDuration);

            for (float time = 0f; time < duration; time += Time.unscaledDeltaTime)
            {
                if (time >= _flashDuration)
                    SetFlash(false);

                // Сплющиваем по вертикали и раздуваем вширь, затухая
                float punch = _punchScale * (1f - time / _punchDuration);
                punch = Mathf.Max(0f, punch);
                transform.localScale = Vector3.Scale(_restScale, new Vector3(1f + punch, 1f - punch, 1f + punch));
                yield return null;
            }

            SetFlash(false);
            transform.localScale = _restScale;
            _routine = null;
        }

        private void SetFlash(bool isOn)
        {
            foreach (Renderer target in _renderers)
            {
                if (target == null)
                    continue;

                if (!isOn)
                {
                    target.SetPropertyBlock(null);
                    continue;
                }

                target.GetPropertyBlock(_block);
                _block.SetColor(BaseColorId, _flashColor);
                _block.SetColor(ColorId, _flashColor);
                _block.SetColor(EmissionId, _flashColor * 2f);
                target.SetPropertyBlock(_block);
            }
        }
    }
}
