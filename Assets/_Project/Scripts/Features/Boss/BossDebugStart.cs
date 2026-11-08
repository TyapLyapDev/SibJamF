#if UNITY_EDITOR
using System.Collections;
using SibJam.Cutscenes;
using SibJam.Player;
using SibJam.Weapons;
using UnityEditor;
using UnityEngine;

namespace SibJam.Boss
{
    // Отладка: с включённым SibJam/Debug/Start At Boss Play стартует без катсцены, игрок сразу у входа на арену босса
    public class BossDebugStart : MonoBehaviour
    {
        public const string PrefKey = "SibJam.Debug.StartAtBoss";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            if (!EditorPrefs.GetBool(PrefKey, false) || FindFirstObjectByType<BossFight>(FindObjectsInactive.Include) == null)
                return;

            // AfterSceneLoad идёт до Start, поэтому автозапуск катсцен ещё можно отменить
            foreach (Cutscene cutscene in FindObjectsByType<Cutscene>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                cutscene.CancelAutoPlay();

            new GameObject(nameof(BossDebugStart)).AddComponent<BossDebugStart>();
        }

        private IEnumerator Start()
        {
            // Ждём кадр, чтобы отработали Start остальных систем (чекпоинт тоже двигает игрока)
            yield return null;

            var fight = FindFirstObjectByType<BossFight>(FindObjectsInactive.Include);
            var health = FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);

            if (health == null || fight.RespawnPoint == null)
            {
                Debug.LogWarning("[BossDebugStart] No player or BossFight RespawnPoint, staying at scene start");
                Destroy(gameObject);
                yield break;
            }

            // Точка стоит на мосту перед дверью арены: делаем босса текущей локацией, чтобы дверь открылась
            var gameLoop = FindFirstObjectByType<GameLoop>(FindObjectsInactive.Include);
            if (gameLoop != null && gameLoop.LocationCount > 0)
                gameLoop.StartFrom(gameLoop.LocationCount - 1);

            GameObject player = health.gameObject;
            var controller = player.GetComponent<CharacterController>();
            if (controller != null) controller.enabled = false;
            player.transform.SetPositionAndRotation(fight.RespawnPoint.position, fight.RespawnPoint.rotation);
            if (controller != null) controller.enabled = true;

            if (player.TryGetComponent(out MeleeWeapon weapon))
                weapon.enabled = true;

            player.SetActive(true);
            Debug.Log("[BossDebugStart] Player moved to the boss arena (SibJam/Debug/Start At Boss)");
            Destroy(gameObject);
        }
    }
}
#endif
