using UnityEngine;

namespace SibJam
{
    // Камера на голове игрока. Тряску поверх накладывает CameraShaker на той же камере.
    // Катсцена забирает камеру через Release и отдаёт через TakeOver — вызовами на таймлайне
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform _target;
        [Tooltip("За сколько секунд камера доезжает до игрока после TakeOver")]
        [SerializeField, Min(0f)] private float _blendDuration = 0.4f;

        private Pose _blendFrom;
        private float _blendTime = -1f;

        // Отдать камеру катсцене: перестаём ставить её к игроку
        public void Release()
        {
            enabled = false;
        }

        // Забрать камеру обратно и плавно подвести к игроку оттуда, где она сейчас
        public void TakeOver()
        {
            _blendFrom = new Pose(transform.position, transform.rotation);
            _blendTime = 0f;
            enabled = true;
        }

        private void LateUpdate()
        {
            if (_target == null)
                return;

            Vector3 position = _target.position;
            Quaternion rotation = _target.rotation;

            if (_blendTime >= 0f)
            {
                _blendTime += Time.deltaTime;
                float t = _blendDuration > 0f ? Mathf.SmoothStep(0f, 1f, _blendTime / _blendDuration) : 1f;
                position = Vector3.Lerp(_blendFrom.position, position, t);
                rotation = Quaternion.Slerp(_blendFrom.rotation, rotation, t);

                if (t >= 1f)
                    _blendTime = -1f;
            }

            transform.SetPositionAndRotation(position, rotation);
        }
    }
}
