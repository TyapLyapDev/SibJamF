using System;
using SibJam.TV;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Смена лица телика в этот момент, без звука: например, посреди длинной реплики телика.
    // Если телик молчит, лицо держится до следующей реплики
    [Serializable, CutsceneAction("Лицо телика", "#FFD27F")]
    public class TVFaceAction : CutsceneAction
    {
        [CutsceneLabel("Телевизор")]
        [SerializeField] private ExposedReference<TVScreen> _tv;
        [CutsceneLabel("Лицо"), CutsceneMainField]
        [SerializeField] private TVFace _face = TVFace.Evil;

        public override string Summary => _face.ToString();

        public override bool IsEmpty => false;

        public override void Play(CutsceneContext context)
        {
            TVScreen tv = context.Resolve(_tv);

            if (tv != null)
                tv.Show(_face);
        }
    }
}
