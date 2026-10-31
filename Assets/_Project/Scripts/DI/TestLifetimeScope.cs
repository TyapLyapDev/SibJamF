using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class TestLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<IPlayerInputReader, PlayerInputReader>(Lifetime.Singleton);
        }
    }
}
