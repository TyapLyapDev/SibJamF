using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace SibJam
{
    [DisallowMultipleComponent]
    public class HoverSound : MonoBehaviour
    {
        [Header("Sound")]
        [SerializeField] private AudioClip[] _clips;

        private UiSfxService _uiSfxService;
        private readonly List<RaycastResult> _raycastResults = new();
        private bool _wasOver;

        [Inject]
        public void Construct(UiSfxService uiSfxService)
        {
            _uiSfxService = uiSfxService;
        }

        private void Update()
        {
            bool isOver = UiPointer.IsOver(transform, _raycastResults);

            if (isOver && !_wasOver)
                PlayRandomClip();

            _wasOver = isOver;
        }

        private void PlayRandomClip()
        {
            if (_clips == null || _clips.Length == 0)
                return;

            AudioClip clip = _clips[Random.Range(0, _clips.Length)];
            _uiSfxService.Play(clip);
        }
    }
}