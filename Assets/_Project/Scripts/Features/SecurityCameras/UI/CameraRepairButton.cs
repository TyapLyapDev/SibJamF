using SibJam.Weapons;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam.SecurityCameras.UI
{
    // Физическая сервисная кнопка под тренировочной камерой; остаётся после её поломки.
    public sealed class CameraRepairButton : MonoBehaviour, IMeleeHitTarget
    {
        [SerializeField] private SecurityCamera _camera;
        [SerializeField] private Renderer _cap;
        [SerializeField] private Material _housingMaterial;
        [SerializeField] private Material _buttonMaterial;
        [SerializeField] private SoundSet _pressSound;
        private bool _ownsButtonMaterial;
        private Coroutine _pressRoutine;
        private Vector3 _capRestPosition;
        private Vector3 _capRestScale;
        private bool _hasRestPose;

        private void OnEnable() => Bind();

        private void Bind()
        {
            if (!Application.isPlaying || _camera == null || _cap == null) return;
            if (!_hasRestPose)
            {
                _capRestPosition = _cap.transform.localPosition;
                _capRestScale = _cap.transform.localScale;
                _hasRestPose = true;
            }
            // Материал сцены остаётся общим ресурсом; состояние кнопки меняется на копии.
            if (!_ownsButtonMaterial)
            {
                _buttonMaterial = new Material(_buttonMaterial);
                _cap.sharedMaterial = _buttonMaterial;
                _ownsButtonMaterial = true;
            }
            Unbind();
            _camera.Broken += Refresh;
            _camera.Repaired += Refresh;
            Refresh(_camera);
        }

        private void OnDisable()
        {
            Unbind();
            StopAllCoroutines();
            _pressRoutine = null;
            if (_hasRestPose && _cap != null)
            {
                _cap.transform.localPosition = _capRestPosition;
                _cap.transform.localScale = _capRestScale;
                if (_camera != null) Refresh(_camera);
            }
        }

        private void Unbind()
        {
            if (_camera == null) return;
            _camera.Broken -= Refresh;
            _camera.Repaired -= Refresh;
        }

        private void Update()
        {
            var view = Camera.main;
            if (view == null) return;
            if (Time.timeScale <= 0f || _camera == null || !_camera.IsBroken
                || Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame) return;
            if (Physics.Raycast(view.transform.position, view.transform.forward, out var hit, 2.5f,
                ~0, QueryTriggerInteraction.Ignore) && hit.collider.GetComponentInParent<CameraRepairButton>() == this)
                Press();
        }

        public void TakeMeleeHit(Vector3 point)
        {
            Press();
        }

        private void Press()
        {
            if (Time.timeScale <= 0f || _camera == null || !_camera.IsBroken || _cap == null || _pressRoutine != null) return;
            SfxEmitter.PlayAt(_pressSound, _cap.transform.position, _camera.SfxOutput);
            _pressRoutine = StartCoroutine(AnimatePress());
        }

        private IEnumerator AnimatePress()
        {
            _buttonMaterial.SetColor("_EmissionColor", Color.white * 3f);
            yield return MoveCap(0f, 1f, .08f);
            if (_camera != null) _camera.Repair();
            yield return MoveCap(1f, 0f, .16f);
            if (_camera != null) Refresh(_camera);
            _pressRoutine = null;
        }

        private IEnumerator MoveCap(float from, float to, float duration)
        {
            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                SetCapPress(Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
                yield return null;
            }
            SetCapPress(to);
        }

        private void SetCapPress(float amount)
        {
            _cap.transform.localPosition = _capRestPosition - Vector3.forward * (.06f * amount);
            _cap.transform.localScale = new Vector3(_capRestScale.x, _capRestScale.y,
                _capRestScale.z * (1f - .3f * amount));
        }

        private void Refresh(SecurityCamera camera)
        {
            var color = camera.IsBroken ? new Color(1f, .35f, .1f) : new Color(.15f, .8f, .4f);
            _cap.sharedMaterial.color = color;
            _cap.sharedMaterial.SetColor("_EmissionColor", color * 1.5f);
        }

        private void OnDestroy()
        {
            if (_camera != null)
            {
                _camera.Broken -= Refresh;
                _camera.Repaired -= Refresh;
            }
            if (_ownsButtonMaterial && _buttonMaterial != null) Destroy(_buttonMaterial);
        }
    }
}
