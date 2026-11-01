using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SibJam
{
    [DisallowMultipleComponent]
    public class HoverNotifier : MonoBehaviour
    {
        private readonly List<RaycastResult> _raycastResults = new();
        private bool _wasOver;

        public event Action Hovered;

        private void Update()
        {
            bool isOver = UiPointer.IsOver(transform, _raycastResults);

            if (isOver && !_wasOver)
                Hovered?.Invoke();

            _wasOver = isOver;
        }
    }
}