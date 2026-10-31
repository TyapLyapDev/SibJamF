using UnityEngine.SceneManagement;

namespace SibJam
{
    public class SceneLoader : ISceneLoader
    {
        public void Load(string sceneName) =>
            SceneManager.LoadScene(sceneName);
    }
}