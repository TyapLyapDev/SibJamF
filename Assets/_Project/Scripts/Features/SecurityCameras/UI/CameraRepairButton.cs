using SibJam.Weapons;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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
        private bool _ownsHousingMaterial;
        private bool _ownsButtonMaterial;
        private Coroutine _pressRoutine;
        private Vector3 _capRestPosition;
        private Vector3 _capRestScale;
        private bool _hasRestPose;

        public static CameraRepairButton Create(SecurityCamera camera)
        {
            foreach (var existing in FindObjectsByType<CameraRepairButton>(FindObjectsInactive.Include))
                if (existing._camera == camera) return existing;

            var root = new GameObject("Camera Repair Button");
            SceneManager.MoveGameObjectToScene(root, camera.gameObject.scene);
            // Стартовая камера находится над нижним ярусом; кнопка — на высоте руки.
            root.transform.position = new Vector3(camera.transform.position.x, 0.95f,
                camera.transform.position.z);
            var button = root.AddComponent<CameraRepairButton>();
            button._camera = camera;
            button.Build();
            button.Bind();
            button.Refresh(camera);
            return button;
        }

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

        private void Build()
        {
            _housingMaterial = MakeMaterial(new Color(0.08f, 0.1f, 0.12f));
            _buttonMaterial = MakeMaterial(Color.red);
            _ownsHousingMaterial = Application.isPlaying;
            _ownsButtonMaterial = Application.isPlaying;
            _buttonMaterial.EnableKeyword("_EMISSION");
            AddPart("Stand", new Vector3(0, -0.45f, 0), new Vector3(.12f, .9f, .12f), _housingMaterial);
            AddPart("Housing", Vector3.zero, new Vector3(.65f, .45f, .22f), _housingMaterial);
            _cap = AddPart("Repair", new Vector3(0, 0, .15f), new Vector3(.3f, .25f, .1f), _buttonMaterial);

        }

        private Renderer AddPart(string name, Vector3 position, Vector3 size, Material material)
        {
            var part = GameObject.CreatePrimitive(PrimitiveType.Cube);
            part.name = name;
            part.transform.SetParent(transform, false);
            part.transform.localPosition = position;
            part.transform.localScale = size;
            var renderer = part.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Material MakeMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = color;
            return material;
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
            if (_ownsHousingMaterial && _housingMaterial != null) Destroy(_housingMaterial);
            if (_ownsButtonMaterial && _buttonMaterial != null) Destroy(_buttonMaterial);
        }
    }
}
