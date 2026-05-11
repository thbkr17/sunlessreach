using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;
using TMPro;
using SunlessReach.Core;
using SunlessReach.Data;

namespace SunlessReach.UI
{
    public class TutorialPrompts : MonoBehaviour
    {
        private enum Hint { Attack, Jump, Heal, Dash, DoubleJump }

        private static readonly HashSet<Hint> _seen = new HashSet<Hint>();

        private const float AutoDismiss = 12f;
        private const float EnemyHintRange = 7f;
        private const float JumpHintDelay = 1.5f;

        private Canvas _canvas;
        private GameObject _panel;
        private TextMeshProUGUI _label;
        private CanvasGroup _group;

        private GameState _gs;
        private Transform _player;

        private Hint? _active;
        private Func<bool> _activeCheck;
        private float _activeTimer;
        private readonly Queue<Hint> _queue = new Queue<Hint>();

        private bool _jumpArmed;
        private float _jumpTimer;
        private float _enemyScanTimer;

        private void Awake() { BuildUI(); }

        private void Start()
        {
            var all = Resources.FindObjectsOfTypeAll<GameState>();
            if (all.Length > 0) _gs = all[0];
            ArmForGameplay();
        }

        private void OnEnable()
        {
            EventBus.OnHealthChanged += OnHealthChanged;
            EventBus.OnAbilityUnlocked += OnAbilityUnlocked;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            EventBus.OnHealthChanged -= OnHealthChanged;
            EventBus.OnAbilityUnlocked -= OnAbilityUnlocked;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            HidePanel();
            _queue.Clear();
            _active = null;
            _activeCheck = null;
            if (scene.name == "MainMenu")
            {
                _seen.Clear();   // re-arm tutorials for the next run
                _jumpArmed = false;
                return;
            }
            ArmForGameplay();
        }

        private void ArmForGameplay()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            _player = p != null ? p.transform : null;
            _jumpArmed = !_seen.Contains(Hint.Jump);
            _jumpTimer = JumpHintDelay;
            _enemyScanTimer = 0.4f;
        }

        private void OnHealthChanged(int current, int max)
        {
            if (_gs == null || _seen.Contains(Hint.Heal)) return;
            // first time the player is hurt while they actually have enough soul to heal
            if (current > 0 && current < max && _gs.currentSouls >= _gs.soulsPerHeal)
                Trigger(Hint.Heal);
        }

        private void OnAbilityUnlocked(AbilityType type)
        {
            if (type == AbilityType.Dash) Trigger(Hint.Dash);
            else if (type == AbilityType.DoubleJump) Trigger(Hint.DoubleJump);
        }

        private void Update()
        {
            // delayed first-jump hint (skip it if the player already jumped on their own)
            if (_jumpArmed)
            {
                if (Pressed(Key.Space, g => g.buttonSouth)) { _jumpArmed = false; _seen.Add(Hint.Jump); }
                else
                {
                    _jumpTimer -= Time.unscaledDeltaTime;
                    if (_jumpTimer <= 0f) { _jumpArmed = false; Trigger(Hint.Jump); }
                }
            }

            // first enemy encounter (regular enemies, not the boss)
            if (!_seen.Contains(Hint.Attack))
            {
                _enemyScanTimer -= Time.unscaledDeltaTime;
                if (_enemyScanTimer <= 0f)
                {
                    _enemyScanTimer = 0.3f;
                    if (_player == null)
                    {
                        var p = GameObject.FindGameObjectWithTag("Player");
                        _player = p != null ? p.transform : null;
                    }
                    if (_player != null)
                    {
                        foreach (var e in UnityEngine.Object.FindObjectsByType<Enemies.EnemyBase>(FindObjectsSortMode.None))
                        {
                            if (e == null) continue;
                            if (Vector3.Distance(e.transform.position, _player.position) <= EnemyHintRange) { Trigger(Hint.Attack); break; }
                        }
                    }
                }
            }

            if (_active.HasValue)
            {
                _activeTimer -= Time.unscaledDeltaTime;
                if ((_activeCheck != null && _activeCheck()) || _activeTimer <= 0f) Dismiss();
            }
        }

        private void Trigger(Hint h)
        {
            if (_seen.Contains(h) || _active == h || _queue.Contains(h)) return;
            if (_active.HasValue) { _queue.Enqueue(h); return; }
            Show(h);
        }

        private void Show(Hint h)
        {
            _active = h;
            _activeTimer = AutoDismiss;
            var data = HintData(h);
            _label.text = data.Item1;
            _activeCheck = data.Item2;
            _canvas.enabled = true;
            _panel.SetActive(true);
            if (_group != null) _group.alpha = 1f;
        }

        private void Dismiss()
        {
            if (_active.HasValue) _seen.Add(_active.Value);
            _active = null;
            _activeCheck = null;
            if (_queue.Count > 0) Show(_queue.Dequeue());
            else HidePanel();
        }

        private void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private static (string, Func<bool>) HintData(Hint h)
        {
            switch (h)
            {
                case Hint.Attack:     return ("Press   <sprite name=\"j\">  /  <sprite name=\"xx\">   to <b>Attack</b>",          () => Pressed(Key.J, g => g.buttonWest));
                case Hint.Jump:       return ("Press   <sprite name=\"space\">  /  <sprite name=\"xa\">   to <b>Jump</b>",         () => Pressed(Key.Space, g => g.buttonSouth));
                case Hint.Heal:       return ("Hold   <sprite name=\"k\">  /  <sprite name=\"xy\">   to <b>Heal</b>  (uses soul)",  () => Pressed(Key.K, g => g.buttonNorth));
                case Hint.Dash:       return ("Press   <sprite name=\"shift\">  /  <sprite name=\"xb\">   to <b>Dash</b>",          () => Pressed(Key.LeftShift, g => g.buttonEast));
                case Hint.DoubleJump: return ("In the air, press   <sprite name=\"space\">  /  <sprite name=\"xa\">   again to <b>Double Jump</b>", () => Pressed(Key.Space, g => g.buttonSouth));
            }
            return (string.Empty, () => true);
        }

        private static bool Pressed(Key key, Func<Gamepad, ButtonControl> gpButton)
        {
            if (Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame) return true;
            if (Gamepad.current != null && gpButton(Gamepad.current).wasPressedThisFrame) return true;
            return false;
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("TutorialCanvas");
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 30000;
            _canvas.enabled = false;

            _panel = new GameObject("HintPanel");
            _panel.transform.SetParent(canvasGo.transform, false);
            var prt = _panel.AddComponent<RectTransform>();
            prt.anchorMin = new Vector2(0.5f, 0f);
            prt.anchorMax = new Vector2(0.5f, 0f);
            prt.pivot = new Vector2(0.5f, 0f);
            prt.anchoredPosition = new Vector2(0f, 110f);
            prt.sizeDelta = new Vector2(700f, 64f);
            var bg = _panel.AddComponent<Image>();
            bg.color = new Color(0.04f, 0.05f, 0.08f, 0.86f);
            _group = _panel.AddComponent<CanvasGroup>();

            var gold = new Color(0.78f, 0.62f, 0.28f, 1f);
            Edge(_panel.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 2), gold);
            Edge(_panel.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 2), gold);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_panel.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(24f, 0f);
            lrt.offsetMax = new Vector2(-24f, 0f);
            _label = labelGo.AddComponent<TextMeshProUGUI>();
            _label.fontSize = 26f;
            _label.alignment = TextAlignmentOptions.Center;
            _label.color = new Color(0.92f, 0.86f, 0.74f, 1f);
            _label.richText = true;
            _label.raycastTarget = false;
            _label.enableWordWrapping = false;

            _panel.SetActive(false);
        }

        private static void Edge(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size, Color c) { UIBuilder.Edge(parent, aMin, aMax, pivot, size, c); }
    }
}
