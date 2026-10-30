using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputActionAsset _inputActions;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_inputActions);
            builder.Register<IInputReader, InputReader>(Lifetime.Singleton);
            builder.Register<ICursorShower, CursorShower>(Lifetime.Singleton);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IPauseSwitcher, PauseSwitcher>(Lifetime.Singleton);
        }
    }
}