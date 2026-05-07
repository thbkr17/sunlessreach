using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class BossHealthUI : MonoBehaviour
    {
        private static readonly Color FrameGold    = new Color(0.78f, 0.62f, 0.28f, 1f);
        private static readonly Color BrightGold   = new Color(1.00f, 0.82f, 0.40f, 1f);
        private static readonly Color BarFillColor = new Color(0.82f, 0.20f, 0.18f, 1f);
        private static readonly Color BarBgColor   = new Color(0.05f, 0.05f, 0.08f, 0.95f);

        private GameObject _root;
        private RectTransform _fillRect;
        private TextMeshProUGUI _nameLabel;
        private float _displayedFraction = 1f;
        private float _targetFraction = 1f;

        private void OnEnable()
        {
            EventBus.OnBossEncountered += Show;
            EventBus.OnBossHealthChanged += UpdateHealth;
            EventBus.OnBossDefeated += Hide;
        }

        private void OnDisable()
        {
            EventBus.OnBossEncountered -= Show;
            EventBus.OnBossHealthChanged -= UpdateHealth;
            EventBus.OnBossDefeated -= Hide;
        }

        private void Update()
        {
            if (_root == null) return;
            if (!_root.activeSelf) return;
            _displayedFraction = Mathf.MoveTowards(_displayedFraction, _targetFraction, Time.unscaledDeltaTime * 0.6f);
            if (_fillRect != null)
                _fillRect.anchorMax = new Vector2(_displayedFraction, 1f);
        }

        private void Show(string bossName, int max)
        {
            EnsureUI();
            if (_root == null) return;
            _targetFraction = 1f;
            _displayedFraction = 1f;
            if (_nameLabel != null) _nameLabel.text = (bossName ?? "BOSS").ToUpper();
            _root.SetActive(true);
        }

        private void UpdateHealth(int cur, int max)
        {
            if (_root == null) return;
            _targetFraction = max <= 0 ? 0f : Mathf.Clamp01((float)cur / max);
        }

        private void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void EnsureUI()
        {
            // Rebuild if root was destroyed (scene reload)
            if (_root != null) return;

            Canvas canvas = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.gameObject.name == "HUDCanvas") { canvas = c; break; }
            }
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("BossHUDCanvas");
                canvasObj.transform.SetParent(transform, false);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 50;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            }

            BuildUI(canvas.transform);
        }

        private void BuildUI(Transform parent)
        {
            _root = new GameObject("BossHealthUI");
            _root.transform.SetParent(parent, false);
            var rootRect = _root.AddComponent<RectTransform>();
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 1f);
            rootRect.anchoredPosition = new Vector2(0, -36);
            rootRect.sizeDelta = new Vector2(720, 70);

            var nameObj = new GameObject("Name");
            nameObj.transform.SetParent(_root.transform, false);
            var nameRect = nameObj.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1f);
            nameRect.sizeDelta = new Vector2(0, 30);
            nameRect.anchoredPosition = Vector2.zero;
            _nameLabel = nameObj.AddComponent<TextMeshProUGUI>();
            _nameLabel.text = "BOSS";
            _nameLabel.fontSize = 22;
            _nameLabel.fontStyle = FontStyles.Bold;
            _nameLabel.color = BrightGold;
            _nameLabel.alignment = TextAlignmentOptions.Center;
            _nameLabel.characterSpacing = 12;

            var barBg = new GameObject("BarBg");
            barBg.transform.SetParent(_root.transform, false);
            var barBgRect = barBg.AddComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0, 0);
            barBgRect.anchorMax = new Vector2(1, 0);
            barBgRect.pivot = new Vector2(0.5f, 0);
            barBgRect.sizeDelta = new Vector2(0, 24);
            barBgRect.anchoredPosition = new Vector2(0, 4);
            barBg.AddComponent<Image>().color = BarBgColor;

            CreateEdge(barBg.transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1f), new Vector2(0, 1), FrameGold);
            CreateEdge(barBg.transform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0f), new Vector2(0, 1), FrameGold);
            CreateEdge(barBg.transform, new Vector2(0, 0), new Vector2(0, 1),
                new Vector2(0f, 0.5f), new Vector2(1, 0), FrameGold);
            CreateEdge(barBg.transform, new Vector2(1, 0), new Vector2(1, 1),
                new Vector2(1f, 0.5f), new Vector2(1, 0), FrameGold);

            var inner = new GameObject("BarInner");
            inner.transform.SetParent(barBg.transform, false);
            var innerRect = inner.AddComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.offsetMin = new Vector2(2, 2);
            innerRect.offsetMax = new Vector2(-2, -2);

            var fill = new GameObject("Fill");
            fill.transform.SetParent(inner.transform, false);
            _fillRect = fill.AddComponent<RectTransform>();
            _fillRect.anchorMin = new Vector2(0, 0);
            _fillRect.anchorMax = new Vector2(1, 1);
            _fillRect.offsetMin = Vector2.zero;
            _fillRect.offsetMax = Vector2.zero;
            _fillRect.pivot = new Vector2(0, 0.5f);
            fill.AddComponent<Image>().color = BarFillColor;

            // Phase markers (60% and 30% thresholds)
            CreatePhaseMarker(inner.transform, 0.60f);
            CreatePhaseMarker(inner.transform, 0.30f);

            _root.SetActive(false);
        }

        private void CreatePhaseMarker(Transform parent, float fraction)
        {
            var marker = new GameObject("PhaseMarker");
            marker.transform.SetParent(parent, false);
            var rect = marker.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(fraction, 0);
            rect.anchorMax = new Vector2(fraction, 1);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(2, 0);
            rect.anchoredPosition = Vector2.zero;
            marker.AddComponent<Image>().color = new Color(FrameGold.r, FrameGold.g, FrameGold.b, 0.85f);
        }

        private void CreateEdge(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Color color) { UIBuilder.Edge(parent, anchorMin, anchorMax, pivot, sizeDelta, color); }
    }
}
