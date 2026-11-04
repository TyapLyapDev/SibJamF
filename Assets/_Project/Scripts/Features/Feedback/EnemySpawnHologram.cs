using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace SibJam.Feedback
{
    [DisallowMultipleComponent]
    public sealed class EnemySpawnHologram : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float _duration = 1.4f;
        [SerializeField, ColorUsage(false, true)] private Color _hologramColor = new Color(0.05f, 2f, 3f, 1f);
        [SerializeField, Min(1f)] private float _scanDensity = 35f;
        [SerializeField, Range(0f, 0.2f)] private float _glitchStrength = 0.035f;

        private static readonly int ProgressId = Shader.PropertyToID("_Progress");
        private static readonly int BoundsId = Shader.PropertyToID("_BoundsY");
        private readonly List<Surface> _surfaces = new List<Surface>();
        private float _elapsed;
        private float _bottomOffset;
        private float _height;

        private sealed class Surface
        {
            public Renderer Renderer;
            public Material[] Originals;
            public Material[] Temporary;
            public ShadowCastingMode Shadows;
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
                    Originals = renderer.sharedMaterials,
                    Shadows = renderer.shadowCastingMode
                };
                surface.Temporary = new Material[surface.Originals.Length];
                for (int i = 0; i < surface.Temporary.Length; i++)
                {
                    Material source = surface.Originals[i];
                    if (source == null) continue;
                    var material = new Material(shader) { name = source.name + " (Spawn Hologram)" };
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
                    if (source.HasProperty("_Metallic")) material.SetFloat("_Metallic", source.GetFloat("_Metallic"));
                    material.SetColor("_HologramColor", _hologramColor);
                    material.SetFloat("_ScanDensity", _scanDensity);
                    material.SetFloat("_GlitchStrength", _glitchStrength);
                    surface.Temporary[i] = material;
                }
                _surfaces.Add(surface);
                renderer.sharedMaterials = surface.Temporary;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
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
                    surface.Renderer.shadowCastingMode = surface.Shadows;
                }
                foreach (Material material in surface.Temporary)
                    if (material != null) Destroy(material);
            }
            _surfaces.Clear();
        }
    }
}
