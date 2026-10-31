using UnityEngine;

namespace SibJam.Waves.Testing
{
    public class WavesDebugView : MonoBehaviour
    {
        [SerializeField] private WavesLoop _wavesLoop;
        [SerializeField] private Vector2 _screenPosition = new(10, 10);

        private void OnGUI()
        {
            if (_wavesLoop == null) return;

            GUILayout.BeginArea(new Rect(_screenPosition.x, _screenPosition.y, 300, 200), GUI.skin.box);
            GUILayout.Label(_wavesLoop.name);
            IWavesLoopInfo info = _wavesLoop.Info;

            GUILayout.Label($"State: {info.State}");
            GUILayout.Label($"Wave index: {info.WaveIndex}");
            GUILayout.Label($"Alive enemies: {info.AliveEnemiesCount}");
            GUILayout.Label(info.State == WavesLoopState.Delay
                ? $"Delay: {info.DelayTimeLeft:0.0}s"
                : "Delay: -");

            if (GUILayout.Button(_wavesLoop.IsRunning ? "Complete" : "Launch"))
            {
                if (_wavesLoop.IsRunning)
                    _wavesLoop.CompleteWaves();
                else
                    _wavesLoop.LaunchWaves();
            }

            GUILayout.EndArea();
        }
    }
}
