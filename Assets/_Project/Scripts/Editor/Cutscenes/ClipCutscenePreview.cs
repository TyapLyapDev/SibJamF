using System.Collections.Generic;
using SibJam.Cutscenes;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace SibJam.EditorTools
{
    // Превью кадра по элементам клипа: анимации ставят позы, «Крепление камеры» даёт вид, «Кривая» на CanvasGroup.alpha — чёрный экран поверх.
    // HeadHider на риге прячет голову и задаёт near clip, как в игре
    // Работает для любой катсцены, у которой нет своего превью
    public class ClipCutscenePreview : CutscenePreview
    {
        private const int PreviewWidth = 640;
        private const int PreviewHeight = 360;
        private const string PlayerTag = "Player";

        private RenderTexture _previewTexture;
        private Camera _previewCamera;
        private float _time;
        private float _fade;

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
            if (Target.Clip == null || Latest<CameraAnchorAction>(time) == null)
            {
                EditorGUILayout.HelpBox("Для кадра нужен элемент «Крепление камеры» на этой секунде или раньше.", MessageType.Info);
                return Rect.zero;
            }

            if (_previewTexture == null || !Mathf.Approximately(_time, time))
                OnTimeChanged(time);

            Rect rect = GUILayoutUtility.GetRect(width, width * PreviewHeight / PreviewWidth, GUILayout.ExpandWidth(true));
            GUI.DrawTexture(rect, _previewTexture, ScaleMode.ScaleToFit);

            if (_fade > 0f && Event.current.type == EventType.Repaint)
                EditorGUI.DrawRect(rect, new Color(0f, 0f, 0f, _fade));

            return rect;
        }

        public override void OnTimeChanged(float time)
        {
            _time = time;
            _fade = 0f;
            float latestStart = float.MinValue;

            // Сцену в превью не меняем: затемнение экрана рисуем поверх кадра
            foreach (CutsceneAction action in Target.Clip.Actions)
            {
                if (action is CurveAction curve && action.Time <= time && action.Time >= latestStart
                    && curve.Property.Property == "alpha" && curve.Property.ResolveComponent(Target) is CanvasGroup)
                {
                    latestStart = action.Time;
                    _fade = Mathf.Clamp01(curve.ValueAt(time - action.Time));
                }
            }
            RenderPreview();
        }

        public override void Stop()
        {
            if (AnimationMode.InAnimationMode())
                AnimationMode.StopAnimationMode();
        }

        // Последний начавшийся к этой секунде элемент типа: он и определяет кадр
        private T Latest<T>(float time) where T : CutsceneAction
        {
            T latest = null;

            foreach (CutsceneAction action in Target.Clip.Actions)
            {
                if (action is T typed && action.Time <= time && (latest == null || action.Time >= latest.Time))
                    latest = typed;
            }

            return latest;
        }

        // Ставит позы через AnimationMode (сцена не меняется) и рендерит кадр с камеры элемента
        private void RenderPreview()
        {
            CameraAnchorAction cameraAction = Latest<CameraAnchorAction>(_time);

            if (cameraAction == null)
                return;

            if (!AnimationMode.InAnimationMode())
                AnimationMode.StartAnimationMode();

            var rigs = new List<Animator>();
            AnimationMode.BeginSampling();

            foreach (CutsceneAction action in Target.Clip.Actions)
            {
                if (action is not AnimationAction animation || animation.Clip == null || action.Time > _time)
                    continue;

                Animator rig = animation.ResolveRig(Target);

                if (rig == null)
                    continue;

                AnimationMode.SampleAnimationClip(rig.gameObject, animation.Clip, animation.ClipTime(_time - action.Time));
                rigs.Add(rig);
            }

            AnimationMode.EndSampling();

            EnsurePreviewCamera();

            if (!cameraAction.TryGetPose(Target, out Pose pose))
                return;

            _previewCamera.transform.SetPositionAndRotation(pose.position, pose.rotation);
            HeadHider headHider = null;

            foreach (Animator rig in rigs)
            {
                if (headHider == null)
                    headHider = rig.GetComponentInChildren<HeadHider>(true);
            }

            _previewCamera.nearClipPlane = headHider != null ? headHider.NearClipPlane : 0.3f;

            // В игре игрок на время катсцены прячется, в превью просто не рисуем его
            var playerRenderers = new List<Renderer>();
            GameObject player = GameObject.FindWithTag(PlayerTag);

            if (player != null)
                playerRenderers.AddRange(player.GetComponentsInChildren<Renderer>());

            foreach (Renderer playerRenderer in playerRenderers)
                playerRenderer.forceRenderingOff = true;

            // Скин в редакторе сам не обновляется до перерисовки — запекаем меши в позе и рендерим их
            var skinned = new List<SkinnedMeshRenderer>();

            foreach (Animator rig in rigs)
                skinned.AddRange(rig.GetComponentsInChildren<SkinnedMeshRenderer>());

            var baked = new List<GameObject>();

            if (headHider != null)
                headHider.SetBonesHidden(true);

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

                if (headHider != null)
                    headHider.SetBonesHidden(false);
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
            _previewCamera.targetTexture = _previewTexture;
            cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = true;
        }
    }
}
