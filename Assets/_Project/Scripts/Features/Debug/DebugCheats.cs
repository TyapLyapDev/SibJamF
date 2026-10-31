using _Project.Scripts.Enemies;
using SibJam.SecurityCameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam.Debugging
{
    // Только для отладки (редактор и development-билд): F1 — убить всех врагов, F2 — сломать камеры текущей локации
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
                KillAllEnemies();

            if (keyboard.f2Key.wasPressedThisFrame)
                BreakCurrentLocationCameras();
        }

        private void OnGUI() =>
            GUI.Label(new Rect(10f, Screen.height - 30f, 400f, 25f), "DEBUG: F1 — убить врагов, F2 — сломать камеры");

        private static void KillAllEnemies()
        {
            foreach (Enemy enemy in FindObjectsByType<Enemy>())
                enemy.Die();
        }

        private void BreakCurrentLocationCameras()
        {
            foreach (SecurityCamera securityCamera in _gameLoop.CurrentLocationLoop.GetComponentsInChildren<SecurityCamera>())
            {
                while (!securityCamera.IsBroken)
                    securityCamera.TakeHit();
            }
        }
#endif
    }
}
