using System.Collections;
using TMPro;
using UnityEngine;

namespace SibJam
{
    [CreateAssetMenu(fileName = nameof(InstantSubtitleAnimation), menuName = "SibJam/Subtitles/Animations/Instant")]
    public class InstantSubtitleAnimation : SubtitleAnimation
    {
        public override IEnumerator Play(TMP_Text target, string text)
        {
            target.text = text;
            yield break;
        }
    }
}