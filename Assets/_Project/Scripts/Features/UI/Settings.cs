using System;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using VContainer;

namespace SibJam
{
    public class Settings : PopUp
    {
        private const float MinSensitivity = 0.02f;
        private const float MaxSensitivity = 2.0f;

        [SerializeField] private Slider _generalSlider;
        [SerializeField] private Slider _musicSlider;
        [SerializeField] private Slider _sfxSlider;
        [SerializeField] private Slider _mouseSensitivitySlider;
        [SerializeField] private Button _closeButton;

        private PopUp _buttonsPopUp;
        private ISaver _saver;
        private AudioMixer _audioMixer;

        [Inject]
        public void Construct(
            ISaver saver, 
            PopUp buttonsPopUp,
            AudioMixer audioMixer)
        {
            _saver = saver ?? throw new ArgumentNullException(nameof(saver));
            _buttonsPopUp = buttonsPopUp != null ? buttonsPopUp : throw new ArgumentNullException(nameof(buttonsPopUp));
            _audioMixer = audioMixer != null ? audioMixer : throw new ArgumentNullException(nameof(audioMixer));
        }

        private void OnEnable()
        {
            SavesData data = _saver.Data;

            _generalSlider.value = data.GeneralVolume;
            _musicSlider.value = data.MusicVolume;
            _sfxSlider.value = data.SfxVolume;

            float normalized = Mathf.InverseLerp(MinSensitivity, MaxSensitivity, data.MouseSensitivity);
            float sliderValue = Mathf.Lerp(_mouseSensitivitySlider.minValue, _mouseSensitivitySlider.maxValue, normalized);
            _mouseSensitivitySlider.SetValueWithoutNotify(sliderValue);

            ApplyVolume(MixerNames.General, data.GeneralVolume);
            ApplyVolume(MixerNames.Music, data.MusicVolume);
            ApplyVolume(MixerNames.Sfx, data.SfxVolume);

            _generalSlider.onValueChanged.AddListener(OnGeneralVolumeChanged);
            _musicSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            _sfxSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
            _mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);

            _closeButton.onClick.AddListener(OnClickClosePopUp);
        }

        private void OnDisable()
        {
            _generalSlider.onValueChanged.RemoveListener(OnGeneralVolumeChanged);
            _musicSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            _sfxSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            _mouseSensitivitySlider.onValueChanged.RemoveListener(OnMouseSensitivityChanged);

            _closeButton.onClick.RemoveListener(OnClickClosePopUp);
        }

        private void OnGeneralVolumeChanged(float value)
        {
            _saver.Data.GeneralVolume = value;
            ApplyVolume(MixerNames.General, value);
        }

        private void OnMusicVolumeChanged(float value)
        {
            _saver.Data.MusicVolume = value;
            ApplyVolume(MixerNames.Music, value);
        }

        private void OnSfxVolumeChanged(float value)
        {
            _saver.Data.SfxVolume = value;
            ApplyVolume(MixerNames.Sfx, value);
        }

        private void OnMouseSensitivityChanged(float value)
        {
            float normalized = Mathf.InverseLerp(_mouseSensitivitySlider.minValue, _mouseSensitivitySlider.maxValue, value);
            _saver.Data.MouseSensitivity = Mathf.Lerp(MinSensitivity, MaxSensitivity, normalized);
        }

        private void OnClickClosePopUp()
        {
            _saver.Save();
            Hide();
            _buttonsPopUp.Show();
        }

        private void ApplyVolume(string mixerParameter, float linearValue) =>
            AudioVolume.Apply(_audioMixer, mixerParameter, linearValue);
    }
}