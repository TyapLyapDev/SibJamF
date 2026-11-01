using SibJam.Feedback;
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

            float deltaTime = Time.unscaledDeltaTime;
            transform.SetPositionAndRotation(_target.position + CameraShake.SampleOffset(deltaTime), _target.rotation * CameraShake.Sample(deltaTime));
        }
    }
}