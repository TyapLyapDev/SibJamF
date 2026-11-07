using System.Collections.Generic;
using SibJam.Waves;
using SibJam.Combat;
using UnityEngine;

namespace _Project.Scripts.Enemies
{
    // Куклы сцены — повторяющаяся тренировочная волна без роста числа врагов и урона.
    public sealed class TrainingDummyWaveSpawner : WaveSpawner
    {
        private const float WaveDelay = 3f;

        [Tooltip("Манекены-шаблоны: в бою участвуют их копии")]
        [SerializeField] private TrainingDummy[] _templates = new TrainingDummy[0];

        private WavesStateMachine _stateMachine;
        private WaveConfig _waveConfig;
        private bool _running;

        public IWavesLoopInfo Info => _stateMachine;
        public void SetRunning(bool running) => _running = running;

        private void Awake()
        {
            // Сохраняем исходные объекты до боя: их копии можно заново создавать после смерти.
            foreach (TrainingDummy template in _templates)
                template.gameObject.SetActive(false);
        }

        private void Start()
        {
            _waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
            _stateMachine = new WavesStateMachine(
                new WaveSequence(new List<WaveConfig> { _waveConfig }),
                new DelaySequence(new List<float> { WaveDelay }),
                this, repeatLastWave: true);
        }

        public override Wave Spawn(WaveConfig config, WaveDifficulty difficulty)
        {
            var wave = new Wave();
            foreach (TrainingDummy template in _templates)
            {
                if (template == null) continue;

                var dummy = Instantiate(template, template.transform.position,
                    template.transform.rotation, template.transform.parent);
                dummy.name = template.name;
                dummy.gameObject.SetActive(true);
                dummy.Init(difficulty.StatsMultiplier, Target);
                wave.BindEnemy(dummy.GetComponent<Enemy>());
            }
            return wave;
        }

        private void Update()
        {
            if (!_running || _stateMachine == null) return;

            if (_stateMachine.State == WavesLoopState.Idle)
                _stateMachine.Launch();
            else
                _stateMachine.Tick(Time.deltaTime);
        }

        private void OnDestroy()
        {
            if (_waveConfig != null)
                Destroy(_waveConfig);
        }
    }
}
