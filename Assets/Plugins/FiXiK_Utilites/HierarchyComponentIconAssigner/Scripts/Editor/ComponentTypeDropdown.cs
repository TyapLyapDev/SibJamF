#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    public class ComponentTypeDropdown : AdvancedDropdown
    {
        private static readonly object _cacheLock = new();
        private static List<Type> _cachedComponentTypes;

        private readonly Action<Type> _onSelected;

        static ComponentTypeDropdown()
        {
            AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
        }

        public ComponentTypeDropdown(AdvancedDropdownState state, Action<Type> onSelected) : base(state)
        {
            _onSelected = onSelected;
        }

        public static void ResetCache()
        {
            lock (_cacheLock)
            {
                _cachedComponentTypes = null;
            }
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            AdvancedDropdownItem root = new(Constants.Component.Title);
            List<Type> componentTypes = GetComponentTypes();

            string noneNameSpaceText = Constants.Component.NoneNamespacesText;

            IOrderedEnumerable<IGrouping<string, Type>> groupedTypes = componentTypes
                .GroupBy(type => type.Namespace ?? noneNameSpaceText)
                .OrderBy(group => group.Key == noneNameSpaceText ? 0 : 1)
                .ThenBy(group => group.Key);

            foreach (IGrouping<string, Type> group in groupedTypes)
            {
                AdvancedDropdownItem groupItem = new(group.Key);

                foreach (Type type in group.OrderBy(type => type.Name))
                {
                    string typeNamespace = string.IsNullOrEmpty(type.Namespace) ? string.Empty : $"({type.Namespace})";
                    string displayName = $"{type.Name} {typeNamespace}";
                    TypeDropdownItem typeItem = new(displayName, type);
                    groupItem.AddChild(typeItem);
                }

                root.AddChild(groupItem);
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeDropdownItem typeItem)
                _onSelected?.Invoke(typeItem.Type);
            else
                Debug.LogWarning(string.Format(Constants.Component.MessageTypeWithIdNotFound, item.name));
        }

        private static List<Type> GetComponentTypes()
        {
            if (_cachedComponentTypes != null)
                return _cachedComponentTypes;

            lock (_cacheLock)
            {
                if (_cachedComponentTypes != null)
                    return _cachedComponentTypes;

                _cachedComponentTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(assembly =>
                    {
                        try { return assembly.GetTypes(); }
                        catch { return Type.EmptyTypes; }
                    })
                    .Where(type => typeof(Component).IsAssignableFrom(type) || type.IsInterface)
                    .ToList();

                return _cachedComponentTypes;
            }
        }

        private static void OnAfterAssemblyReload() =>
            ResetCache();
    }
}
#endif