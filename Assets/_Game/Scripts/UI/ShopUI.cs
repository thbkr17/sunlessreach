using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;
using SunlessReach.Data;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class ShopUI : MonoBehaviour
    {
        [SerializeField] private GameState gameState;
        [SerializeField] private ShopItemData[] shopItems;

        Transform _root;
        RectTransform _framePanelRect;
        Transform _itemContainer;
        TextMeshProUGUI _goldDisplay;
        ItemView[] _itemViews;
        bool _wired;
        bool _isOpen;
        bool _justOpened;

        public bool IsOpen => _isOpen;
        InputAction _interactAction;

        class ItemView
        {
            public Button button;
            public Image background;
            public Image selectStripe;
            public TextMeshProUGUI nameLabel;
            public TextMeshProUGUI costLabel;
            public TextMeshProUGUI descLabel;
            public GameObject soldBadge;
        }

        void Awake()
        {
            _interactAction = new InputAction("ShopClose", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/rightShoulder");
            _interactAction.Enable();
        }

        void Start()
        {
            if (gameState == null)
            {
                var states = Resources.FindObjectsOfTypeAll<GameState>();
                if (states.Length > 0) gameState = states[0];
            }
            if (shopItems == null || shopItems.Length == 0)
                shopItems = Resources.LoadAll<ShopItemData>("ShopItems");
        }

        void EnsureWired()
        {
            if (_wired) return;
            _wired = true;
            _root = transform.Find("ShopOverlay");
            if (_root == null) return;
            _framePanelRect = (RectTransform)_root.Find("FramePanel");
            _itemContainer  = _framePanelRect.Find("ItemContainer");
            _goldDisplay    = _framePanelRect.Find("GoldPill/GoldText").GetComponent<TextMeshProUGUI>();

            BuildItemCards();
            ConfigureButtonNavigation();
            _root.gameObject.SetActive(false);
        }

        void BuildItemCards()
        {
            if (shopItems == null) shopItems = new ShopItemData[0];
            _itemViews = new ItemView[shopItems.Length];
            for (int i = 0; i < shopItems.Length; i++)
                _itemViews[i] = CreateItemCard(i, shopItems[i]);
        }

        ItemView CreateItemCard(int index, ShopItemData item)
        {
            var view = new ItemView();

            var card = new GameObject($"Item_{index}", typeof(RectTransform));
            card.transform.SetParent(_itemContainer, false);
            ((RectTransform)card.transform).sizeDelta = new Vector2(0, 90);
            view.background = card.AddComponent<Image>();
            view.background.color = UIBuilder.ItemBg;

            var btn = card.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            int captured = index;
            btn.onClick.AddListener(() => { UiSfx.PlayClick(); TryBuyItem(captured); });
            view.button = btn;

            var hairline = new Color(UIBuilder.FrameGold.r, UIBuilder.FrameGold.g, UIBuilder.FrameGold.b, 0.35f);
            UIBuilder.Edge(card.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 1), hairline);
            UIBuilder.Edge(card.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 1), hairline);

            var stripe = new GameObject("SelectStripe", typeof(RectTransform));
            stripe.transform.SetParent(card.transform, false);
            var sr = (RectTransform)stripe.transform;
            sr.anchorMin = new Vector2(0, 0); sr.anchorMax = new Vector2(0, 1);
            sr.pivot = new Vector2(0f, 0.5f); sr.sizeDelta = new Vector2(4, 0);
            view.selectStripe = stripe.AddComponent<Image>();
            view.selectStripe.color = UIBuilder.BrightGold;
            stripe.SetActive(false);

            view.nameLabel = MakeCardLabel(card.transform, "Name", item.itemName, 26, FontStyles.Bold, Color.white,
                new Vector2(0, 0.5f), new Vector2(0.7f, 1f), new Vector2(20, 0), new Vector2(0, -8), TextAlignmentOptions.MidlineLeft);
            view.costLabel = MakeCardLabel(card.transform, "Cost", "", 24, FontStyles.Bold, UIBuilder.BrightGold,
                new Vector2(0.6f, 0.5f), new Vector2(1f, 1f), new Vector2(0, 0), new Vector2(-20, -8), TextAlignmentOptions.MidlineRight);
            view.descLabel = MakeCardLabel(card.transform, "Desc", item.description, 16, FontStyles.Normal, UIBuilder.CreamText,
                new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(20, 8), new Vector2(-20, 0), TextAlignmentOptions.MidlineLeft);
            view.descLabel.enableWordWrapping = true;

            var sold = new GameObject("SoldBadge", typeof(RectTransform));
            sold.transform.SetParent(card.transform, false);
            var soldRect = (RectTransform)sold.transform;
            soldRect.anchorMin = new Vector2(1, 0.5f); soldRect.anchorMax = new Vector2(1, 1);
            soldRect.pivot = new Vector2(1, 0.5f);
            soldRect.anchoredPosition = new Vector2(-20, -22);
            soldRect.sizeDelta = new Vector2(86, 26);
            sold.AddComponent<Image>().color = new Color(0.65f, 0.25f, 0.20f, 1f);
            var soldLabel = MakeCardLabel(sold.transform, "SoldText", "SOLD", 16, FontStyles.Bold, Color.white,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center);
            soldLabel.characterSpacing = 4;
            sold.SetActive(false);
            view.soldBadge = sold;

            return view;
        }

        static TextMeshProUGUI MakeCardLabel(Transform parent, string name, string text, int size, FontStyles style, Color color,
            Vector2 aMin, Vector2 aMax, Vector2 offMin, Vector2 offMax, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.offsetMin = offMin; r.offsetMax = offMax;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.fontStyle = style;
            tmp.color = color; tmp.alignment = align;
            tmp.enableWordWrapping = false; tmp.richText = true;
            return tmp;
        }

        void ConfigureButtonNavigation()
        {
            if (_itemViews == null) return;
            for (int i = 0; i < _itemViews.Length; i++)
            {
                var btn = _itemViews[i]?.button;
                if (btn == null) continue;
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                nav.selectOnUp   = NextInteractable(i, -1);
                nav.selectOnDown = NextInteractable(i, +1);
                btn.navigation = nav;
            }
        }

        Button NextInteractable(int from, int step)
        {
            for (int i = from + step; i >= 0 && i < _itemViews.Length; i += step)
            {
                var b = _itemViews[i]?.button;
                if (b != null && b.interactable) return b;
            }
            return null;
        }

        void EnsureValidSelection()
        {
            if (EventSystem.current == null || _itemViews == null) return;
            var cur = EventSystem.current.currentSelectedGameObject;
            for (int i = 0; i < _itemViews.Length; i++)
            {
                var b = _itemViews[i]?.button;
                if (b != null && b.gameObject == cur && b.interactable) return;
            }
            for (int i = 0; i < _itemViews.Length; i++)
            {
                var b = _itemViews[i]?.button;
                if (b != null && b.interactable) { EventSystem.current.SetSelectedGameObject(b.gameObject); return; }
            }
            EventSystem.current.SetSelectedGameObject(null);
        }

        void Update()
        {
            if (!_isOpen) return;
            UpdateSelection();
            if (_justOpened) { _justOpened = false; return; }
            if (_interactAction.WasPressedThisFrame()) CloseShop();
        }

        void UpdateSelection()
        {
            if (_itemViews == null) return;
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);

            for (int i = 0; i < _itemViews.Length; i++)
            {
                var v = _itemViews[i];
                if (v == null || v.button == null) continue;
                bool soldOut = v.soldBadge != null && v.soldBadge.activeSelf;
                bool isSel = !soldOut && selected == v.button.gameObject;

                if (v.selectStripe != null)
                {
                    if (v.selectStripe.gameObject.activeSelf != isSel) v.selectStripe.gameObject.SetActive(isSel);
                    if (isSel) v.selectStripe.color = new Color(UIBuilder.BrightGold.r, UIBuilder.BrightGold.g, UIBuilder.BrightGold.b, pulse);
                }

                var dimmed = new Color(0.55f, 0.50f, 0.42f, 1f);
                if (soldOut)     { v.background.color = UIBuilder.ItemBg;         v.nameLabel.color = dimmed;             v.descLabel.color = dimmed; }
                else if (isSel)  { v.background.color = UIBuilder.ItemBgSelected; v.nameLabel.color = UIBuilder.BrightGold; v.descLabel.color = UIBuilder.CreamText; }
                else             { v.background.color = UIBuilder.ItemBg;         v.nameLabel.color = Color.white;         v.descLabel.color = UIBuilder.CreamText; }
            }
        }

        void OnDestroy()
        {
            _interactAction?.Disable();
            _interactAction?.Dispose();
        }

        public void OpenShop()
        {
            if (_isOpen) return;
            EnsureWired();
            if (_root == null) return;
            _isOpen = true;
            _justOpened = true;
            _root.gameObject.SetActive(true);
            Time.timeScale = 0;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)_itemContainer);
            RefreshDisplay();
            SelectFirstButton();
            EventBus.RaiseShopOpened();
            StopAllCoroutines();
            StartCoroutine(AnimateOpen());
        }

        public void CloseShop()
        {
            if (!_isOpen) return;
            _isOpen = false;
            if (_root != null) _root.gameObject.SetActive(false);
            Time.timeScale = 1;
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            EventBus.RaiseShopClosed();
        }

        IEnumerator AnimateOpen()
        {
            if (_framePanelRect == null) yield break;
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                float s = Mathf.Lerp(0.92f, 1.0f, t);
                _framePanelRect.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            _framePanelRect.localScale = Vector3.one;
        }

        void SelectFirstButton()
        {
            if (EventSystem.current == null || _itemViews == null) return;
            for (int i = 0; i < _itemViews.Length; i++)
            {
                var btn = _itemViews[i]?.button;
                if (btn != null && btn.interactable)
                {
                    EventSystem.current.SetSelectedGameObject(btn.gameObject);
                    return;
                }
            }
        }

        public void TryBuyItem(int index)
        {
            if (index < 0 || index >= shopItems.Length) return;
            var item = shopItems[index];

            switch (item.effectType)
            {
                case ShopItemEffect.AddMaxHeart:
                    if (gameState.heartShardsOwned >= item.maxPurchases) return;
                    if (!gameState.TrySpendGold(item.cost)) return;
                    gameState.maxHearts += item.effectAmount;
                    gameState.currentHearts += item.effectAmount;
                    gameState.heartShardsOwned++;
                    EventBus.RaiseHealthChanged(gameState.currentHearts, gameState.maxHearts);
                    break;
                case ShopItemEffect.AddAttackDamage:
                    if (gameState.hasSharpenedBlade) return;
                    if (!gameState.TrySpendGold(item.cost)) return;
                    gameState.attackDamage += item.effectAmount;
                    gameState.hasSharpenedBlade = true;
                    break;
                case ShopItemEffect.AddMaxSouls:
                    if (gameState.hasSoulVessel) return;
                    if (!gameState.TrySpendGold(item.cost)) return;
                    gameState.maxSouls += item.effectAmount;
                    gameState.hasSoulVessel = true;
                    EventBus.RaiseSoulsChanged(gameState.currentSouls, gameState.maxSouls);
                    break;
            }
            RefreshDisplay();
        }

        void RefreshDisplay()
        {
            if (_goldDisplay != null && gameState != null)
                _goldDisplay.text = gameState.currentGold.ToString();

            if (_itemViews == null) return;
            for (int i = 0; i < _itemViews.Length; i++)
            {
                var v = _itemViews[i];
                if (v == null || i >= shopItems.Length) continue;
                var item = shopItems[i];

                bool soldOut = false;
                string countSuffix = "";

                if (item.effectType == ShopItemEffect.AddMaxHeart)
                {
                    countSuffix = $"  <size=18><color=#9C8B70>{gameState.heartShardsOwned}/{item.maxPurchases}</color></size>";
                    soldOut = gameState.heartShardsOwned >= item.maxPurchases;
                }
                else if (item.effectType == ShopItemEffect.AddAttackDamage && gameState.hasSharpenedBlade) soldOut = true;
                else if (item.effectType == ShopItemEffect.AddMaxSouls && gameState.hasSoulVessel)         soldOut = true;

                v.nameLabel.text = item.itemName + countSuffix;
                v.descLabel.text = item.description;
                v.costLabel.text = soldOut ? "" : $"{item.cost}<color=#FFD166>g</color>";
                v.soldBadge.SetActive(soldOut);
                v.button.interactable = !soldOut;
            }
            ConfigureButtonNavigation();
            EnsureValidSelection();
        }
    }
}
