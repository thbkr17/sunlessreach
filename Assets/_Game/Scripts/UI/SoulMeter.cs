using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class SoulMeter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI soulText;        // old label, kept hidden
        [SerializeField] private Sprite globeMaskSprite;          // ZombiSoft GlobeMask.png
        [SerializeField] private Sprite liquidSprite;             // ZombiSoft ManaGlobe_0
        [SerializeField] private Sprite glassSprite;              // ZombiSoft Glass
        [SerializeField] private float orbSize = 72f;

        private static readonly Color SoulColor     = new Color(0.32f, 0.72f, 1.00f, 0.95f);
        private static readonly Color InteriorColor = new Color(0.03f, 0.06f, 0.13f, 1f);
        private static readonly Color GlassColor    = new Color(1f, 1f, 1f, 0.85f);

        private RectTransform _liquidRect;
        private TextMeshProUGUI _countLabel;
        private float _displayed, _target;
        private bool _built;

        private void Awake()
        {
            // soulText lives on this same GameObject - disable the component, not the object,
            // or we'd switch ourselves off.
            if (soulText != null) soulText.enabled = false;
        }

        private void Start()
        {
            EnsureUI();
        }

        private void OnEnable()  { EventBus.OnSoulsChanged += UpdateDisplay; }
        private void OnDisable() { EventBus.OnSoulsChanged -= UpdateDisplay; }

        private void UpdateDisplay(int current, int max)
        {
            EnsureUI();
            _target = max <= 0 ? 0f : Mathf.Clamp01((float)current / max);
            if (_countLabel != null) _countLabel.text = current.ToString();
        }

        private void Update()
        {
            if (_liquidRect == null) return;
            _displayed = Mathf.MoveTowards(_displayed, _target, Time.deltaTime * 1.5f);
            _liquidRect.anchoredPosition = new Vector2(0f, -orbSize * (1f - _displayed));
        }

        private void EnsureUI()
        {
            if (_built) return;
            _built = true;

            var orb = new GameObject("SoulOrb");
            orb.transform.SetParent(transform, false);
            var orbRect = orb.AddComponent<RectTransform>();
            orbRect.anchorMin = new Vector2(0, 1);
            orbRect.anchorMax = new Vector2(0, 1);
            orbRect.pivot     = new Vector2(0, 1);
            orbRect.anchoredPosition = Vector2.zero;
            orbRect.sizeDelta = new Vector2(orbSize, orbSize);

            // Dark interior (visible through the empty part of the orb)
            MakeImage("Interior", orb.transform, globeMaskSprite, InteriorColor);

            // Circular mask - clips the liquid to the orb shape
            var maskGo = new GameObject("Mask");
            maskGo.transform.SetParent(orb.transform, false);
            Stretch(maskGo.AddComponent<RectTransform>());
            var mimg = maskGo.AddComponent<Image>();
            mimg.sprite = globeMaskSprite;
            mimg.color = Color.white;
            mimg.raycastTarget = false;
            var mask = maskGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            // Liquid - slides up/down inside the mask
            var liquid = MakeImage("Liquid", maskGo.transform, liquidSprite != null ? liquidSprite : globeMaskSprite, SoulColor);
            _liquidRect = liquid.rectTransform;

            var glass = MakeImage("Glass", orb.transform, glassSprite, GlassColor);
            if (glass.sprite == null) glass.enabled = false;

            var lblGo = new GameObject("Count");
            lblGo.transform.SetParent(orb.transform, false);
            Stretch(lblGo.AddComponent<RectTransform>());
            _countLabel = lblGo.AddComponent<TextMeshProUGUI>();
            _countLabel.text = "0";
            _countLabel.fontSize = orbSize * 0.40f;
            _countLabel.fontStyle = FontStyles.Bold;
            _countLabel.color = Color.white;
            _countLabel.alignment = TextAlignmentOptions.Center;
            _countLabel.raycastTarget = false;
            _countLabel.enableWordWrapping = false;

            _displayed = 0f;
            _liquidRect.anchoredPosition = new Vector2(0f, -orbSize);
        }

        private static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Stretch(go.AddComponent<RectTransform>());
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            return img;
        }

        private static void Stretch(RectTransform r)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
        }
    }
}
