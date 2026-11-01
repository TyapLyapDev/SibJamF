using _Project.Scripts.Enemies;
using UnityEngine;

namespace SibJam
{
    // Звуки врага: шаги по скорости агента, редкие голоса, замах и удар/выстрел по смене фазы атаки
    public class EnemySfx : MonoBehaviour
    {
        [SerializeField] private SfxEmitter _sfx;

        [Header("Движение")]
        [SerializeField] private SoundSet _stepSound;
        [SerializeField] private SoundSet _stepBodySound;
        [SerializeField, Min(0.05f)] private float _stepInterval = 0.45f;
        [SerializeField, Min(0f)] private float _minMoveSpeed = 0.4f;

        [Header("Голос")]
        [SerializeField] private SoundSet _idleVoice;
        [SerializeField] private Vector2 _voiceInterval = new Vector2(4f, 9f);

        [Header("Атака")]
        [SerializeField] private SoundSet _windupSound;
        [SerializeField] private SoundSet _releaseSound;
        [SerializeField] private SoundSet _releaseBodySound;

        [Header("Смерть")]
        [SerializeField] private SoundSet _deathSound;

        private Enemy _enemy;
        private EnemyMotor _motor;
        private IEnemyAttack _attack;
        private AttackPhase _lastPhase;
        private float _stepTimer;
        private float _voiceTimer;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _motor = GetComponent<EnemyMotor>();
            _attack = GetComponent<IEnemyAttack>();
            _voiceTimer = Random.Range(_voiceInterval.x, _voiceInterval.y);
        }

        private void OnEnable()
        {
            if (_enemy != null)
                _enemy.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_enemy != null)
                _enemy.Died -= OnDied;
        }

        private void Update()
        {
            if (_sfx == null || (_enemy != null && _enemy.IsDead))
                return;

            TickSteps();
            TickVoice();
            TickAttack();
        }

        private void TickSteps()
        {
            if (_motor == null || _motor.IsKnockedBack || _motor.Speed < _minMoveSpeed)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer -= Time.deltaTime;

            if (_stepTimer > 0f)
                return;

            _stepTimer = _stepInterval;
            _sfx.Play(_stepSound);
            _sfx.Play(_stepBodySound);
        }

        private void TickVoice()
        {
            _voiceTimer -= Time.deltaTime;

            if (_voiceTimer > 0f)
                return;

            _voiceTimer = Random.Range(_voiceInterval.x, _voiceInterval.y);
            _sfx.Play(_idleVoice);
        }

        private void TickAttack()
        {
            if (_attack == null)
                return;

            AttackPhase phase = _attack.Phase;

            if (phase == _lastPhase)
                return;

            if (phase == AttackPhase.Windup)
                _sfx.Play(_windupSound);
            else if (_lastPhase == AttackPhase.Windup && phase != AttackPhase.None)
            {
                _sfx.Play(_releaseSound);
                _sfx.Play(_releaseBodySound);
            }

            _lastPhase = phase;
        }

        // Выгрузка уровня тоже шлёт Died, но хп при этом не на нуле
        private void OnDied(Enemy enemy)
        {
            if (enemy.Health > 0f)
                return;

            SfxEmitter.PlayAt(_deathSound, transform.position + Vector3.up, _sfx != null ? _sfx.Output : null);
        }
    }
}
