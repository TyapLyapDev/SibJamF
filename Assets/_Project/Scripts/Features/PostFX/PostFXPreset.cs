using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SibJam.PostFX
{
    // Переключает пресет картинки и даёт крутить основные эффекты Lab-профиля прямо с этого компонента
    [ExecuteAlways]
    public class PostFXPreset : MonoBehaviour
    {
        public enum Preset
        {
            Off,
            Lab
        }

        [SerializeField] private Preset _preset = Preset.Lab;
        [SerializeField] private Volume _labVolume;

        [Header("Эффекты (пишутся в профиль Lab)")]
        [SerializeField, Range(0f, 1f)] private float _filmGrain = 0.12f;
        [SerializeField, Range(0f, 1f)] private float _vignette = 0.36f;
        [SerializeField, Range(0f, 1f)] private float _chromaticAberration = 0.06f;
        [SerializeField, Range(0f, 3f)] private float _bloom = 0.85f;
        [SerializeField, Range(-2f, 5f)] private float _exposure = 2.65f;
        [SerializeField, Range(-100f, 50f)] private float _saturation = -8f;
        [SerializeField, Range(-50f, 50f)] private float _contrast = 14f;

        [Header("Фоновый свет")]
        [SerializeField, Range(0f, 1f)] private float _presetAmbientIntensity = 0.7f;
        [SerializeField] private float _originalAmbientIntensity = 1f;

        [Header("Дымка")]
        [SerializeField] private Color _fogColor = new Color(0.07f, 0.065f, 0.06f);
        [SerializeField, Range(0f, 0.1f)] private float _fogDensity = 0.018f;

        private void OnEnable() => Apply();

        private void OnValidate() => Apply();

        public void SetPreset(Preset preset)
        {
            _preset = preset;
            Apply();
        }

        private void Apply()
        {
            bool isOn = _preset != Preset.Off;

            if (_labVolume != null)
            {
                _labVolume.enabled = _preset == Preset.Lab;
                ApplyEffects(_labVolume.sharedProfile);
            }

            RenderSettings.ambientIntensity = isOn ? _presetAmbientIntensity : _originalAmbientIntensity;

            RenderSettings.fog = isOn;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = _fogColor;
            RenderSettings.fogDensity = _fogDensity;
        }

        private void ApplyEffects(VolumeProfile profile)
        {
            if (profile == null)
                return;

            if (profile.TryGet(out FilmGrain grain))
                grain.intensity.Override(_filmGrain);

            if (profile.TryGet(out Vignette vignette))
                vignette.intensity.Override(_vignette);

            if (profile.TryGet(out ChromaticAberration aberration))
                aberration.intensity.Override(_chromaticAberration);

            if (profile.TryGet(out Bloom bloom))
                bloom.intensity.Override(_bloom);

            if (profile.TryGet(out ColorAdjustments color))
            {
                color.postExposure.Override(_exposure);
                color.saturation.Override(_saturation);
                color.contrast.Override(_contrast);
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
                UnityEditor.EditorUtility.SetDirty(profile);
#endif
        }
    }
}
