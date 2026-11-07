using _Project.Scripts.Enemies;
using SibJam.Cutscenes;
using SibJam.Locations;
using SibJam.SecurityCameras;
using SibJam.Tutorial;
using UnityEngine;

namespace SibJam.StartLocation
{
    // Весь порядок стартовой локации:
    // катсцена (она же открывает первую дверь) → вход за дверь → волны манекенов → камеры сломаны → выход открыт.
    // Подсказки обучения идут параллельно, по порядку массива _tutorial.
    public sealed class StartLocationFlow : MonoBehaviour
    {
        private enum Phase { WakeUp, GoToTraining, Training, Cleared }

        [Header("Пробуждение")]
        [SerializeField] private Cutscene _wakeUp;

        [Header("Обучение, по порядку")]
        [SerializeField] private TutorialZone[] _tutorial = new TutorialZone[0];

        [Header("Тренировка")]
        [SerializeField] private TrainingEntryTrigger _entryTrigger;
        [SerializeField] private TrainingZone _zone;
        [SerializeField] private TrainingDummyWaveSpawner _training;
        [SerializeField] private CameraDestructionGoal _trainingCameras;
        [SerializeField] private CurrentLocationInfo _locationInfo;

        [Header("Выход")]
        [SerializeField] private LocationDoor _exitDoor;

        [Header("Отладка")]
        [SerializeField] private Phase _phase = Phase.WakeUp;

        private bool _hudShown;

        private void OnEnable()
        {
            _wakeUp.Finished += OnWakeUpFinished;
            _entryTrigger.Entered += OnEnteredTraining;
            _zone.InsideChanged += ApplyZone;
            _trainingCameras.Completed += OnCamerasBroken;

            for (int i = 0; i + 1 < _tutorial.Length; i++)
                _tutorial[i].Completed += OnTutorialZoneCompleted;
        }

        private void OnDisable()
        {
            _wakeUp.Finished -= OnWakeUpFinished;
            _entryTrigger.Entered -= OnEnteredTraining;
            _zone.InsideChanged -= ApplyZone;
            _trainingCameras.Completed -= OnCamerasBroken;

            for (int i = 0; i + 1 < _tutorial.Length; i++)
                _tutorial[i].Completed -= OnTutorialZoneCompleted;
        }

        private void Start()
        {
            if (_tutorial.Length > 0)
                _tutorial[0].Unlock();
        }

        private void OnWakeUpFinished()
        {
            if (_phase == Phase.WakeUp)
                _phase = Phase.GoToTraining;
        }

        private void OnEnteredTraining()
        {
            if (_phase > Phase.GoToTraining)
                return;

            _phase = Phase.Training;
            ApplyZone();
        }

        // Вне зоны волны стоят на паузе, а HUD показывает обычную локацию
        private void ApplyZone()
        {
            bool active = _phase >= Phase.Training && _zone.IsPlayerInside;
            _training.SetRunning(active);

            bool showHud = active && _locationInfo.LocationIndex == 0;
            if (showHud == _hudShown)
                return;

            _hudShown = showHud;
            if (showHud)
                _locationInfo.ShowOverride(_training.Info, _trainingCameras);
            else
                _locationInfo.ClearOverride();
        }

        private void OnCamerasBroken()
        {
            _phase = Phase.Cleared;

            if (!_exitDoor.IsOpen)
                _exitDoor.Open();
        }

        private void OnTutorialZoneCompleted(TutorialZone zone)
        {
            int next = System.Array.IndexOf(_tutorial, zone) + 1;
            if (next > 0 && next < _tutorial.Length)
                _tutorial[next].Unlock();
        }
    }
}
