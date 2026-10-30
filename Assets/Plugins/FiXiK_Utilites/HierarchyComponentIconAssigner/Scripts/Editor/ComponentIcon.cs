#if UNITY_EDITOR
using System;
using UnityEngine;

namespace FiXiK.HierarchyComponentIconAssigner
{
    [Serializable]
    public class ComponentIcon
    {
        public const string IconPropertyName = nameof(_icon);
        public const string TypePropertyName = nameof(_typeName);

        [SerializeField] private Texture2D _icon;
        [SerializeField] private string _typeName;

        private string _cachedTypeName;
        private Type _cachedType;

        public Texture2D Icon => _icon;

        public Type Type
        {
            get
            {
                return UpdateCachedType();
            }
            set
            {
                _cachedType = value;
                _typeName = value?.AssemblyQualifiedName;
                _cachedTypeName = _typeName;
            }
        }

        public Type UpdateCachedType()
        {
            if (_cachedTypeName != _typeName || _cachedType == null)
            {
                _cachedTypeName = _typeName;
                _cachedType = string.IsNullOrEmpty(_typeName) ? null : Type.GetType(_typeName);
            }

            return _cachedType;
        }

        public void Clear()
        {
            _typeName = null;
            _icon = null;
            _cachedType = null;
        }
    }
}
#endif