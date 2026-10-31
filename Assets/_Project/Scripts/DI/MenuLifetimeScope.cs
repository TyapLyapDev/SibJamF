using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class MenuLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameExitDialog _gameExitDialog;
        [SerializeField] private PopUp _menuButtons;
        [SerializeField] private Settings _settings; 

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<MenuEntryPoint>(Lifetime.Singleton);
            builder.RegisterComponent(_gameExitDialog);
            builder.RegisterComponent(_menuButtons);
            builder.RegisterComponent(_settings);
        }
    }
}
