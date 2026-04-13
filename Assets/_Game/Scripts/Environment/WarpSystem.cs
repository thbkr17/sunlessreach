using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using SunlessReach.Core;
using SunlessReach.Data;

namespace SunlessReach.Environment
{
    // Campfire rest / fast-travel: set your respawn point, or warp to a campfire you've visited.
    public class WarpSystem : MonoBehaviour
    {
        private struct WarpPoint { public string id; public string label; public Vector3 pos; public bool isCampfire; }

        private static readonly WarpPoint[] Warps =
        {
            new WarpPoint { id = "spawn",     label = "Spawn Campfire",      pos = new Vector3(0f,      2.5f, 0f), isCampfire = true  },
            new WarpPoint { id = "cave_end",  label = "Cave's End Campfire", pos = new Vector3(320f,    21f,  0f), isCampfire = true  },
            new WarpPoint { id = "merchant",  label = "Merchant's Campfire",  pos = new Vector3(-182f,   17f,  0f), isCampfire = true  },
            new WarpPoint { id = "swamp_end", label = "Swamp's End Campfire",pos = new Vector3(-294.5f, 8.5f, 0f), isCampfire = true  },
        };
        private const float UnlockRange   = 5f;
        private const float InteractRange = 3.5f;

        private static readonly Color OverlayColor   = new Color(0.03f, 0.04f, 0.07f, 0.82f);
        private static readonly Color PanelInterior  = new Color(0.07f, 0.09f, 0.13f, 1f);
        private static readonly Color FrameGold      = new Color(0.78f, 0.62f, 0.28f, 1f);
        private static readonly Color BrightGold     = new Color(1.00f, 0.82f, 0.40f, 1f);
        private static readonly Color CreamText      = new Color(0.92f, 0.86f, 0.74f, 1f);
        private static readonly Color ItemBg         = new Color(0.10f, 0.12f, 0.16f, 0.85f);
        private static readonly Color ItemBgSelected = new Color(0.16f, 0.20f, 0.26f, 1f);

        private GameState _gameState;
        private Transform _player;
        private InputAction _interact;

        private GameObject _menuRoot, _promptRoot;
        private RectTransform _buttonsRect, _frameRect;
        private TextMeshProUGUI _titleLabel;
        private bool _menuOpen;
        private readonly List<Transform> _campfires = new();   // every Checkpoint in the scene

        private class BtnView { public Button button; public Image bg; public Image stripe; public TextMeshProUGUI label; }
        private readonly List<BtnView> _btns = new();

        private void Awake()
        {
            _interact = new InputAction("WarpInteract", InputActionType.Button);
            _interact.AddBinding("<Keyboard>/e");
            _interact.AddBinding("<Gamepad>/rightShoulder");
            _interact.Enable();
        }

        private void OnDestroy()
        {
            _interact?.Disable();
            _interact?.Dispose();
        }

        private GameState GS()
        {
            if (_gameState != null) return _gameState;
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) _gameState = gm.GameState;
            if (_gameState == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameState>();
                if (all.Length > 0) _gameState = all[0];
            }
            return _gameState;
        }

        private void Update()
        {
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p == null) return;
                _player = p.transform;
                RefreshCampfires();
            }
            var gs = GS();
            if (gs == null) return;
            if (gs.unlockedWarps == null) gs.unlockedWarps = new List<string>();
            if (!gs.unlockedWarps.Contains("spawn")) gs.unlockedWarps.Add("spawn");

            // Debug teleport hotkeys.
            if (Keyboard.current != null)
            {
                if (Keyboard.current.mKey.wasPressedThisFrame) DebugTeleport(new Vector3(-198f, 113f, 0f));      // just outside the boss arena
                if (Keyboard.current.nKey.wasPressedThisFrame) DebugTeleport(new Vector3(320f, 21f, 0f));        // cave's end (near the dash pickup)
                if (Keyboard.current.bKey.wasPressedThisFrame) DebugTeleport(new Vector3(-294.5f, 8.5f, 0f));    // swamp's end (near the double-jump pickup)
            }

            if (_menuOpen)
            {
                UpdateSelection();
                // Pressing Interact again closes it (Escape is left to the pause menu).
                if (_interact.WasPressedThisFrame())
                    CloseMenu();
                return;
            }

            // Unlock the fast-travel destinations the player has physically reached.
            for (int i = 0; i < Warps.Length; i++)
                if (Vector3.Distance(_player.position, Warps[i].pos) <= UnlockRange)
                    gs.UnlockWarp(Warps[i].id);

            // Nearest campfire (a fast-travel hub or any scene Checkpoint). warpIndex >= 0 means it's a hub.
            float best = InteractRange;
            int nearWarp = -1;
            Vector3 nearPos = Vector3.zero;
            string nearTitle = "REST";
            bool found = false;

            for (int i = 0; i < Warps.Length; i++)
            {
                if (!Warps[i].isCampfire) continue;
                float d = Vector3.Distance(_player.position, Warps[i].pos);
                if (d <= best) { best = d; nearWarp = i; nearPos = Warps[i].pos; nearTitle = Warps[i].label; found = true; }
            }
            for (int i = 0; i < _campfires.Count; i++)
            {
                var t = _campfires[i];
                if (t == null) continue;
                float d = Vector3.Distance(_player.position, t.position);
                if (d <= best) { best = d; nearWarp = -1; nearPos = t.position; nearTitle = "REST"; found = true; }
            }

            ShowPrompt(found);
            if (found && _interact.WasPressedThisFrame())
                OpenMenu(nearPos, nearWarp, nearTitle);
        }

        private void DebugTeleport(Vector3 dest)
        {
            if (_player == null) return;
            _player.position = dest;
            var rb = _player.GetComponent<Rigidbody>();
            if (rb != null) { rb.position = dest; rb.linearVelocity = Vector3.zero; }
        }

        private void RefreshCampfires()
        {
            _campfires.Clear();
            foreach (var cp in FindObjectsByType<Checkpoint>(FindObjectsSortMode.None))
                if (cp != null) _campfires.Add(cp.transform);
        }



        private void OpenMenu(Vector3 herePos, int warpIndex, string title)
        {
            EnsureUI();
            ShowPrompt(false);

            _titleLabel.text = title.ToUpper();

            foreach (var b in _btns) if (b.button != null) Destroy(b.button.gameObject);
            _btns.Clear();

            AddButton("Set Spawn Here", () => SetSpawn(herePos, title));

            var gs = GS();
            const float selfExcludeRange = 8f;
            for (int i = 0; i < Warps.Length; i++)
            {
                if (i == warpIndex) continue;
                // Don't list the hub we're standing at.
                if (Vector3.Distance(Warps[i].pos, herePos) < selfExcludeRange) continue;
                if (gs != null && !gs.IsWarpUnlocked(Warps[i].id)) continue;
                int target = i;
                AddButton($"Travel to {Warps[i].label}", () => TravelTo(target));
            }

            AddButton("Leave", CloseMenu);

            for (int i = 0; i < _btns.Count; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0) nav.selectOnUp = _btns[i - 1].button;
                if (i < _btns.Count - 1) nav.selectOnDown = _btns[i + 1].button;
                _btns[i].button.navigation = nav;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonsRect);
            float h = 150f + _btns.Count * 70f;
            _frameRect.sizeDelta = new Vector2(560f, Mathf.Clamp(h, 280f, 700f));

            _menuRoot.SetActive(true);
            _menuOpen = true;
            SetPlayerControlEnabled(false);
            Time.timeScale = 0f;
            EventBus.RaiseGamePaused();
            if (_btns.Count > 0) SelectButton(_btns[0].button);
        }

        private void CloseMenu()
        {
            StopAllCoroutines();
            _menuOpen = false;
            if (_menuRoot != null) _menuRoot.SetActive(false);
            Time.timeScale = 1f;
            // Drop any velocity that built up while the menu was open, then hand control back.
            if (_player != null)
            {
                var rb = _player.GetComponent<Rigidbody>();
                if (rb != null) rb.linearVelocity = Vector3.zero;
            }
            SetPlayerControlEnabled(true);
            EventBus.RaiseGameResumed();
        }

        private void SetPlayerControlEnabled(bool enabled)
        {
            if (_player == null) return;
            var input = _player.GetComponent<Player.PlayerInputHandler>();
            if (input != null) input.enabled = enabled;
        }

        private void SetSpawn(Vector3 pos, string title)
        {
            var gs = GS();
            if (gs != null) gs.lastCheckpointPosition = pos;
            // Confirmation only - leave the menu open so the player can also fast-travel.
            if (_titleLabel != null) _titleLabel.text = "SPAWN SET HERE";
            StopAllCoroutines();
            StartCoroutine(RestoreTitle(1.2f, title.ToUpper()));
        }

        private IEnumerator RestoreTitle(float seconds, string text)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
            if (_menuOpen && _titleLabel != null) _titleLabel.text = text;
        }

        private void TravelTo(int index)
        {
            CloseMenu();
            if (_player == null) return;
            Vector3 dest = Warps[index].pos;
            _player.position = dest;
            var rb = _player.GetComponent<Rigidbody>();
            if (rb != null) { rb.position = dest; rb.linearVelocity = Vector3.zero; }
            var gs = GS();
            if (gs != null) gs.UnlockWarp(Warps[index].id);
        }



        private void ShowPrompt(bool show)
        {
            if (show && _promptRoot == null) EnsureUI();
            if (_promptRoot != null && _promptRoot.activeSelf != show) _promptRoot.SetActive(show);
        }

        private void AddButton(string label, System.Action onClick)
        {
            var v = new BtnView();
            var go = new GameObject($"WarpBtn_{label}");
            go.transform.SetParent(_buttonsRect, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 56);
            v.bg = go.AddComponent<Image>();
            v.bg.color = ItemBg;
            var btn = go.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => { UiSfx.PlayClick(); onClick(); });
            v.button = btn;

            var stripeGo = new GameObject("Stripe");
            stripeGo.transform.SetParent(go.transform, false);
            var srect = stripeGo.AddComponent<RectTransform>();
            srect.anchorMin = new Vector2(0, 0); srect.anchorMax = new Vector2(0, 1);
            srect.pivot = new Vector2(0, 0.5f); srect.sizeDelta = new Vector2(4, 0); srect.anchoredPosition = Vector2.zero;
            v.stripe = stripeGo.AddComponent<Image>();
            v.stripe.color = BrightGold;
            stripeGo.SetActive(false);

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lrect = lblGo.AddComponent<RectTransform>();
            lrect.anchorMin = Vector2.zero; lrect.anchorMax = Vector2.one;
            lrect.offsetMin = new Vector2(20, 0); lrect.offsetMax = new Vector2(-20, 0);
            v.label = lblGo.AddComponent<TextMeshProUGUI>();
            v.label.text = label;
            v.label.fontSize = 24;
            v.label.fontStyle = FontStyles.Bold;
            v.label.color = Color.white;
            v.label.alignment = TextAlignmentOptions.Center;
            v.label.enableWordWrapping = false;

            _btns.Add(v);
        }

        private void UpdateSelection()
        {
            var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var v in _btns)
            {
                if (v == null || v.button == null) continue;
                bool isSel = sel == v.button.gameObject;
                if (v.stripe != null)
                {
                    if (v.stripe.gameObject.activeSelf != isSel) v.stripe.gameObject.SetActive(isSel);
                    if (isSel) v.stripe.color = new Color(BrightGold.r, BrightGold.g, BrightGold.b, pulse);
                }
                if (isSel) { v.bg.color = ItemBgSelected; v.label.color = BrightGold; }
                else       { v.bg.color = ItemBg;         v.label.color = Color.white; }
            }
        }

        private void SelectButton(Button btn)
        {
            if (EventSystem.current != null && btn != null)
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }

        private void EnsureUI()
        {
            if (_menuRoot != null) return;

            Canvas canvas = null;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if (c.gameObject.name == "HUDCanvas") { canvas = c; break; }
            if (canvas == null) canvas = FindAnyObjectByType<Canvas>();
            Transform parent;
            if (canvas != null) parent = canvas.transform;
            else
            {
                var cobj = new GameObject("WarpCanvas");
                var c = cobj.AddComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                c.sortingOrder = 350;
                cobj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                cobj.AddComponent<GraphicRaycaster>();
                parent = cobj.transform;
            }


            _promptRoot = new GameObject("WarpPrompt");
            _promptRoot.transform.SetParent(parent, false);
            var prect = _promptRoot.AddComponent<RectTransform>();
            prect.anchorMin = new Vector2(0.5f, 0f); prect.anchorMax = new Vector2(0.5f, 0f);
            prect.pivot = new Vector2(0.5f, 0f);
            prect.anchoredPosition = new Vector2(0, 110);
            prect.sizeDelta = new Vector2(480, 48);
            var pbg = _promptRoot.AddComponent<Image>();
            pbg.color = new Color(0.03f, 0.04f, 0.07f, 0.7f);
            pbg.raycastTarget = false;
            var plblGo = new GameObject("Label");
            plblGo.transform.SetParent(_promptRoot.transform, false);
            var plrect = plblGo.AddComponent<RectTransform>();
            plrect.anchorMin = Vector2.zero; plrect.anchorMax = Vector2.one;
            plrect.offsetMin = Vector2.zero; plrect.offsetMax = Vector2.zero;
            var plbl = plblGo.AddComponent<TextMeshProUGUI>();
            plbl.text = "Press   <sprite name=\"e\">  /  <sprite name=\"rb\">   to rest";
            plbl.fontSize = 24;
            plbl.color = CreamText;
            plbl.richText = true;
            plbl.alignment = TextAlignmentOptions.Center;
            plbl.raycastTarget = false;
            _promptRoot.SetActive(false);


            _menuRoot = new GameObject("WarpMenu");
            _menuRoot.transform.SetParent(parent, false);
            var mrect = _menuRoot.AddComponent<RectTransform>();
            mrect.anchorMin = Vector2.zero; mrect.anchorMax = Vector2.one;
            mrect.offsetMin = Vector2.zero; mrect.offsetMax = Vector2.zero;
            var ov = _menuRoot.AddComponent<Image>();
            ov.color = OverlayColor;
            ov.raycastTarget = true;
            _menuRoot.transform.SetAsLastSibling();

            var frame = new GameObject("Frame");
            frame.transform.SetParent(_menuRoot.transform, false);
            _frameRect = frame.AddComponent<RectTransform>();
            _frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            _frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            _frameRect.pivot = new Vector2(0.5f, 0.5f);
            _frameRect.sizeDelta = new Vector2(560, 360);
            _frameRect.anchoredPosition = Vector2.zero;
            frame.AddComponent<Image>().color = PanelInterior;
            AddEdge(frame.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 2));
            AddEdge(frame.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 2));
            AddEdge(frame.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(2, 0));
            AddEdge(frame.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(2, 0));

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(frame.transform, false);
            var trect = titleGo.AddComponent<RectTransform>();
            trect.anchorMin = new Vector2(0.5f, 1f); trect.anchorMax = new Vector2(0.5f, 1f);
            trect.pivot = new Vector2(0.5f, 1f);
            trect.anchoredPosition = new Vector2(0, -36);
            trect.sizeDelta = new Vector2(520, 56);
            _titleLabel = titleGo.AddComponent<TextMeshProUGUI>();
            _titleLabel.text = "REST";
            _titleLabel.fontSize = 36;
            _titleLabel.fontStyle = FontStyles.Bold;
            _titleLabel.color = BrightGold;
            _titleLabel.alignment = TextAlignmentOptions.Center;
            _titleLabel.characterSpacing = 6;

            var ruleGo = new GameObject("Rule");
            ruleGo.transform.SetParent(frame.transform, false);
            var rrect = ruleGo.AddComponent<RectTransform>();
            rrect.anchorMin = new Vector2(0.5f, 1f); rrect.anchorMax = new Vector2(0.5f, 1f);
            rrect.pivot = new Vector2(0.5f, 0.5f);
            rrect.sizeDelta = new Vector2(260, 1);
            rrect.anchoredPosition = new Vector2(0, -84);
            ruleGo.AddComponent<Image>().color = FrameGold;

            var btnContainer = new GameObject("Buttons");
            btnContainer.transform.SetParent(frame.transform, false);
            _buttonsRect = btnContainer.AddComponent<RectTransform>();
            _buttonsRect.anchorMin = new Vector2(0.5f, 1f); _buttonsRect.anchorMax = new Vector2(0.5f, 1f);
            _buttonsRect.pivot = new Vector2(0.5f, 1f);
            _buttonsRect.anchoredPosition = new Vector2(0, -110);
            _buttonsRect.sizeDelta = new Vector2(460, 400);
            var layout = btnContainer.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _menuRoot.SetActive(false);
        }

        private void AddEdge(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size) { SunlessReach.UI.UIBuilder.Edge(parent, aMin, aMax, pivot, size, SunlessReach.UI.UIBuilder.FrameGold); }
    }
}
