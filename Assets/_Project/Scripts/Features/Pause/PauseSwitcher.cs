using UnityEngine;

namespace SibJam
{
    public class PauseSwitcher : IPauseSwitcher
    {
        public void Pause()
        {
            Time.timeScale = 0f;
        }

        public void Unpause()
        {
            Time.timeScale = 1f;
        }
    }
}