using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace SibJam
{
    public class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private Music _music;
        [SerializeField] private AudioMixer _mixer;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_inputActions);
            builder.Register<IInputReader, InputReader>(Lifetime.Singleton);
            builder.Register<ICursorShower, CursorShower>(Lifetime.Singleton);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IPauseSwitcher, PauseSwitcher>(Lifetime.Singleton);
            builder.Register<ISaver, Saver>(Lifetime.Singleton);
            builder.RegisterComponent(_music);
            builder.RegisterComponent(_mixer);

            builder.RegisterBuildCallback(v =>
            {
                v.Resolve<ISaver>();
            });
        }
    }
}