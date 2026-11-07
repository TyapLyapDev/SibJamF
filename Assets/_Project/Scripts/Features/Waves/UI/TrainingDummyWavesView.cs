using _Project.Scripts.Enemies;
using SibJam.Combat;
using SibJam.Locations;
using UnityEngine;

namespace SibJam.Waves.UI
{
    // Тренировка использует те же панели волн и камер, что и боевые зоны.
    public sealed class TrainingDummyWavesView : MonoBehaviour
    {
        private TrainingDummyWaveSpawner _spawner;

        public void Bind(TrainingDummyWaveSpawner spawner)
        {
            _spawner = spawner;
            var location = FindAnyObjectByType<CurrentLocationInfo>();
            if (location != null)
                location.SetTraining(spawner);

            foreach (var opener in FindObjectsByType<DoorOpener>(FindObjectsInactive.Include))
            {
                if (opener.gameObject.scene != gameObject.scene) continue;

                // SM_Gates первой двери стоит поперёк оси Z; выход из комнаты — в +Z.
                // Триггер остаётся на месте, когда полотно двери поднимается.
                _spawner.SetEntrance(opener.transform.position.z + 0.15f);
                transform.position = opener.transform.position + new Vector3(0f, 1.25f, 1f);
                var trigger = gameObject.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(4f, 2.5f, 1f);
                var body = gameObject.AddComponent<Rigidbody>();
                body.isKinematic = true;
                body.useGravity = false;
                return;
            }

            Debug.LogError("[Training] First door is missing; entry trigger was not created", this);
        }

        private void OnTriggerEnter(Collider other)
        {
            var player = other.GetComponentInParent<PlayerTarget>();
            if (player != null && player.isActiveAndEnabled)
                _spawner.EnterTraining();
        }
    }
}
