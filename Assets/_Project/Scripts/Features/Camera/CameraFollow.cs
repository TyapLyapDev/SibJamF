using UnityEngine;

namespace SibJam
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;

        private void LateUpdate()
        {
            if (_target == null)
                return;

            transform.SetPositionAndRotation(_target.position, _target.rotation);
        }
    }
}