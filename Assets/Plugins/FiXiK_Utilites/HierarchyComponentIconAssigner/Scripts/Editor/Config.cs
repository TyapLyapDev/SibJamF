#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    [Serializable]
    public class Config : ScriptableObject
    {
        public const string ComponentIconListPropertyName = nameof(_componentIconList);
        public const string EnablerPropertyName = nameof(_enabled);

        [Tooltip(Constants.Settings.TooltipIconRenderingSwitch)]
        [SerializeField] private bool _enabled = true;

        [Tooltip(Constants.Settings.TooltipChoosingIconAndComponent)]
        [SerializeField] private List<ComponentIcon> _componentIconList = new();

        public event Action Changed;

        public bool Enabled => _enabled;

        public IReadOnlyList<ComponentIcon> ComponentIconList => _componentIconList;

        private void OnValidate()
        {
            foreach (ComponentIcon componentIcon in _componentIconList)
                componentIcon.UpdateCachedType();

            Changed?.Invoke();
        }
    }
}
#endif