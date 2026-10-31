using UnityEngine;
using UnityEngine.Audio;
using VContainer;

namespace SibJam
{
    public class AudioVolumeInitializer : MonoBehaviour
    {
        private ISaver _saver;
        private AudioMixer _mixer;

        [Inject]
        public void Construct(ISaver saver, AudioMixer mixer)
        {
            _saver = saver;
            _mixer = mixer;
        }

        private void Start()
        {
            AudioVolume.Apply(_mixer, MixerNames.General, _saver.Data.GeneralVolume);
            AudioVolume.Apply(_mixer, MixerNames.Music, _saver.Data.MusicVolume);
            AudioVolume.Apply(_mixer, MixerNames.Sfx, _saver.Data.SfxVolume);
        }
    }
}