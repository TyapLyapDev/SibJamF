using UnityEngine;

namespace SibJam
{
    public class GameExitDialog : MonoBehaviour
    {
        public void Show() =>
            gameObject.SetActive(true);

        public void Hide() =>
            gameObject.SetActive(false);
    }
}