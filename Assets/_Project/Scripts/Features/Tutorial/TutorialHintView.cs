using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SibJam.Tutorial
{
    // Окно обучения у правого края: список «клавиша — действие», выполненные строки гаснут.
    // Строки клонируются из шаблона _rowTemplate (у него дочерние KeyBox/Key и Text)
    [RequireComponent(typeof(CanvasGroup))]
    public class TutorialHintView : MonoBehaviour
    {
        [SerializeField] private RectTransform _rowTemplate;
        [SerializeField, Min(0.01f)] private float _fadeTime = 0.25f;
        [SerializeField, Range(0f, 1f)] private float _doneAlpha = 0.3f;

        private readonly List<CanvasGroup> _rows = new List<CanvasGroup>();
        private CanvasGroup _group;
        private object _owner;

        private void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            _rowTemplate.gameObject.SetActive(false);
        }

        private void Update()
        {
            float target = _owner != null ? 1f : 0f;
            _group.alpha = Mathf.MoveTowards(_group.alpha, target, Time.unscaledDeltaTime / _fadeTime);
        }

        // Последняя показавшая зона — владелец; спрятать может только она
        public void Show(object owner, IReadOnlyList<TutorialZone.Step> steps)
        {
            _owner = owner;

            while (_rows.Count < steps.Count)
            {
                RectTransform row = Instantiate(_rowTemplate, _rowTemplate.parent);
                var group = row.GetComponent<CanvasGroup>();
                if (group == null)
                    group = row.gameObject.AddComponent<CanvasGroup>();
                _rows.Add(group);
            }

            for (int i = 0; i < _rows.Count; i++)
            {
                bool used = i < steps.Count;
                _rows[i].gameObject.SetActive(used);

                if (!used)
                    continue;

                Transform row = _rows[i].transform;
                Transform keyBox = row.Find("KeyBox");
                bool hasKey = !string.IsNullOrEmpty(steps[i].Key);
                keyBox.gameObject.SetActive(hasKey);

                if (hasKey)
                    keyBox.GetComponentInChildren<TMP_Text>(true).text = steps[i].Key;

                row.Find("Text").GetComponent<TMP_Text>().text = steps[i].Text;
                _rows[i].alpha = 1f;
            }
        }

        public void MarkDone(object owner, int index)
        {
            if (_owner == owner && index >= 0 && index < _rows.Count)
                _rows[index].alpha = _doneAlpha;
        }

        public void Hide(object owner)
        {
            if (_owner == owner)
                _owner = null;
        }
    }
}
