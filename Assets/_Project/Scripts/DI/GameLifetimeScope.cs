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

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameButtonsShower>(Lifetime.Singleton).AsSelf();
            builder.Register<IPlayerInputReader, PlayerInputReader>(Lifetime.Singleton);
            builder.RegisterComponent(_gameExitDialog);
            builder.RegisterComponent(_gameButtons);
            builder.RegisterComponent(_settings);
        }
    }
}