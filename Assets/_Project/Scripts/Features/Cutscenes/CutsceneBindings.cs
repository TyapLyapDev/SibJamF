using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SibJam.Cutscenes
{
    // Привязки клипа к объектам сцены: по ним ExposedReference в клипе находят источники звука, цели вызовов и т.п.
    // Клип — файл и не может ссылаться на сцену сам, поэтому ссылки хранит компонент катсцены
    [Serializable]
    public class CutsceneBindings : IExposedPropertyTable
    {
        [SerializeField] private List<int> _ids = new List<int>();
        [SerializeField] private List<Object> _values = new List<Object>();

        public Object GetReferenceValue(PropertyName id, out bool idValid)
        {
            int index = _ids.IndexOf(id.GetHashCode());
            idValid = index >= 0;
            return idValid ? _values[index] : null;
        }

        public void SetReferenceValue(PropertyName id, Object value)
        {
            int index = _ids.IndexOf(id.GetHashCode());

            if (index >= 0)
            {
                _values[index] = value;
                return;
            }

            _ids.Add(id.GetHashCode());
            _values.Add(value);
        }

        public void ClearReferenceValue(PropertyName id)
        {
            int index = _ids.IndexOf(id.GetHashCode());

            if (index < 0)
                return;

            _ids.RemoveAt(index);
            _values.RemoveAt(index);
        }
    }
}
