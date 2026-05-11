using UnityEngine;
using TMPro;

namespace SunlessReach.UI
{
    public class FpsCounter : MonoBehaviour
    {
        private Canvas _canvas;
        private TextMeshProUGUI _label;
        private float _accum;
        private int _frames;
        private const float Interval = 0.5f;

        private void Awake()
        {
            BuildUI();
            Apply();
        }

        private void OnEnable()  { Core.AudioPrefs.OnChanged += Apply; }
        private void OnDisable() { Core.AudioPrefs.OnChanged -= Apply; }

        private void BuildUI()
        {
            var canvasGo = new GameObject("FpsCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 32767;   // draw above everything (HUD, death screen, etc.)

            var labelGo = new GameObject("FpsText");
            labelGo.transform.SetParent(canvasGo.transform, false);
            var rt = labelGo.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -10f);
            rt.sizeDelta = new Vector2(160f, 34f);

            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 22f;
            _label.fontStyle = FontStyles.Bold;
            _label.alignment = TextAlignmentOptions.TopRight;
            _label.color = new Color(1f, 1f, 1f, 0.5f);   // semi-transparent
            _label.raycastTarget = false;
            _label.text = string.Empty;
        }

        private void Apply()
        {
            if (_canvas != null) _canvas.enabled = Core.AudioPrefs.ShowFps;
        }

        private void Update()
        {
            if (_canvas == null || !_canvas.enabled) return;
            _accum += Time.unscaledDeltaTime;
            _frames++;
            if (_accum >= Interval)
            {
                int fps = _accum > 0f ? Mathf.RoundToInt(_frames / _accum) : 0;
                _label.text = fps + " FPS";
                _accum = 0f;
                _frames = 0;
            }
        }
    }
}
