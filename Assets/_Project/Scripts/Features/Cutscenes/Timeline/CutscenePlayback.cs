using System;
using System.Collections.Generic;

namespace SibJam.Cutscenes
{
    // Один проход по клипу: какие элементы уже запущены. Своё на каждый запуск — клип общий и не меняется.
    // Время задаёт катсцена, проход только запускает элементы, до которых оно дошло
    public class CutscenePlayback
    {
        private readonly IReadOnlyList<CutsceneAction> _actions;
        private readonly CutsceneContext _context;
        private readonly bool[] _fired;

        public CutscenePlayback(CutsceneClip clip, CutsceneContext context)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));

            _actions = clip.Actions;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fired = new bool[_actions.Count];
        }

        public void Tick(float time)
        {
            for (int i = 0; i < _fired.Length; i++)
            {
                CutsceneAction action = _actions[i];

                if (_fired[i] || action == null || time < action.Time)
                    continue;

                _fired[i] = true;
                action.Play(_context);
            }
        }

        // Конец или пропуск: невыполненные элементы отдают обязательное, всё звучащее глушится
        public void Finish()
        {
            for (int i = 0; i < _fired.Length; i++)
            {
                if (_fired[i])
                    continue;

                _fired[i] = true;
                _actions[i]?.PlaySkipped(_context);
            }

            foreach (CutsceneAction action in _actions)
                action?.Stop(_context);
        }
    }
}
