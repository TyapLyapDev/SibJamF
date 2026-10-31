using System;
using VContainer.Unity;

namespace SibJam
{
    public class Bootstrap : IStartable
    {
        private readonly ISceneLoader _sceneLoader;

        public Bootstrap(ISceneLoader sceneLoader)
        {
            _sceneLoader = sceneLoader ?? throw new ArgumentNullException(nameof(sceneLoader));
        }

        public void Start()
        {
            _sceneLoader.Load(SceneNames.Menu);
        }
    }
}