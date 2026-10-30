using UnityEngine;

namespace SibJam.Waves.Testing
{
    public class WavesDebugView : MonoBehaviour
    {
        [SerializeField] private WavesLifeCycle _wavesLifeCycle;
        [SerializeField] private Vector2 _screenPosition = new(10, 10);

        private void OnGUI()
        {
            if (_wavesLifeCycle == null || _wavesLifeCycle.Waves == null) return;

            var waves = _wavesLifeCycle.Waves;
            var delays = _wavesLifeCycle.Delays;

            GUILayout.BeginArea(new Rect(_screenPosition.x, _screenPosition.y, 300, 200), GUI.skin.box);
            GUILayout.Label(_wavesLifeCycle.name);
            GUILayout.Label($"Running: {_wavesLifeCycle.IsRunning}  Completed: {_wavesLifeCycle.IsCompleted}");
            GUILayout.Label($"Wave index: {waves.CurrentIndex}");
            GUILayout.Label($"Alive enemies: {waves.CurrentWave?.AliveCount ?? 0}");
            GUILayout.Label(delays.IsCompleted
                ? "Delay: -"
                : $"Delay #{delays.CurrentIndex}: {delays.TimeLeft:0.0}s");

            if (GUILayout.Button(_wavesLifeCycle.IsRunning ? "Complete" : "Launch"))
            {
                if (_wavesLifeCycle.IsRunning)
                    _wavesLifeCycle.CompleteWaves();
                else
                    _wavesLifeCycle.LaunchWaves();
            }

            GUILayout.EndArea();
        }
    }
}
