using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SibJam.MainMenu
{
    // Таймкод камеры наблюдения в углу меню: «КАМ-01 · чч:мм:сс», идёт от случайного часа, точка «REC» мигает
    public class MenuTimecode : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;
        [SerializeField] private string _prefix = "КАМ-01";
        [SerializeField] private Graphic _recDot;

        private float _offset;

        private void Awake() => _offset = Random.Range(0f, 86400f);

        private void Update()
        {
            int total = (int)(_offset + Time.unscaledTime) % 86400;
            _text.text = $"{_prefix} · {total / 3600:00}:{total / 60 % 60:00}:{total % 60:00}";

            if (_recDot != null)
                _recDot.enabled = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f;
        }
    }
}
