using UnityEngine;
using UnityEngine.UI;

namespace SibJam
{
    // World-space слайдер хп над объектом, собирается в рантайме, чтобы не трогать префабы.
    // Скрыт, пока владелец не получил урон
    public class WorldHealthBar : MonoBehaviour
    {
        private const float Width = 1.1f;
        private const float Height = 0.11f;
        private const float Inset = 0.02f;
        private const float TopOffset = 0.35f;

        private Slider _slider;
        private Transform _bar;
        private float _height;
        private bool _shown;

        private void Awake()
        {
            _height = CalcLocalHeight() + TopOffset;
            Build();
        }

        public void SetValue(float value01)
        {
            _shown = true;
            _bar.gameObject.SetActive(enabled);
            _slider.value = Mathf.Clamp01(value01);
        }

        private void LateUpdate()
        {
            if (!_bar.gameObject.activeSelf) return;

            _bar.position = transform.position + Vector3.up * _height;
            if (Camera.main is { } cam)
                _bar.rotation = cam.transform.rotation;
        }

        private void OnEnable()
        {
            if (_bar != null && _shown) _bar.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            if (_bar != null) _bar.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (_bar != null) Destroy(_bar.gameObject);
        }

        private float CalcLocalHeight()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return 2f;

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds.max.y - transform.position.y;
        }

        private void Build()
        {
            var canvasGo = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas));
            _bar = canvasGo.transform;
            // Не дочерний — чтобы масштаб владельца не влиял на размер полоски
            canvasGo.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)_bar;
            canvasRect.sizeDelta = new Vector2(Width, Height);

            // Стиль HUD: почти чёрная подложка, внутри красная заливка с отступом в пару пикселей
            var background = CreateImage("Background", canvasRect, new Color(0.043f, 0.047f, 0.059f, 0.9f));
            Stretch(background.rectTransform);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform)).GetComponent<RectTransform>();
            fillArea.SetParent(canvasRect, false);
            Stretch(fillArea);
            fillArea.offsetMin = new Vector2(Inset, Inset);
            fillArea.offsetMax = new Vector2(-Inset, -Inset);

            var fill = CreateImage("Fill", fillArea, new Color(0.898f, 0.282f, 0.302f));
            Stretch(fill.rectTransform);

            _slider = canvasGo.AddComponent<Slider>();
            _slider.fillRect = fill.rectTransform;
            _slider.interactable = false;
            _slider.transition = Selectable.Transition.None;
            _slider.minValue = 0f;
            _slider.maxValue = 1f;
            _slider.value = 1f;

            canvasGo.SetActive(false);
        }

        private static Image CreateImage(string name, Transform parent, Color color)
        {
            var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.transform.SetParent(parent, false);
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
