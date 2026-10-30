using UnityEngine;

namespace SibJam
{
    public class MenuButtons : MonoBehaviour
    {
        public void Show() =>
            gameObject.SetActive(true);

        public void Hide() =>
            gameObject.SetActive(false);
    }
}