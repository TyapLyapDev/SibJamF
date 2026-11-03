using System.Collections.Generic;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Катсцена как файл, по аналогии с AnimationClip: элементы, у каждого своё время, и длина.
    // Ссылок на сцену тут нет: источники звука и цели вызовов — ExposedReference,
    // а сами объекты сцены привязаны на компоненте Cutscene. Правится в окне Window → SibJam → Катсцены
    [CreateAssetMenu(menuName = "SibJam/Катсцена", fileName = "Cutscene")]
    public class CutsceneClip : ScriptableObject
    {
        [SerializeReference] private List<CutsceneAction> _actions = new List<CutsceneAction>();
        [Tooltip("Сколько секунд идёт катсцена. 0 — до конца последнего элемента")]
        [SerializeField, Min(0f)] private float _length;

        public IReadOnlyList<CutsceneAction> Actions => _actions;

        public float Length => _length > 0f ? _length : EndTime;

        // Когда заканчивается последний элемент
        public float EndTime
        {
            get
            {
                float end = 0f;

                foreach (CutsceneAction action in _actions)
                {
                    if (action != null)
                        end = Mathf.Max(end, action.End);
                }

                return end;
            }
        }
    }
}
