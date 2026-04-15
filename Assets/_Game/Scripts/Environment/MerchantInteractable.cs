using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using SunlessReach.UI;

namespace SunlessReach.Environment
{
    public class MerchantInteractable : MonoBehaviour
    {
        [SerializeField] private ShopUI shopUI;
        [SerializeField] private GameObject interactPrompt;
        [SerializeField] private float interactRange = 3f;

        private Transform _player;
        private bool _playerInRange;
        private InputAction _interactAction;
        private GameObject _promptGo;   // the assigned prompt, or one we build at runtime

        private void Awake()
        {
            _interactAction = new InputAction("Interact", InputActionType.Button);
            _interactAction.AddBinding("<Keyboard>/e");
            _interactAction.AddBinding("<Gamepad>/rightShoulder");
            _interactAction.Enable();
        }

        private void Start()
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                _player = playerObj.transform;

            if (shopUI == null)
                shopUI = FindAnyObjectByType<ShopUI>();
            if (shopUI == null)
                CreateRuntimeShopUI();

            _promptGo = interactPrompt != null ? interactPrompt : BuildPrompt();
            if (_promptGo != null) _promptGo.SetActive(false);
        }

        private void CreateRuntimeShopUI()
        {
            Canvas canvas = FindHudCanvas();
            if (canvas == null) return;
            var shopObj = new GameObject("ShopUI");
            shopObj.transform.SetParent(canvas.transform, false);
            shopUI = shopObj.AddComponent<ShopUI>();
        }

        private static Canvas FindHudCanvas()
        {
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.gameObject.name == "HUDCanvas") return c;
            return FindAnyObjectByType<Canvas>();
        }

        // "Press E / RB to trade" pill, shown while in range.
        private GameObject BuildPrompt()
        {
            var canvas = FindHudCanvas();
            if (canvas == null) return null;

            var go = new GameObject("MerchantPrompt");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 110f);
            rt.sizeDelta = new Vector2(500f, 48f);
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.04f, 0.07f, 0.7f);
            bg.raycastTarget = false;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lrt = lblGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.text = "Press   <sprite name=\"e\">  /  <sprite name=\"rb\">   to trade";
            lbl.fontSize = 24f;
            lbl.color = new Color(0.92f, 0.86f, 0.74f, 1f);
            lbl.richText = true;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            return go;
        }

        private void Update()
        {
            if (_player == null)
            {
                var playerObj = GameObject.FindGameObjectWithTag("Player");
                if (playerObj != null) _player = playerObj.transform;
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            bool wasInRange = _playerInRange;
            _playerInRange = dist <= interactRange;

            if (_promptGo != null && _playerInRange != wasInRange)
                _promptGo.SetActive(_playerInRange);

            if (!_playerInRange) return;

            if (_interactAction.WasPressedThisFrame() && shopUI != null)
            {
                if (_promptGo != null) _promptGo.SetActive(false);
                shopUI.OpenShop();
            }
        }

        private void OnDestroy()
        {
            _interactAction?.Disable();
            _interactAction?.Dispose();
        }
    }
}
