using UnityEngine;

namespace SibJam.Cutscenes
{
    // Камера внутри головы рига: прячет голову из кадра и приближает near clip, чтобы не резало то, что у лица.
    // Включается и выключается явно — «Действиями» HideHead и ShowHead на таймлайне катсцены.
    // Кости сжимаются в конце кадра, после камеры катсцены (она читает голову целой), и возвращаются в начале следующего
    [DefaultExecutionOrder(200)]
    public class HeadHider : MonoBehaviour
    {
        [Tooltip("Кости, которые не должны лезть в кадр (голова): сжимаются в ноль")]
        [SerializeField] private Transform[] _hiddenBones;
        [Tooltip("Near clip главной камеры, пока голова спрятана")]
        [SerializeField, Min(0.01f)] private float _nearClipPlane = 0.03f;

        private Camera _camera;
        private float _defaultNearClipPlane;

        public bool IsHiding { get; private set; }

        public float NearClipPlane => _nearClipPlane;

        public void HideHead()
        {
            if (IsHiding)
                return;

            IsHiding = true;
            _camera = Camera.main;

            if (_camera == null)
                return;

            _defaultNearClipPlane = _camera.nearClipPlane;
            _camera.nearClipPlane = _nearClipPlane;
        }

        public void ShowHead()
        {
            if (!IsHiding)
                return;

            IsHiding = false;
            SetBonesHidden(false);

            if (_camera != null)
                _camera.nearClipPlane = _defaultNearClipPlane;
        }

        private void OnDisable() => ShowHead();

        private void Update()
        {
            if (IsHiding)
                SetBonesHidden(false);
        }

        private void LateUpdate()
        {
            if (IsHiding)
                SetBonesHidden(true);
        }

        public void SetBonesHidden(bool hidden)
        {
            foreach (Transform bone in _hiddenBones)
            {
                if (bone != null)
                    bone.localScale = hidden ? Vector3.zero : Vector3.one;
            }
        }
    }
}
