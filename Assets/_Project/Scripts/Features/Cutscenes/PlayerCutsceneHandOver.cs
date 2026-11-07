using SibJam.Weapons;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Катсцена от первого лица, «Действиями» на таймлайне: Hide прячет игрока и забирает камеру у CameraFollow,
    // HandOver ставит игрока туда, где стоит камера, и плавно отдаёт камеру обратно.
    // Высота пола — та, на которой игрок стоял в момент Hide
    public class PlayerCutsceneHandOver : MonoBehaviour
    {
        private float _floorHeight;

        // Старт с чекпоинта: катсцены не было, игрок сразу готов к бою. Без точки остаётся где стоит
        public void AppearAt(Transform point)
        {
            if (point != null)
            {
                var controller = GetComponent<CharacterController>();
                if (controller != null) controller.enabled = false;
                transform.SetPositionAndRotation(point.position, point.rotation);
                if (controller != null) controller.enabled = true;
            }

            if (TryGetComponent(out MeleeWeapon weapon))
                weapon.enabled = true;

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            _floorHeight = transform.position.y;
            gameObject.SetActive(false);

            CameraFollow follow = FindCameraFollow();

            if (follow != null)
                follow.Release();
        }

        public void HandOver()
        {
            Camera camera = Camera.main;

            if (camera != null)
                PlaceAt(new Pose(camera.transform.position, camera.transform.rotation));

            gameObject.SetActive(true);

            CameraFollow follow = FindCameraFollow();

            if (follow != null)
                follow.TakeOver();
        }

        private static CameraFollow FindCameraFollow()
        {
            Camera camera = Camera.main;
            return camera != null ? camera.GetComponent<CameraFollow>() : null;
        }

        // Ставим игрока ногами на пол под камерой и разворачиваем туда, куда она смотрит
        private void PlaceAt(Pose pose)
        {
            Vector3 forward = Vector3.ProjectOnPlane(pose.forward, Vector3.up);
            Quaternion yaw = forward.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(forward) : transform.rotation;
            Vector3 feet = new Vector3(pose.position.x, _floorHeight, pose.position.z);

            if (TryGetComponent(out CharacterController controller))
                feet = FindFreeSpot(feet, yaw * Vector3.forward, controller);

            transform.SetPositionAndRotation(feet, yaw);
        }

        // Капсула игрока толще тела героя: если в точке финала она задевает мебель, сдвигаем вперёд, пока не встанет свободно
        private static Vector3 FindFreeSpot(Vector3 feet, Vector3 forward, CharacterController controller)
        {
            const float Step = 0.1f;
            const int MaxSteps = 15;
            const float FloorGap = 0.05f;

            float radius = controller.radius;
            Vector3 bottom = controller.center + Vector3.up * (FloorGap - controller.height * 0.5f + radius);
            Vector3 top = controller.center + Vector3.up * (controller.height * 0.5f - radius);

            for (int i = 0; i < MaxSteps; i++)
            {
                Vector3 candidate = feet + forward * (Step * i);

                if (!Physics.CheckCapsule(candidate + bottom, candidate + top, radius, ~0, QueryTriggerInteraction.Ignore))
                    return candidate;
            }

            return feet;
        }
    }
}
