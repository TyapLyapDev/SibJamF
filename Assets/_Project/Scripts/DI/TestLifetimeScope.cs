using SibJam.SecurityCameras;
using SibJam.Feedback;
using UnityEngine;
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

            // Камеры наблюдения трясут главную камеру; в тестовых сценах шейкера может не быть
            Camera mainCamera = Camera.main;
            if (!mainCamera.TryGetComponent(out CameraShaker shaker))
                shaker = mainCamera.gameObject.AddComponent<CameraShaker>();
            builder.RegisterComponent(shaker);

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
