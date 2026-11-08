using _Project.Scripts.Enemies;
using SibJam.Checkpoints;
using SibJam.Player;
using SibJam.SecurityCameras;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SibJam.Debugging
{
    // Только для отладки (редактор и development-билд): F1 — убить всех врагов, F2 — сломать камеры текущей локации, F3 — снять игроку хп,
    // Shift+1…9 — перезапуск сцены с чекпоинта нужной локации (как «Продолжить» из меню)
    public class DebugCheats : MonoBehaviour
    {
        [SerializeField] private GameLoop _gameLoop;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField, Min(0f)] private float _debugDamage = 50f;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null) return;

            if (keyboard.f1Key.wasPressedThisFrame)
                KillAllEnemies();

            if (keyboard.f2Key.wasPressedThisFrame)
                BreakCurrentLocationCameras();

            if (keyboard.f3Key.wasPressedThisFrame && _playerHealth != null)
                _playerHealth.TakeDamage(_debugDamage);

            if (keyboard.shiftKey.isPressed)
            {
                for (int i = 0; i < CheckpointKeys.Length; i++)
                {
                    if (keyboard[CheckpointKeys[i]].wasPressedThisFrame)
                        TeleportToCheckpoint(i);
                }
            }
        }

        private static readonly Key[] CheckpointKeys =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9,
        };

        // Через штатный путь «продолжить»: сохраняем чекпоинт и перезагружаем сцену —
        // локации, двери и враги поднимаются в корректном состоянии, без ручной перестановки игрока
        private void TeleportToCheckpoint(int locationIndex)
        {
            if (_gameLoop == null || locationIndex >= _gameLoop.LocationCount)
            {
                Debug.LogWarning($"[DebugCheats] No location with index {locationIndex}");
                return;
            }

            var scope = FindAnyObjectByType<ProjectLifetimeScope>();

            if (scope == null || scope.Container == null)
            {
                Debug.LogWarning("[DebugCheats] ProjectLifetimeScope not found — start the game from the boot/menu scene");
                return;
            }

            var checkpoint = (ICheckpoint)scope.Container.Resolve(typeof(ICheckpoint));
            var sceneLoader = (ISceneLoader)scope.Container.Resolve(typeof(ISceneLoader));

            checkpoint.Save(locationIndex);
            checkpoint.RequestContinue();
            Debug.Log($"[DebugCheats] Restarting from checkpoint of location {locationIndex + 1}");
            sceneLoader.Load(SceneNames.Main);
        }

        private void OnGUI() =>
            GUI.Label(new Rect(10f, Screen.height - 30f, 700f, 25f), $"DEBUG: F1 — убить врагов, F2 — сломать камеры, F3 — −{_debugDamage:0} хп, Shift+1…{Mathf.Min(_gameLoop != null ? _gameLoop.LocationCount : 0, 9)} — чекпоинт");

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
