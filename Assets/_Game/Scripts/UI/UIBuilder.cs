using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SunlessReach.UI
{
    public static class UIBuilder
    {
        public static readonly Color Overlay         = new Color(0.04f, 0.05f, 0.08f, 1f);
        public static readonly Color OverlayDim      = new Color(0.04f, 0.05f, 0.08f, 0.82f);
        public static readonly Color PanelInterior   = new Color(0.07f, 0.09f, 0.13f, 1f);
        public static readonly Color FrameGold       = new Color(0.78f, 0.62f, 0.28f, 1f);
        public static readonly Color BrightGold      = new Color(1.00f, 0.82f, 0.40f, 1f);
        public static readonly Color CreamText       = new Color(0.92f, 0.86f, 0.74f, 1f);
        public static readonly Color ItemBg          = new Color(0.10f, 0.12f, 0.16f, 0.85f);
        public static readonly Color ItemBgSelected  = new Color(0.16f, 0.20f, 0.26f, 1f);

        public static GameObject FullScreenOverlay(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return go;
        }

        public static RectTransform GoldFramedPanel(Transform parent, Vector2 size, out Transform frameTransform)
        {
            var frame = new GameObject("FramePanel");
            frame.transform.SetParent(parent, false);
            var rect = frame.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            frame.AddComponent<Image>().color = PanelInterior;

            Edge(frame.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 2), FrameGold);
            Edge(frame.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 2), FrameGold);
            Edge(frame.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(2, 0), FrameGold);
            Edge(frame.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(2, 0), FrameGold);

            Diamond(frame.transform, new Vector2(0, 1), new Vector2(16, -16), 14, BrightGold);
            Diamond(frame.transform, new Vector2(1, 1), new Vector2(-16, -16), 14, BrightGold);
            Diamond(frame.transform, new Vector2(0, 0), new Vector2(16, 16), 14, BrightGold);
            Diamond(frame.transform, new Vector2(1, 0), new Vector2(-16, 16), 14, BrightGold);

            frameTransform = frame.transform;
            return rect;
        }

        public static void Edge(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
                                Vector2 pivot, Vector2 size, Color color)
        {
            var go = new GameObject("Edge");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        public static void TitleRule(Transform frame, Vector2 anchoredPos, float width)
        {
            var rule = new GameObject("TitleRule");
            rule.transform.SetParent(frame, false);
            var rect = rule.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 1);
            rect.anchoredPosition = anchoredPos;
            rule.AddComponent<Image>().color = FrameGold;

            Diamond(frame, new Vector2(0.5f, 1f), anchoredPos, 12, BrightGold);
        }

        public static Image Diamond(Transform parent, Vector2 anchor, Vector2 anchoredPos, float size, Color color)
        {
            var go = new GameObject("Diamond");
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = anchoredPos;
            rect.localRotation = Quaternion.Euler(0, 0, 45);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        public static GameObject Label(Transform parent, string name, string text, int fontSize,
                                       Vector2 anchorMin, Vector2 anchorMax,
                                       Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPos;
            rect.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.richText = true;
            return go;
        }
    }
}
