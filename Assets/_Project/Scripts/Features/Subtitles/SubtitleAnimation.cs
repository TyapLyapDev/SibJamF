using System.Collections;
using TMPro;
using UnityEngine;

namespace SibJam
{
    public abstract class SubtitleAnimation : ScriptableObject
    {
        public abstract IEnumerator Play(TMP_Text target, string text);
    }
}