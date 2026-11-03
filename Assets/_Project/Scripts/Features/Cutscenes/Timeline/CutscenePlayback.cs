using System;
using System.Collections.Generic;

namespace SibJam.Cutscenes
{
    // Один проход по клипу: какие элементы уже запущены и какие ещё идут. Своё на каждый запуск — клип общий и не меняется.
    // Время задаёт катсцена, проход запускает элементы, до которых оно дошло, и ведёт длящиеся
    public class CutscenePlayback
    {
        private readonly IReadOnlyList<CutsceneAction> _actions;
        private readonly CutsceneContext _context;
        private readonly bool[] _fired;
        private readonly bool[] _done;
        // Элемент шёл на этом кадре: в LateTick он получит то же время
        private readonly bool[] _active;
        private readonly float[] _localTime;

        public CutscenePlayback(CutsceneClip clip, CutsceneContext context)
        {
            if (clip == null)
                throw new ArgumentNullException(nameof(clip));

            _actions = clip.Actions;
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _fired = new bool[_actions.Count];
            _done = new bool[_actions.Count];
            _active = new bool[_actions.Count];
            _localTime = new float[_actions.Count];
        }

        // Автозапуск отменён: элементы убирают с экрана то, что сохранено в сцене для начала катсцены
        public static void Cancel(CutsceneClip clip, CutsceneContext context)
        {
            foreach (CutsceneAction action in clip.Actions)
                action?.Cancel(context);
        }

        public void Tick(float time)
        {
            for (int i = 0; i < _fired.Length; i++)
            {
                CutsceneAction action = _actions[i];

                if (_done[i] || action == null || time < action.Time)
                    continue;

                if (!_fired[i])
                {
                    _fired[i] = true;
                    action.Play(_context);
                }

                _localTime[i] = Math.Min(time - action.Time, action.Duration);
                _active[i] = true;
                _done[i] = time >= action.End;
                action.Evaluate(_context, _localTime[i]);
            }
        }

        public void LateTick()
        {
            for (int i = 0; i < _active.Length; i++)
            {
                if (!_active[i])
                    continue;

                _active[i] = false;
                _actions[i]?.LateEvaluate(_context, _localTime[i]);
            }
        }

        // Конец или пропуск: невыполненные элементы отдают обязательное, идущие доводятся до своего конца,
        // всё звучащее глушится
        public void Finish()
        {
            // По времени, а не по порядку в файле: пропущенные «включить» и «выключить» выполнятся в верном порядке.
            // При равном времени — порядок в файле (Sort нестабилен)
            var order = new List<int>();

            for (int i = 0; i < _fired.Length; i++)
            {
                if (!_done[i] && _actions[i] != null)
                    order.Add(i);
            }

            order.Sort((a, b) =>
            {
                int byTime = _actions[a].Time.CompareTo(_actions[b].Time);
                return byTime != 0 ? byTime : a.CompareTo(b);
            });

            // Сначала длящиеся элементы встают в свой последний кадр: анимации, затем камера (из LateEvaluate)
            var lasting = new List<CutsceneAction>();

            foreach (int i in order)
            {
                CutsceneAction action = _actions[i];

                if (action.Duration <= 0f)
                    continue;

                lasting.Add(action);

                if (_fired[i])
                    action.Evaluate(_context, action.Duration);
                else
                    action.PlaySkipped(_context);
            }

            foreach (CutsceneAction action in lasting)
                action.LateEvaluate(_context, action.Duration);

            // Мгновенные — последними: вызовы видят уже собранный последний кадр (например, игрок встаёт на место камеры)
            foreach (int i in order)
            {
                CutsceneAction action = _actions[i];

                if (action.Duration <= 0f && !_fired[i])
                    action.PlaySkipped(_context);
            }

            for (int i = 0; i < _fired.Length; i++)
            {
                _fired[i] = true;
                _done[i] = true;
            }

            Array.Clear(_active, 0, _active.Length);

            foreach (CutsceneAction action in _actions)
                action?.Stop(_context);
        }
    }
}
