using System.Collections;
using TMPro;
using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(DecryptSubtitleAnimation), menuName = "SibJam/Subtitles/Animations/Decrypt")]
    public class DecryptSubtitleAnimation : SubtitleAnimation
    {
        private const string DefaultScramble = "!@#$%^&*()_+-=[]{}|;:,.<>?/~`";

        [field: SerializeField, Min(1f)] public float CharactersPerSecond { get; private set; } = 25f;
        [field: SerializeField, TextArea(1, 2)] public string ScrambleCharacters { get; private set; } = DefaultScramble;
        [field: SerializeField] public bool UseUnscaledTime { get; private set; } = true;

        public override IEnumerator Play(TMP_Text target, string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                target.text = "";
                yield break;
            }

            bool[] isVisible = ComputeVisibleMap(text);
            int totalVisible = 0;
            for (int i = 0; i < isVisible.Length; i++)
                if (isVisible[i]) totalVisible++;

            float interval = 1f / CharactersPerSecond;
            float timer = 0f;
            int revealedCount = 0;

            target.text = BuildDisplay(text, isVisible, revealedCount);

            while (revealedCount < totalVisible)
            {
                timer += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                while (timer >= interval && revealedCount < totalVisible)
                {
                    timer -= interval;
                    revealedCount++;
                }

                target.text = BuildDisplay(text, isVisible, revealedCount);
                yield return null;
            }

            target.text = text;
        }

        private bool[] ComputeVisibleMap(string text)
        {
            bool[] isVisible = new bool[text.Length];
            int tagDepth = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<') tagDepth++;
                isVisible[i] = tagDepth == 0;
                if (c == '>') tagDepth = Mathf.Max(0, tagDepth - 1);
            }

            return isVisible;
        }

        private string BuildDisplay(string text, bool[] isVisible, int revealedVisibleCount)
        {
            char[] chars = new char[text.Length];
            int visibleIndex = 0;

            for (int i = 0; i < text.Length; i++)
            {
                if (!isVisible[i])
                {
                    chars[i] = text[i];
                    continue;
                }

                if (visibleIndex < revealedVisibleCount || text[i] == ' ')
                    chars[i] = text[i];
                else
                    chars[i] = GetScrambleChar();

                visibleIndex++;
            }

            return new string(chars);
        }

        private char GetScrambleChar()
        {
            if (string.IsNullOrEmpty(ScrambleCharacters))
                return '?';

            return ScrambleCharacters[Random.Range(0, ScrambleCharacters.Length)];
        }
    }
}