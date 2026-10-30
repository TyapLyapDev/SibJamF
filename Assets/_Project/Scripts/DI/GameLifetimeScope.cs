using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<GameEntryPoint>(Lifetime.Singleton);
        }
    }
}