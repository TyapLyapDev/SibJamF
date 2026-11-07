using SibJam.SecurityCameras;
using SibJam.Boss;
using SibJam.Checkpoints;
using SibJam.Credits;
using SibJam.Cutscenes;
using SibJam.Feedback;
using SibJam.Defeat;
using SibJam.Player;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameExitDialog _gameExitDialog;
        [SerializeField] private PopUp _gameButtons;
        [SerializeField] private Settings _settings;
        [SerializeField] private Anticlicker _anticlicker;
        [Tooltip("На главной камере: тряска для отдачи игрока")]
        [SerializeField] private CameraShaker _cameraShaker;
        [Tooltip("Плашка на сервисном канвасе субтитров")]
        [SerializeField, FormerlySerializedAs("_subtitles")] private SubtitlesView _subtitlesView;
        [Tooltip("Голос героя на сервисном объекте реплик")]
        [SerializeField, FormerlySerializedAs("_voiceLinePlayer")] private VoiceSource _voiceSource;
        [Tooltip("Подсказка пропуска катсцен на сервисном канвасе")]
        [SerializeField] private SkipHintView _skipHintView;
        [Tooltip("Финальные титры на сервисном канвасе (пусто — сцена без них)")]
        [SerializeField] private CreditsView _creditsView;

        [Header("Чекпоинты и поражение (пусто — сцена без них)")]
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private Cutscene _wakeUpCutscene;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private DefeatScreen _defeatScreen;

        [Header("Бой с боссом (пусто — сцена без него)")]
        [SerializeField] private BossFight _bossFight;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameButtonsShower>(Lifetime.Singleton).AsSelf();
            builder.Register<IPlayerInputReader, PlayerInputReader>(Lifetime.Singleton);
            builder.Register<ImpactFx>(Lifetime.Singleton);
            builder.RegisterBuildCallback(resolver =>
            {
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    foreach (var camera in root.GetComponentsInChildren<SecurityCamera>(true))
                        resolver.Inject(camera);
                }
            });
            builder.RegisterComponent(_gameExitDialog);
            builder.RegisterComponent(_gameButtons);
            builder.RegisterComponent(_settings);
            builder.RegisterComponent(_cameraShaker);
            builder.RegisterComponent(_anticlicker);
            builder.RegisterComponent(_subtitlesView);
            builder.Register<Subtitles>(Lifetime.Singleton);
            builder.RegisterComponent(_voiceSource);
            builder.Register<VoiceLinePlayer>(Lifetime.Singleton);
            builder.RegisterComponent(_skipHintView);
            builder.Register<SkipHint>(Lifetime.Singleton);

            // Остальные катсцены сцены (реплики теликов по триггерам и т.п.) тоже получают сервисы реплик и субтитров
            builder.RegisterBuildCallback(container =>
            {
                foreach (Cutscene cutscene in FindObjectsByType<Cutscene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if (cutscene != _wakeUpCutscene)
                        container.Inject(cutscene);
                }

                // Телики говорят через тот же сервис реплик
                foreach (TV.TVRoom room in FindObjectsByType<TV.TVRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                    container.Inject(room);

                // Зоны обучения читают ввод игрока
                foreach (Tutorial.TutorialZone zone in FindObjectsByType<Tutorial.TutorialZone>(FindObjectsInactive.Include))
                    container.Inject(zone);
            });

            if (_creditsView != null)
            {
                builder.RegisterComponent(_creditsView);
                builder.Register<EndCredits>(Lifetime.Singleton);
            }

            if (_playerHealth != null)
                builder.RegisterComponent(_playerHealth).As<IPlayerHealthInfo>().AsSelf();

            if (_gameLoop != null && _wakeUpCutscene != null && _playerHealth != null)
            {
                builder.RegisterComponent(_gameLoop);
                builder.RegisterComponent(_wakeUpCutscene);
                builder.RegisterEntryPoint<CheckpointFlow>(Lifetime.Singleton);
            }

            if (_defeatScreen != null)
                builder.RegisterComponent(_defeatScreen);

            if (_bossFight != null)
            {
                builder.RegisterComponent(_bossFight);
                builder.RegisterEntryPoint<VictoryFlow>(Lifetime.Singleton);
            }
        }
    }
}