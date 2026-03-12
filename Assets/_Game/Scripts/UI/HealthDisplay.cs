using UnityEngine;
using UnityEngine.UI;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class HealthDisplay : MonoBehaviour
    {
        [SerializeField] private Transform heartContainer;
        [SerializeField] private Sprite heartFillSprite;     // HeartSystem/Sprites/Fill.png
        [SerializeField] private Sprite heartOutlineSprite;  // HeartSystem/Sprites/Outline.png
        [SerializeField] private float heartSize = 40f;
        [SerializeField] private float heartSpacing = 6f;

        private static readonly Color FillColor    = new Color(0.85f, 0.15f, 0.18f, 1f);
        private static readonly Color EmptyFill     = new Color(0.20f, 0.05f, 0.06f, 0.55f);
        private static readonly Color OutlineColor = new Color(0.05f, 0.04f, 0.05f, 1f);

        private Image[] _fillImages;

        private void OnEnable()  { EventBus.OnHealthChanged += UpdateDisplay; }
        private void OnDisable() { EventBus.OnHealthChanged -= UpdateDisplay; }

        private void UpdateDisplay(int current, int max)
        {
            if (_fillImages == null || _fillImages.Length != max)
                BuildHearts(max);

            for (int i = 0; i < _fillImages.Length; i++)
            {
                if (_fillImages[i] != null)
                    _fillImages[i].color = i < current ? FillColor : EmptyFill;
            }
        }

        private void BuildHearts(int count)
        {
            if (heartContainer == null) heartContainer = transform;

            // Make sure the container lays the hearts out in a row.
            var layout = heartContainer.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = heartContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = heartSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            for (int i = heartContainer.childCount - 1; i >= 0; i--)
                Destroy(heartContainer.GetChild(i).gameObject);

            count = Mathf.Max(0, count);
            _fillImages = new Image[count];
            for (int i = 0; i < count; i++)
                _fillImages[i] = CreateHeart(heartContainer, i);
        }

        private Image CreateHeart(Transform parent, int index)
        {
            var heart = new GameObject($"Heart_{index}");
            heart.transform.SetParent(parent, false);
            var hrect = heart.AddComponent<RectTransform>();
            hrect.sizeDelta = new Vector2(heartSize, heartSize);
            // Pixel-art sprites are tiny, so just let the LayoutElement report the size.
            var le = heart.AddComponent<LayoutElement>();
            le.preferredWidth = heartSize;
            le.preferredHeight = heartSize;

            // Outline (behind), always visible - shows the "slot" even when empty.
            var outlineGo = new GameObject("Outline");
            outlineGo.transform.SetParent(heart.transform, false);
            var orect = outlineGo.AddComponent<RectTransform>();
            orect.anchorMin = Vector2.zero; orect.anchorMax = Vector2.one;
            orect.offsetMin = Vector2.zero; orect.offsetMax = Vector2.zero;
            var oimg = outlineGo.AddComponent<Image>();
            oimg.sprite = heartOutlineSprite;
            oimg.color = OutlineColor;
            oimg.preserveAspect = true;
            oimg.raycastTarget = false;
            // If no outline sprite, fall back to the fill silhouette so something shows.
            if (oimg.sprite == null) oimg.sprite = heartFillSprite;

            // Fill (front), tinted red/dark depending on health.
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(heart.transform, false);
            var frect = fillGo.AddComponent<RectTransform>();
            frect.anchorMin = Vector2.zero; frect.anchorMax = Vector2.one;
            frect.offsetMin = Vector2.zero; frect.offsetMax = Vector2.zero;
            var fimg = fillGo.AddComponent<Image>();
            fimg.sprite = heartFillSprite;
            fimg.color = FillColor;
            fimg.preserveAspect = true;
            fimg.raycastTarget = false;

            return fimg;
        }
    }
}
