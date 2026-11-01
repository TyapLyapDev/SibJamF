using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Video;

namespace SibJam.Props
{
    // На время игры выводит видео на все экраны видеостены, при выключении возвращает картинку покоя
    public class TVWallVideo : MonoBehaviour
    {
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        [SerializeField] private VideoPlayer _player;
        [SerializeField] private Material[] _screens;

        [Header("Кадрирование видео на стене")]
        [SerializeField] private Vector2 _tiling = Vector2.one;
        [SerializeField] private Vector2 _offset = Vector2.zero;

        [Header("Картинка, когда видео не играет")]
        [SerializeField] private Texture _idleImage;
        [SerializeField] private Vector2 _idleTiling = Vector2.one;
        [SerializeField] private Vector2 _idleOffset = Vector2.zero;

        [Header("Звук")]
        [SerializeField] private AudioSource _audio;
        [SerializeField] private AudioMixerGroup _mixerGroup;
        [SerializeField] private bool _muted;
        [SerializeField, Range(0f, 1f)] private float _volume = 0.6f;
        [SerializeField, Min(1f)] private float _hearingDistance = 40f;

        private void OnValidate() => ApplyAudio();

        private void OnEnable()
        {
            ApplyAudio();

            if (_player == null || _player.targetTexture == null || _player.clip == null)
                return;

            SetScreens(_player.targetTexture, _tiling, _offset);
            _player.Play();
        }

        private void OnDisable()
        {
            if (_idleImage != null)
                SetScreens(_idleImage, _idleTiling, _idleOffset);
        }

        private void SetScreens(Texture image, Vector2 tiling, Vector2 offset)
        {
            foreach (Material screen in _screens)
            {
                if (screen == null)
                    continue;

                screen.SetTexture(BaseMapId, image);
                screen.SetTextureScale(BaseMapId, tiling);
                screen.SetTextureOffset(BaseMapId, offset);
            }
        }

        private void ApplyAudio()
        {
            if (_audio == null)
                return;

            _audio.outputAudioMixerGroup = _mixerGroup;
            _audio.mute = _muted;
            _audio.volume = _volume;
            _audio.maxDistance = _hearingDistance;
            _audio.minDistance = Mathf.Min(_audio.minDistance, _hearingDistance);
        }
    }
}
