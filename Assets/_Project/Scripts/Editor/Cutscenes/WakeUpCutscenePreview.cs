using System.Collections.Generic;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SibJam.EditorTools
{
    // Превью кадра пробуждения в окне катсцен: тело в позе из анимации, вид глазами героя
    [CutscenePreviewFor(typeof(WakeUpCutscene))]
    public class WakeUpCutscenePreview : CutscenePreview
    {
        private const int PreviewWidth = 640;
        private const int PreviewHeight = 360;

        private SerializedObject _serializedObject;
        private SerializedProperty _rig;
        private SerializedProperty _clip;
        private SerializedProperty _eye;
        private SerializedProperty _hiddenBones;

        private RenderTexture _previewTexture;
        private Camera _previewCamera;
        private float _time;

        private AnimationClip Clip => _clip.objectReferenceValue as AnimationClip;

        protected override void OnCreated()
        {
            _serializedObject = new SerializedObject(Target);
            _rig = _serializedObject.FindProperty("_rig");
            _clip = _serializedObject.FindProperty("_clip");
            _eye = _serializedObject.FindProperty("_eye");
            _hiddenBones = _serializedObject.FindProperty("_hiddenBones");
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_previewTexture != null)
                Object.DestroyImmediate(_previewTexture);

            if (_previewCamera != null)
                Object.DestroyImmediate(_previewCamera.gameObject);
        }

        public override Rect Draw(float width, float time)
        {
            _serializedObject.Update();

            if (Clip == null || _rig.objectReferenceValue == null || _eye.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("Для превью кадра нужны Rig, Clip и Eye на компоненте катсцены.", MessageType.Warning);
                return Rect.zero;
            }

            if (_previewTexture == null)
                OnTimeChanged(time);

            Rect rect = GUILayoutUtility.GetRect(width, width * PreviewHeight / PreviewWidth, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit);
            return rect;
        }

        public override void OnTimeChanged(float time)
        {
            _time = time;
            RenderPreview();
        }

        public override void Stop()
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        // Ставит тело в позу через AnimationMode (сцена не меняется) и рендерит кадр глазами героя
        private void RenderPreview()
        {
            var rig = (Animator)_rig.objectReferenceValue;
            var eye = (Transform)_eye.objectReferenceValue;

            if (rig == null || eye == null || Clip == null)
                return;

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(rig.gameObject, Clip, _time);
            AnimationMode.EndSampling();

            EnsurePreviewCamera();
            _previewCamera.transform.SetPositionAndRotation(eye.position + eye.rotation * _serializedObject.FindProperty("_eyeOffset").vector3Value, eye.rotation);

            var hiddenBones = new List<Transform>();

            for (int i = 0; i < _hiddenBones.arraySize; i++)
            {
                if (_hiddenBones.GetArrayElementAtIndex(i).objectReferenceValue is Transform bone)
                    hiddenBones.Add(bone);
            }

            // В игре катсцена выключает игрока, в превью просто не рисуем его
            var playerRenderers = new List<Renderer>();
            GameObject player = GameObject.FindWithTag(_serializedObject.FindProperty("_playerTag").stringValue);

            if (player != null)
                playerRenderers.AddRange(player.GetComponentsInChildren<Renderer>());

            foreach (Renderer playerRenderer in playerRenderers)
                playerRenderer.forceRenderingOff = true;

            // Скин в редакторе сам не обновляется до перерисовки — запекаем меши в позе и рендерим их
            var skinned = rig.GetComponentsInChildren<SkinnedMeshRenderer>();
            var baked = new List<GameObject>();
            var scales = new Vector3[hiddenBones.Count];

            for (int i = 0; i < hiddenBones.Count; i++)
            {
                scales[i] = hiddenBones[i].localScale;
                hiddenBones[i].localScale = Vector3.zero;
            }

            try
            {
                foreach (SkinnedMeshRenderer renderer in skinned)
                {
                    var mesh = new Mesh();
                    renderer.BakeMesh(mesh, true);
                    var bakedObject = new GameObject("CutscenePreviewBake") { hideFlags = HideFlags.HideAndDontSave };
                    bakedObject.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                    // Запечённый меш в единицах объекта: у рига из Blender масштаб может быть 100, его надо вернуть
                    bakedObject.transform.localScale = renderer.transform.lossyScale;
                    bakedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
                    var bakedRenderer = bakedObject.AddComponent<MeshRenderer>();
                    bakedRenderer.sharedMaterials = renderer.sharedMaterials;
                    bakedRenderer.renderingLayerMask = renderer.renderingLayerMask;
                    renderer.forceRenderingOff = true;
                    baked.Add(bakedObject);
                }

                _previewCamera.Render();
            }
            finally
            {
                foreach (GameObject bakedObject in baked)
                {
                    Object.DestroyImmediate(bakedObject.GetComponent<MeshFilter>().sharedMesh);
                    Object.DestroyImmediate(bakedObject);
                }

                foreach (SkinnedMeshRenderer renderer in skinned)
                    renderer.forceRenderingOff = false;

                foreach (Renderer playerRenderer in playerRenderers)
                    playerRenderer.forceRenderingOff = false;

                for (int i = 0; i < hiddenBones.Count; i++)
                    hiddenBones[i].localScale = scales[i];
            }
        }

        private void EnsurePreviewCamera()
        {
            if (_previewTexture == null)
                _previewTexture = new RenderTexture(PreviewWidth, PreviewHeight, 24) { hideFlags = HideFlags.HideAndDontSave };

            if (_previewCamera != null)
                return;

            var cameraObject = new GameObject("CutscenePreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
            _previewCamera = cameraObject.AddComponent<Camera>();
            _previewCamera.enabled = false;
            _previewCamera.fieldOfView = 60f;
            _previewCamera.nearClipPlane = 0.03f;
            _previewCamera.targetTexture = _previewTexture;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        }
    }
}
