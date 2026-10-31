using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class BootstrapLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<Bootstrap>(Lifetime.Singleton);
        }
    }
}
