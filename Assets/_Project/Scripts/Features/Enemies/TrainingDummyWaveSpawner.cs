using System.Collections.Generic;
using System.Linq;
using SibJam.Waves;
using SibJam.Waves.UI;
using SibJam.Combat;
using SibJam.SecurityCameras;
using SibJam.SecurityCameras.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Project.Scripts.Enemies
{
    // Куклы сцены — повторяющаяся тренировочная волна без роста числа врагов и урона.
    public sealed class TrainingDummyWaveSpawner : WaveSpawner, ICameraDestructionGoalInfo
    {
        private const float WaveDelay = 3f;

        private readonly List<TrainingDummy> _templates = new();
        private WavesStateMachine _stateMachine;
        private WaveConfig _waveConfig;
        private PlayerTarget _player;
        private float _entranceZ = float.NegativeInfinity;
        private readonly List<SecurityCamera> _cameras = new();
        public bool HasEntered { get; private set; }

        public event System.Action Changed;
        public int TotalCount => _cameras.Count;
        public int BrokenCount { get; private set; }
        public int LeftCount => TotalCount - BrokenCount;
        public bool IsCompleted => LeftCount == 0;
        public bool IsPlayerNearby => _player != null && IsNearby(_player.Position, 20f);
        public bool IsPlayerOnLowerFloor => _player != null && _player.isActiveAndEnabled
            && _templates.Any(template => template != null
                && _player.Position.y >= template.transform.position.y - 1.5f
                && _player.Position.y < template.transform.position.y + 2.5f);
        public bool IsPlayerInside => HasEntered && IsPlayerNearby && IsPlayerOnLowerFloor
            && _player.Position.z >= _entranceZ;

        public void EnterTraining() => HasEntered = true;

        public void SetEntrance(float z) => _entranceZ = z;

        public IWavesLoopInfo Info => _stateMachine;
        public int WaveNumber { get; private set; }

        public bool IsNearby(Vector3 position, float radius) =>
            _templates.Any(template => template != null
                && new Vector2(template.transform.position.x - position.x,
                    template.transform.position.z - position.z).sqrMagnitude <= radius * radius);

        public static void CreateForScene(TrainingDummy source)
        {
            var templates = FindObjectsByType<TrainingDummy>(FindObjectsSortMode.InstanceID)
                .Where(dummy => dummy.gameObject.scene == source.gameObject.scene
                    && dummy.isActiveAndEnabled && !dummy.IsInitialized && dummy.Config != null)
                .ToArray();
            if (templates.Length == 0) return;

            var root = new GameObject("TrainingDummyWaves");
            SceneManager.MoveGameObjectToScene(root, source.gameObject.scene);
            var spawner = root.AddComponent<TrainingDummyWaveSpawner>();
            spawner.Initialize(templates);
            root.AddComponent<TrainingDummyWavesView>().Bind(spawner);
        }

        private void Initialize(TrainingDummy[] templates)
        {
            _player = FindAnyObjectByType<PlayerTarget>(FindObjectsInactive.Include);
            // Сохраняем исходные объекты до боя: их копии можно заново создавать после смерти.
            foreach (TrainingDummy template in templates)
            {
                _templates.Add(template);
                template.gameObject.SetActive(false);
            }

            foreach (var camera in FindObjectsByType<SecurityCamera>())
            {
                // Камера стартовой комнаты. Камера у дальнего прохода относится к другой зоне.
                if (camera.gameObject.scene != gameObject.scene
                    || !camera.GetComponentsInParent<Transform>().Any(parent => parent.name == "SM_CameraDesk (4)")) continue;
                _cameras.Add(camera);
                if (camera.IsBroken) BrokenCount++;
                else camera.Broken += OnCameraBroken;
                camera.Repaired += OnCameraRepaired;
                CameraRepairButton.Create(camera);
            }

            _waveConfig = ScriptableObject.CreateInstance<WaveConfig>();
            _stateMachine = new WavesStateMachine(
                new WaveSequence(new List<WaveConfig> { _waveConfig }),
                new DelaySequence(new List<float> { WaveDelay }),
                this, repeatLastWave: true);
        }

        public override Wave Spawn(WaveConfig config, WaveDifficulty difficulty)
        {
            WaveNumber++;
            var wave = new Wave();
            foreach (TrainingDummy template in _templates)
            {
                if (template == null) continue;

                var dummy = Instantiate(template, template.transform.position,
                    template.transform.rotation, template.transform.parent);
                dummy.name = template.name;
                dummy.gameObject.SetActive(true);
                dummy.Init(difficulty.StatsMultiplier);
                wave.BindEnemy(dummy.GetComponent<Enemy>());
            }
            return wave;
        }

        private void Update()
        {
            if (_stateMachine == null) return;

            if (_player == null)
                _player = FindObjectsByType<PlayerTarget>(FindObjectsSortMode.None)
                    .FirstOrDefault(player => player.gameObject.scene == gameObject.scene);

            // Первый запуск — только после пересечения триггера за первой дверью.
            if (!IsPlayerInside) return;

            if (_stateMachine.State == WavesLoopState.Idle)
                _stateMachine.Launch();
            else
                _stateMachine.Tick(Time.deltaTime);
        }

        private void OnCameraBroken(SecurityCamera camera)
        {
            camera.Broken -= OnCameraBroken;
            BrokenCount++;
            Changed?.Invoke();
        }

        private void OnCameraRepaired(SecurityCamera camera)
        {
            BrokenCount = Mathf.Max(0, BrokenCount - 1);
            camera.Broken += OnCameraBroken;
            Changed?.Invoke();
        }

        private void OnDestroy()
        {
            foreach (var camera in _cameras)
                if (camera != null)
                {
                    camera.Broken -= OnCameraBroken;
                    camera.Repaired -= OnCameraRepaired;
                }
            if (_waveConfig != null)
                Destroy(_waveConfig);
        }
    }
}
