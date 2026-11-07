using System.Collections.Generic;
using UnityEngine;

namespace SibJam.Feedback
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawnHologram : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float _duration = 2.2f;
        [SerializeField, ColorUsage(false, true)] private Color _hologramColor = new Color(3f, 0.05f, 0.05f, 1f);
        [SerializeField, Range(2f, 16f)] private float _coarseGridScale = 6f;
        [SerializeField, Range(8f, 64f)] private float _fineGridScale = 24f;
        [SerializeField, Range(4f, 32f)] private float _fragmentScale = 14f;
        [SerializeField, Range(0f, 5f)] private float _gridIntensity = 1.5f;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int BoundsId = Shader.PropertyToID("_BoundsY");
        private static readonly int OriginId = Shader.PropertyToID("_SpawnOrigin");
        private readonly List<Surface> _surfaces = new List<Surface>();
        private float _elapsed;
        private float _bottomOffset;
        private float _height;

        private sealed class Surface
        {
            public Renderer Renderer;
            public Material[] Originals;
            public Material[] Temporary;
        }

        public void Play()
        {
            Restore();
            if (!isActiveAndEnabled) return;
            // Resources reference also preserves the shader in player builds.
            Shader shader = Resources.Load<Shader>("EnemyHologramSpawn");
            if (shader == null || !shader.isSupported) return;

            Bounds bounds = new Bounds();
            bool hasBounds = false;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !(renderer is MeshRenderer || renderer is SkinnedMeshRenderer)) continue;
                if (!hasBounds) bounds = renderer.bounds;
                else bounds.Encapsulate(renderer.bounds);
                hasBounds = true;
                var surface = new Surface
                {
                    Renderer = renderer,
                    Originals = renderer.sharedMaterials
                };
                surface.Temporary = new Material[surface.Originals.Length];
                for (int i = 0; i < surface.Temporary.Length; i++)
                {
                    Material source = surface.Originals[i];
                    if (source == null) continue;
                    var material = new Material(shader) { name = source.name + " (Spawn Construction)" };
                    // The project's atlas graph uses _Atlat rather than _BaseMap.
                    string map = source.HasProperty("_Atlat") ? "_Atlat"
                        : source.HasProperty("_BaseMap") ? "_BaseMap"
                        : source.HasProperty("_MainTex") ? "_MainTex" : null;
                    if (map != null)
                    {
                        material.SetTexture("_BaseMap", source.GetTexture(map));
                        material.SetTextureScale("_BaseMap", source.GetTextureScale(map));
                        material.SetTextureOffset("_BaseMap", source.GetTextureOffset(map));
                    }
                    if (source.HasProperty("_BaseColor")) material.SetColor("_BaseColor", source.GetColor("_BaseColor"));
                    else if (source.HasProperty("_Color")) material.SetColor("_BaseColor", source.GetColor("_Color"));
                    if (source.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", source.GetFloat("_Smoothness"));
                    // AtlasBase uses a constant matte surface (smoothness 0), not Lit's 0.5 default.
                    else if (source.HasProperty("_Atlat")) material.SetFloat("_Smoothness", 0f);
                    if (source.HasProperty("_Metallic")) material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                    material.SetColor("_HologramColor", _hologramColor);
                    material.SetFloat("_CoarseGridScale", _coarseGridScale);
                    material.SetFloat("_FineGridScale", _fineGridScale);
                    material.SetFloat("_FragmentScale", _fragmentScale);
                    material.SetFloat("_GridIntensity", _gridIntensity);
                    surface.Temporary[i] = material;
                }
                _surfaces.Add(surface);
                renderer.sharedMaterials = surface.Temporary;
            }
            _bottomOffset = bounds.min.y - transform.position.y;
            _height = Mathf.Max(bounds.size.y, 0.01f);
            _elapsed = 0f;
            ApplyProgress(0f);
        }

        private void Update()
        {
            if (_surfaces.Count == 0) return;
            _elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(_elapsed / Mathf.Max(_duration, 0.05f));
            ApplyProgress(progress);
            if (progress >= 1f) Restore();
        }

        private void ApplyProgress(float progress)
        {
            Vector4 bounds = new Vector4(transform.position.y + _bottomOffset, _height, 0f, 0f);
            foreach (Surface surface in _surfaces)
                foreach (Material material in surface.Temporary)
                {
                    if (material == null) continue;
                    material.SetFloat(ProgressId, progress);
                    material.SetVector(BoundsId, bounds);
                    material.SetVector(OriginId, transform.position);
                }
        }

        private void OnDisable() => Restore();
        private void OnDestroy() => Restore();

        private void Restore()
        {
            foreach (Surface surface in _surfaces)
            {
                if (surface.Renderer != null)
                {
                    surface.Renderer.sharedMaterials = surface.Originals;
                }
                foreach (Material material in surface.Temporary)
                    if (material != null) Destroy(material);
            }
            _surfaces.Clear();
        }
    }
}
