using SibJam.Checkpoints;
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
        [SerializeField] private UiSfxService _uiSfxService;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(_inputActions);
            builder.Register<IInputReader, InputReader>(Lifetime.Singleton);
            builder.Register<ICursorShower, CursorShower>(Lifetime.Singleton);
            builder.Register<ISceneLoader, SceneLoader>(Lifetime.Singleton);
            builder.Register<IPauseSwitcher, PauseSwitcher>(Lifetime.Singleton);
            builder.Register<ISaver, Saver>(Lifetime.Singleton);
            builder.Register<ICheckpoint, Checkpoint>(Lifetime.Singleton);
            builder.RegisterComponent(_music);
            builder.RegisterComponent(_mixer);
            builder.RegisterComponent(_uiSfxService);

            builder.RegisterBuildCallback(v =>
            {
                v.Resolve<ISaver>();
            });
        }
    }
}