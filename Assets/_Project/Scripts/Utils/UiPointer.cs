using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SibJam
{
    public static class UiPointer
    {
        public static bool IsOver(Transform target, List<RaycastResult> buffer)
        {
            if (target == null) return false;

            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null) return false;
            if (Mouse.current == null) return false;

            PointerEventData pointerData = new(eventSystem)
            {
                position = Mouse.current.position.ReadValue(),
            };

            buffer.Clear();
            eventSystem.RaycastAll(pointerData, buffer);

            foreach (RaycastResult result in buffer)
            {
                if (result.gameObject == null) continue;

                Transform t = result.gameObject.transform;
                return t == target || t.IsChildOf(target);
            }

            return false;
        }
    }
}