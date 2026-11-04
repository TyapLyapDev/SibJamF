using SibJam.SecurityCameras;
using SibJam.Feedback;
using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class TestLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
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
        }
    }
}
