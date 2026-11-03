using System;
using UnityEngine;

namespace SibJam.Cutscenes
{
    // Главная камера едет за объектом сцены (например, костью-камерой на голове рига) и смотрит его глазами.
    // Тряску поверх накладывает CameraShaker на камере. Последний кадр остаётся на камере до конца катсцены.
    // Спрятать голову и приблизить near clip для камеры внутри головы — HeadHider, через «Действия»
    [Serializable, CutsceneAction("Крепление камеры", "#FFC14D")]
    public class CameraAnchorAction : CutsceneAction
    {
        private const float DefaultDuration = 5f;

        [CutsceneLabel("За чем едет", "Объект или кость, глазами которой смотрит камера"), CutsceneMainField]
        [SerializeField] private ExposedReference<Transform> _anchor;
        [CutsceneLabel("Сдвиг, м", "В осях объекта: Y — выше, Z — вперёд")]
        [SerializeField] private Vector3 _shift;
        [CutsceneLabel("Длина, с")]
        [SerializeField, Min(0.1f)] private float _duration = DefaultDuration;

        public override float Duration => _duration;

        public override string Summary => "Крепление камеры";

        // Не привязан объект — не за чем ехать
        public override bool IsEmpty => PropertyName.IsNullOrEmpty(_anchor.exposedName);

        public override void Play(CutsceneContext context) { }

        public override void LateEvaluate(CutsceneContext context, float localTime)
        {
            Camera camera = Camera.main;

            if (camera == null || !TryGetPose(context.Bindings, out Pose pose))
                return;

            camera.transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        public Transform ResolveAnchor(IExposedPropertyTable bindings) => _anchor.Resolve(bindings);

        public bool TryGetPose(IExposedPropertyTable bindings, out Pose pose)
        {
            Transform anchor = ResolveAnchor(bindings);
            pose = anchor != null ? new Pose(anchor.position + anchor.rotation * _shift, anchor.rotation) : default;
            return anchor != null;
        }
    }
}
