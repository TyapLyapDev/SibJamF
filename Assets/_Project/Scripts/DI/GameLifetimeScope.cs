using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class GameLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameExitDialog _gameExitDialog;
        [SerializeField] private PopUp _gameButtons;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameButtonsShower>(Lifetime.Singleton);
            builder.Register<IPlayerInputReader, PlayerInputReader>(Lifetime.Singleton);
            builder.RegisterComponent(_gameExitDialog);
            builder.RegisterComponent(_gameButtons);
        }
    }
}