using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class DeathScreen : MonoBehaviour
    {
        // Old scene-authored panel, kept hidden - we build our own overlay below.
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private AudioClip deathClip;        // "YOU DIED" stinger
        [SerializeField] private float deathClipVolume = 0.85f;

        private static readonly Color BlackColor  = new Color(0f, 0f, 0f, 1f);
        private static readonly Color DiedColor   = new Color(0.80f, 0.10f, 0.10f, 1f);
        private static readonly Color PromptColor = new Color(0.85f, 0.82f, 0.78f, 1f);

        private const float FadeDuration = 1.6f;   // black closes all the way in
        private const float DiedFadeIn   = 0.7f;
        private const float PromptFadeIn  = 0.45f;
        private const float DiedDelay    = 0.15f;  // after the screen is fully black
        private const float PromptDelay  = 0.25f;  // after DIED text shows

        private GameObject _root;
        private Image _blackBg;        // solid black, fills the screen
        private Image _vignette;       // radial gradient: black at edges, clear in centre
        private RectTransform _vignetteRect;
        private TextMeshProUGUI _diedLabel, _promptLabel;
        private bool _acceptInput;
        private bool _sawAllReleased;
        private Coroutine _seq;
        private AudioSource _deathAudio;
        private static Sprite _vignetteSprite;

        private void Start()
        {
            if (deathPanel != null) deathPanel.SetActive(false);
        }

        private void OnEnable()
        {
            EventBus.OnPlayerDied += OnPlayerDied;
            EventBus.OnPlayerRespawned += OnPlayerRespawned;
        }

        private void OnDisable()
        {
            EventBus.OnPlayerDied -= OnPlayerDied;
            EventBus.OnPlayerRespawned -= OnPlayerRespawned;
            AudioListener.pause = false;   // never leave global audio paused
        }

        private void Update()
        {
            if (!_acceptInput) return;

            bool anyDown = AnyInputHeld();
            // Don't fire on a key the player was already holding from gameplay - wait for
            // one clean "everything released" frame, then the next press respawns.
            if (!_sawAllReleased)
            {
                if (!anyDown) _sawAllReleased = true;
                return;
            }
            if (anyDown)
            {
                _acceptInput = false;
                DoRespawn();
            }
        }

        private static bool AnyInputHeld()
        {
            if (Keyboard.current != null && Keyboard.current.anyKey.isPressed) return true;
            if (Gamepad.current != null)
            {
                var gp = Gamepad.current;
                if (gp.buttonSouth.isPressed || gp.buttonEast.isPressed ||
                    gp.buttonNorth.isPressed || gp.buttonWest.isPressed ||
                    gp.startButton.isPressed) return true;
            }
            if (Mouse.current != null && Mouse.current.leftButton.isPressed) return true;
            return false;
        }

        private void OnPlayerDied()
        {
            if (deathPanel != null) deathPanel.SetActive(false);
            EnsureUI();
            _root.SetActive(true);

            // Silence everything else and play just the death stinger.
            AudioListener.pause = true;
            if (deathClip != null)
            {
                if (_deathAudio == null)
                {
                    _deathAudio = gameObject.AddComponent<AudioSource>();
                    _deathAudio.playOnAwake = false;
                    _deathAudio.spatialBlend = 0f;
                    _deathAudio.ignoreListenerPause = true;   // plays through the global pause
                }
                _deathAudio.Stop();
                _deathAudio.clip = deathClip;
                _deathAudio.volume = Mathf.Clamp01(deathClipVolume) * Core.AudioPrefs.SfxVolume;
                _deathAudio.Play();
            }

            if (_seq != null) StopCoroutine(_seq);
            _seq = StartCoroutine(PlaySequence());
        }

        private void EndDeathState()
        {
            AudioListener.pause = false;
            if (_deathAudio != null) _deathAudio.Stop();
        }

        private void OnPlayerRespawned()
        {
            if (_seq != null) { StopCoroutine(_seq); _seq = null; }
            _acceptInput = false;
            if (_root != null) _root.SetActive(false);
            EndDeathState();
        }

        private void DoRespawn()
        {
            if (_root != null) _root.SetActive(false);
            EndDeathState();
            var gm = FindAnyObjectByType<GameManager>();
            if (gm != null) gm.RespawnPlayer();
        }

        public void Respawn()  // kept for any UI button still wired to it
        {
            _acceptInput = false;
            DoRespawn();
        }

        public void QuitToMenu()
        {
            EndDeathState();
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        private IEnumerator PlaySequence()
        {
            _acceptInput = false;
            _sawAllReleased = false;
            SetImageAlpha(_blackBg, 0f);
            SetImageAlpha(_vignette, 0f);
            SetVignetteScale(1f);
            SetAlpha(_diedLabel, 0f);
            SetAlpha(_promptLabel, 0f);

            // 1) Black closes inward: the vignette (dark edges) intensifies and shrinks so
            //    its clear centre collapses to nothing, while a solid black layer behind it
            //    ramps up to fully opaque - so it ends 100% black with no gaps.
            float t = 0f;
            while (t < FadeDuration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / FadeDuration);
                // vignette appears quickly and starts squeezing inward
                _vignette.color = new Color(0f, 0f, 0f, Mathf.Clamp01(p * 2.2f));
                SetVignetteScale(Mathf.Lerp(3.2f, 0.45f, Mathf.SmoothStep(0f, 1f, p)));
                // solid black hangs back, then rushes in for the final frames
                SetImageAlpha(_blackBg, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((p - 0.45f) / 0.55f)));
                yield return null;
            }
            SetVignetteScale(0.45f);
            _vignette.color = BlackColor;
            SetImageAlpha(_blackBg, 1f);

            yield return WaitUnscaled(DiedDelay);

            // 2) "YOU DIED" fades in (eases down in scale a touch)
            t = 0f;
            var diedRect = _diedLabel.rectTransform;
            while (t < DiedFadeIn)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / DiedFadeIn);
                SetAlpha(_diedLabel, p);
                float s = Mathf.Lerp(1.12f, 1f, Mathf.SmoothStep(0f, 1f, p));
                diedRect.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            SetAlpha(_diedLabel, 1f);
            diedRect.localScale = Vector3.one;

            yield return WaitUnscaled(PromptDelay);

            // 3) "Press any key to respawn" fades in
            t = 0f;
            while (t < PromptFadeIn)
            {
                t += Time.unscaledDeltaTime;
                SetAlpha(_promptLabel, Mathf.Clamp01(t / PromptFadeIn));
                yield return null;
            }
            SetAlpha(_promptLabel, 1f);

            _acceptInput = true;

            // Gentle pulse on the prompt while we wait for input
            while (_acceptInput)
            {
                float a = 0.55f + 0.45f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f);
                SetAlpha(_promptLabel, a);
                yield return null;
            }
        }

        private static IEnumerator WaitUnscaled(float seconds)
        {
            float t = 0f;
            while (t < seconds) { t += Time.unscaledDeltaTime; yield return null; }
        }

        private void SetVignetteScale(float s)
        {
            if (_vignetteRect != null) _vignetteRect.localScale = new Vector3(s, s, 1f);
        }

        private static void SetAlpha(TextMeshProUGUI label, float a)
        {
            if (label == null) return;
            var c = label.color; c.a = a; label.color = c;
        }

        private static void SetImageAlpha(Image img, float a)
        {
            if (img == null) return;
            var c = img.color; c.a = a; img.color = c;
        }

        private void EnsureUI()
        {
            if (_root != null) return;

            // Render inside an existing screen-space canvas. Creating a *child* canvas
            // would make a nested canvas, which does NOT auto-fit the screen - its rect
            // would stay at the default ~100x100, shrinking everything to a tiny square.
            Transform parent = transform;
            var ownCanvas = GetComponent<Canvas>();
            if (ownCanvas == null)
            {
                var sceneCanvas = FindAnyObjectByType<Canvas>();
                if (sceneCanvas != null) parent = sceneCanvas.transform;
                else
                {
                    var canvasObj = new GameObject("DeathScreenCanvas");
                    var c = canvasObj.AddComponent<Canvas>();
                    c.renderMode = RenderMode.ScreenSpaceOverlay;
                    c.sortingOrder = 500;
                    canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    canvasObj.AddComponent<GraphicRaycaster>();
                    parent = canvasObj.transform;
                }
            }

            _root = new GameObject("DeathScreenOverlay");
            _root.transform.SetParent(parent, false);
            StretchFull(_root.AddComponent<RectTransform>());
            _root.transform.SetAsLastSibling();   // draw on top of everything else in the canvas

            // Solid black backing - guarantees a fully-black end state and covers the
            // screen corners that the shrinking vignette no longer reaches.
            var bgGo = new GameObject("BlackBg");
            bgGo.transform.SetParent(_root.transform, false);
            StretchFull(bgGo.AddComponent<RectTransform>());
            _blackBg = bgGo.AddComponent<Image>();
            _blackBg.color = new Color(0f, 0f, 0f, 0f);
            _blackBg.raycastTarget = false;

            // Radial vignette layer (drawn on top of the solid layer)
            var vigGo = new GameObject("Vignette");
            vigGo.transform.SetParent(_root.transform, false);
            _vignetteRect = vigGo.AddComponent<RectTransform>();
            StretchFull(_vignetteRect);
            _vignette = vigGo.AddComponent<Image>();
            _vignette.sprite = GetVignetteSprite();
            _vignette.type = Image.Type.Simple;
            _vignette.color = new Color(0f, 0f, 0f, 0f);
            _vignette.raycastTarget = false;

            _diedLabel = MakeLabel("YouDied", _root.transform, "YOU DIED", 160, FontStyles.Bold, DiedColor);
            var dr = _diedLabel.rectTransform;
            dr.anchorMin = new Vector2(0.5f, 0.5f);
            dr.anchorMax = new Vector2(0.5f, 0.5f);
            dr.pivot     = new Vector2(0.5f, 0.5f);
            dr.sizeDelta = new Vector2(1600f, 260f);
            dr.anchoredPosition = new Vector2(0f, 70f);
            _diedLabel.characterSpacing = 12f;

            _promptLabel = MakeLabel("Prompt", _root.transform, "Press any key to respawn", 38, FontStyles.Normal, PromptColor);
            var pr = _promptLabel.rectTransform;
            pr.anchorMin = new Vector2(0.5f, 0.5f);
            pr.anchorMax = new Vector2(0.5f, 0.5f);
            pr.pivot     = new Vector2(0.5f, 0.5f);
            pr.sizeDelta = new Vector2(1200f, 70f);
            pr.anchoredPosition = new Vector2(0f, -120f);
            _promptLabel.characterSpacing = 4f;

            SetImageAlpha(_blackBg, 0f);
            SetImageAlpha(_vignette, 0f);
            SetVignetteScale(1f);
            SetAlpha(_diedLabel, 0f);
            SetAlpha(_promptLabel, 0f);
            _root.SetActive(false);
        }

        private static Sprite GetVignetteSprite()
        {
            if (_vignetteSprite != null) return _vignetteSprite;
            const int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var cols = new Color32[size * size];
            Vector2 c = new Vector2((size - 1) / 2f, (size - 1) / 2f);
            float maxD = size / 2f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / maxD;   // 0 centre .. ~1.41 corner
                float a = Mathf.SmoothStep(0.30f, 1.05f, d);               // clear centre, opaque toward edges
                cols[y * size + x] = new Color32(0, 0, 0, (byte)Mathf.Clamp(a * 255f, 0f, 255f));
            }
            tex.SetPixels32(cols);
            tex.Apply(false, false);
            _vignetteSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            return _vignetteSprite;
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = Vector2.zero;
        }

        private static TextMeshProUGUI MakeLabel(string name, Transform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text;
            label.fontSize = size;
            label.enableAutoSizing = false;
            label.fontStyle = style;
            label.color = color;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
            return label;
        }
    }
}
