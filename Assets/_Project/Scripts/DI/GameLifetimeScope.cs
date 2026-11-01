using SibJam.Boss;
using SibJam.Checkpoints;
using SibJam.Cutscenes;
using SibJam.Defeat;
using SibJam.Player;
using UnityEngine;
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
        [SerializeField] private Subtitles _subtitles;

        [Header("Чекпоинты и поражение (пусто — сцена без них)")]
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private WakeUpCutscene _wakeUpCutscene;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private DefeatScreen _defeatScreen;

        [Header("Бой с боссом (пусто — сцена без него)")]
        [SerializeField] private BossFight _bossFight;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameButtonsShower>(Lifetime.Singleton).AsSelf();
            builder.Register<IPlayerInputReader, PlayerInputReader>(Lifetime.Singleton);
            builder.RegisterComponent(_gameExitDialog);
            builder.RegisterComponent(_gameButtons);
            builder.RegisterComponent(_settings);
            builder.RegisterComponent(_anticlicker);
            builder.RegisterComponent(_subtitles);

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