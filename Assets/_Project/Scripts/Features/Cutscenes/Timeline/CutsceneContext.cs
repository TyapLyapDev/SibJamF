using UnityEngine;

namespace SibJam.Cutscenes
{
    // То, что нужно элементам таймлайна во время игры
    public class CutsceneContext
    {
        public CutsceneContext(VoiceLinePlayer voiceLines, Subtitles subtitles, AudioSource baseSource, IExposedPropertyTable bindings)
        {
            VoiceLines = voiceLines;
            Subtitles = subtitles;
            BaseSource = baseSource;
            Bindings = bindings;
        }

        public VoiceLinePlayer VoiceLines { get; }
        public Subtitles Subtitles { get; }

        // 2D-источник катсцены для звуков без своего источника
        public AudioSource BaseSource { get; }

        // Объекты сцены, привязанные к клипу на компоненте катсцены
        public IExposedPropertyTable Bindings { get; }

        public T Resolve<T>(ExposedReference<T> reference) where T : Object => reference.Resolve(Bindings);
    }
}
