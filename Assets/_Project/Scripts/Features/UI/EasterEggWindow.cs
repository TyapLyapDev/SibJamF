using UnityEngine;
using VContainer;

namespace SibJam
{
    public class EasterEggWindow : PopUp
    {
        [SerializeField] private AudioClip _clip;

        private UiSfxService _uiSfxService;

        [Inject]
        public void Construct(UiSfxService uiSfxService)
        {
            _uiSfxService = uiSfxService;
        }

        private void OnEnable()
        {
            _uiSfxService.Play(_clip);
        }
    }
}