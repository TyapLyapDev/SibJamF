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
        [CutsceneLabel("Комната с теликами")]
        [SerializeField] private ExposedReference<TVRoom> _tv;
        [CutsceneLabel("Лицо"), CutsceneMainField]
        [SerializeField] private TVFace _face = TVFace.Evil;

        public override string Summary => _face.ToString();

        public override bool IsEmpty => false;

        public override void Play(CutsceneContext context)
        {
            TVRoom room = context.Resolve(_tv);

            if (room != null)
                room.ShowFace(_face);
        }
    }
}
