using System.Collections;
using TMPro;
using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(TypewriterSubtitleAnimation), menuName = "SibJam/Subtitles/Animations/Typewriter")]
    public class TypewriterSubtitleAnimation : SubtitleAnimation
    {
        [field: SerializeField, Min(1f)] public float CharactersPerSecond { get; private set; } = 30f;
        [field: SerializeField] public bool UseUnscaledTime { get; private set; } = true;

        public override IEnumerator Play(TMP_Text target, string text)
        {
            target.text = text;
            target.ForceMeshUpdate();
            int totalVisible = target.textInfo.characterCount;
            target.maxVisibleCharacters = 0;

            float interval = 1f / CharactersPerSecond;
            float timer = 0f;
            int visibleCount = 0;

            while (visibleCount < totalVisible)
            {
                timer += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                while (timer >= interval && visibleCount < totalVisible)
                {
                    timer -= interval;
                    visibleCount++;
                }

                target.maxVisibleCharacters = visibleCount;

                yield return null;
            }

            target.maxVisibleCharacters = totalVisible;
        }
    }
}