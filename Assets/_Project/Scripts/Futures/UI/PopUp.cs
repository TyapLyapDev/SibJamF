using UnityEngine;

namespace SibJam
{
    public abstract class PopUp : MonoBehaviour
    {
        public void Show() =>
            gameObject.SetActive(true);

        public void Hide() =>
            gameObject.SetActive(false);
    }
}