using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

namespace SibJam
{
    [DisallowMultipleComponent]
    public class ClickSound : MonoBehaviour, IPointerClickHandler
    {
        [Header("Sound")]
        [SerializeField] private AudioClip[] _clips;

        private UiSfxService _uiSfxService;
        private int _lastIndex = -1;

        [Inject]
        public void Construct(UiSfxService uiSfxService)
        {
            _uiSfxService = uiSfxService;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            PlayRandomClip();
        }

        private void PlayRandomClip()
        {
            if (_clips == null || _clips.Length == 0)
                return;

            int index;

            if (_clips.Length == 1)
            {
                index = 0;
            }
            else
            {
                do
                {
                    index = Random.Range(0, _clips.Length);
                } 
                while (index == _lastIndex);
            }

            _lastIndex = index;
            _uiSfxService.Play(_clips[index]);
        }
    }
}