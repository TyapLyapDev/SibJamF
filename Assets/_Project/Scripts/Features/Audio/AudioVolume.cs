using UnityEngine;
using UnityEngine.Audio;

namespace SibJam
{
    public static class AudioVolume
    {
        public static void Apply(AudioMixer mixer, string mixerParameter, float linearValue)
        {
            if (mixer == null)
                return;

            float clampedValue = Mathf.Max(linearValue, 0.0001f);
            float decibels = Mathf.Log10(clampedValue) * 20f;
            mixer.SetFloat(mixerParameter, decibels);
        }
    }
}