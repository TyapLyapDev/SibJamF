#if UNITY_EDITOR
using System;
using UnityEditor.IMGUI.Controls;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public class TypeDropdownItem : AdvancedDropdownItem
    {
        public Type Type { get; }

        public TypeDropdownItem(string name, Type type) : base(name)
        {
            Type = type;
        }
    }
}
#endif