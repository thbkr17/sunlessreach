using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SunlessReach.Core;
using SunlessReach.Data;

namespace SunlessReach.UI
{
    public class MoneyCounter : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI moneyText;   // old label, kept hidden
        [SerializeField] private GameState gameState;
        [SerializeField] private float iconSize = 30f;

        private static readonly Color CoinGold = new Color(1.00f, 0.82f, 0.26f, 1f);
        private static readonly Color CoinRim  = new Color(0.55f, 0.38f, 0.06f, 1f);
        private static Sprite _coinSprite;

        private TextMeshProUGUI _amountLabel;
        private bool _built;

        private void Awake()
        {
            if (gameState == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameState>();
                if (all.Length > 0) gameState = all[0];
            }
            // moneyText sits on this same GameObject - disable the component, not the object.
            if (moneyText != null) moneyText.enabled = false;
        }

        private void Start()
        {
            EnsureUI();
            if (gameState != null) UpdateDisplay(gameState.currentGold);
        }

        private void OnEnable()  { EventBus.OnMoneyChanged += UpdateDisplay; }
        private void OnDisable() { EventBus.OnMoneyChanged -= UpdateDisplay; }

        private void UpdateDisplay(int total)
        {
            EnsureUI();
            if (_amountLabel != null) _amountLabel.text = total.ToString();
        }

        private void EnsureUI()
        {
            if (_built) return;
            _built = true;

            // Sit just to the right of the soul orb (which is anchored top-left of the HUD).
            var rt = GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot     = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(108f, -88f);
                rt.sizeDelta = new Vector2(170f, 40f);
            }

            var row = new GameObject("MoneyRow");
            row.transform.SetParent(transform, false);
            var rowRect = row.AddComponent<RectTransform>();
            rowRect.anchorMin = Vector2.zero; rowRect.anchorMax = Vector2.one;
            rowRect.offsetMin = Vector2.zero; rowRect.offsetMax = Vector2.zero;
            var layout = row.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var iconGo = new GameObject("CoinIcon");
            iconGo.transform.SetParent(row.transform, false);
            var irect = iconGo.AddComponent<RectTransform>();
            irect.sizeDelta = new Vector2(iconSize, iconSize);
            var ile = iconGo.AddComponent<LayoutElement>();
            ile.preferredWidth = iconSize; ile.preferredHeight = iconSize;
            var iimg = iconGo.AddComponent<Image>();
            iimg.sprite = GetCoinSprite();
            iimg.raycastTarget = false;
            iimg.preserveAspect = true;

            var lblGo = new GameObject("Amount");
            lblGo.transform.SetParent(row.transform, false);
            var lrect = lblGo.AddComponent<RectTransform>();
            lrect.sizeDelta = new Vector2(120f, iconSize);
            var lle = lblGo.AddComponent<LayoutElement>();
            lle.preferredWidth = 120f; lle.preferredHeight = iconSize;
            _amountLabel = lblGo.AddComponent<TextMeshProUGUI>();
            _amountLabel.text = "0";
            _amountLabel.fontSize = 26f;
            _amountLabel.fontStyle = FontStyles.Bold;
            _amountLabel.color = new Color(0.95f, 0.90f, 0.78f, 1f);
            _amountLabel.alignment = TextAlignmentOptions.MidlineLeft;
            _amountLabel.enableWordWrapping = false;
            _amountLabel.raycastTarget = false;
        }

        private static Sprite GetCoinSprite()
        {
            if (_coinSprite != null) return _coinSprite;
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var cols = new Color[size * size];
            Vector2 c = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / r;   // 0 centre .. 1 edge
                Color col;
                if (d > 1f) col = new Color(0f, 0f, 0f, 0f);
                else if (d > 0.82f) col = CoinRim;
                else
                {
                    float shade = Mathf.Lerp(1.08f, 0.78f, (float)y / (size - 1));   // soft top-light
                    col = new Color(Mathf.Clamp01(CoinGold.r * shade), Mathf.Clamp01(CoinGold.g * shade), Mathf.Clamp01(CoinGold.b * shade), 1f);
                }
                cols[y * size + x] = col;
            }
            tex.SetPixels(cols);
            tex.Apply(false, false);
            _coinSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _coinSprite;
        }
    }
}
