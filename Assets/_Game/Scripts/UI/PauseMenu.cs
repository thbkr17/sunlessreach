using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;

        Transform _root;
        Transform _mainPanel, _optionsPanel, _controlsPanel;
        RectTransform _mainFrameRect, _optionsFrameRect, _controlsFrameRect;
        Button[] _mainButtons;
        Button _optionsBack, _controlsBack, _fpsButton;
        TextMeshProUGUI _fpsLabel;
        Slider _musicSlider, _sfxSlider;
        Image _musicStripe, _sfxStripe;
        TextMeshProUGUI _musicLabel, _sfxLabel;
        GameObject _lastSelected;
        bool _isPaused;
        bool _wired;
        Coroutine _anim;

        void Start()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
        }

        void Update()
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;
            bool pressed = (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                        || (gamepad != null && gamepad.startButton.wasPressedThisFrame);

            if (pressed)
            {
                if (_isPaused && _optionsPanel != null && _optionsPanel.gameObject.activeSelf) { CloseOptions(); return; }
                if (_isPaused && _controlsPanel != null && _controlsPanel.gameObject.activeSelf) { CloseControls(); return; }
                TogglePause();
            }

            if (_isPaused) PulseAll();
        }

        public void TogglePause() => SetPaused(!_isPaused);
        public void Resume() => SetPaused(false);

        public void QuitToMenu()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }

        void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (paused)
            {
                EnsureWired();
                _root.gameObject.SetActive(true);
                ShowOnly(_mainPanel);
                Select(_mainButtons[0]);
                PlayOpen(_mainFrameRect);
                Time.timeScale = 0f;
                EventBus.RaiseGamePaused();
            }
            else
            {
                if (_root != null) _root.gameObject.SetActive(false);
                Time.timeScale = 1f;
                EventBus.RaiseGameResumed();
            }
        }

        public void OpenOptions()  { ShowOnly(_optionsPanel);  Select(_optionsBack);  PlayOpen(_optionsFrameRect); }
        public void CloseOptions() { ShowOnly(_mainPanel);     Select(_mainButtons[1]); PlayOpen(_mainFrameRect); }
        public void OpenControls() { ShowOnly(_controlsPanel); Select(_controlsBack); PlayOpen(_controlsFrameRect); }
        public void CloseControls(){ ShowOnly(_mainPanel);     Select(_mainButtons[2]); PlayOpen(_mainFrameRect); }

        void ShowOnly(Transform panel)
        {
            _mainPanel.gameObject.SetActive(panel == _mainPanel);
            _optionsPanel.gameObject.SetActive(panel == _optionsPanel);
            _controlsPanel.gameObject.SetActive(panel == _controlsPanel);
        }

        void EnsureWired()
        {
            if (_wired) return;
            _wired = true;

            _root = transform.Find("PauseMenuOverlay");
            _mainPanel     = _root.Find("PauseMain");
            _optionsPanel  = _root.Find("PauseOptions");
            _controlsPanel = _root.Find("PauseControls");
            _mainFrameRect     = (RectTransform)_mainPanel.Find("FramePanel");
            _optionsFrameRect  = (RectTransform)_optionsPanel.Find("FramePanel");
            _controlsFrameRect = (RectTransform)_controlsPanel.Find("FramePanel");

            var btns = _mainFrameRect.Find("Buttons");
            _mainButtons = new[]
            {
                btns.Find("Btn_Resume").GetComponent<Button>(),
                btns.Find("Btn_Options").GetComponent<Button>(),
                btns.Find("Btn_Controls").GetComponent<Button>(),
                btns.Find("Btn_QuitMenu").GetComponent<Button>(),
            };
            _mainButtons[0].onClick.AddListener(() => Click(Resume));
            _mainButtons[1].onClick.AddListener(() => Click(OpenOptions));
            _mainButtons[2].onClick.AddListener(() => Click(OpenControls));
            _mainButtons[3].onClick.AddListener(() => Click(QuitToMenu));
            for (int i = 0; i < _mainButtons.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0) nav.selectOnUp = _mainButtons[i - 1];
                if (i < _mainButtons.Length - 1) nav.selectOnDown = _mainButtons[i + 1];
                _mainButtons[i].navigation = nav;
            }

            var vol = _optionsFrameRect.Find("VolumeRows");
            var music = vol.Find("Vol_Music");
            var sfx   = vol.Find("Vol_Sfx");
            _musicSlider = music.Find("Slider").GetComponent<Slider>();
            _sfxSlider   = sfx.Find("Slider").GetComponent<Slider>();
            _musicStripe = music.Find("SelectStripe").GetComponent<Image>();
            _sfxStripe   = sfx.Find("SelectStripe").GetComponent<Image>();
            _musicLabel  = music.Find("Label").GetComponent<TextMeshProUGUI>();
            _sfxLabel    = sfx.Find("Label").GetComponent<TextMeshProUGUI>();
            _musicSlider.value = AudioPrefs.MusicVolume;
            _sfxSlider.value   = AudioPrefs.SfxVolume;
            _musicSlider.onValueChanged.AddListener(v => AudioPrefs.MusicVolume = v);
            _sfxSlider.onValueChanged.AddListener(v => AudioPrefs.SfxVolume = v);

            _fpsButton = vol.Find("Btn_Fps").GetComponent<Button>();
            _fpsLabel  = _fpsButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            _fpsLabel.text = FpsLabel();
            _fpsButton.onClick.AddListener(() => Click(ToggleFps));

            _optionsBack = _optionsFrameRect.Find("BackContainer/Btn_OptionsBack").GetComponent<Button>();
            _optionsBack.onClick.AddListener(() => Click(CloseOptions));
            _controlsBack = _controlsFrameRect.Find("BackContainer/Btn_ControlsBack").GetComponent<Button>();
            _controlsBack.onClick.AddListener(() => Click(CloseControls));

            _musicSlider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = _sfxSlider };
            _sfxSlider.navigation   = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _musicSlider, selectOnDown = _fpsButton };
            _fpsButton.navigation   = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _sfxSlider,   selectOnDown = _optionsBack };
            _optionsBack.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _fpsButton };
            _controlsBack.navigation = new Navigation { mode = Navigation.Mode.Explicit };
        }

        void PulseAll()
        {
            var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (sel != _lastSelected)
            {
                if (sel != null && _lastSelected != null) UiSfx.PlayScroll();
                _lastSelected = sel;
            }
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var b in _mainButtons) PulseButton(b, sel, pulse);
            PulseButton(_optionsBack, sel, pulse);
            PulseButton(_controlsBack, sel, pulse);
            PulseButton(_fpsButton, sel, pulse);
            PulseVolume(_musicSlider, _musicStripe, _musicLabel, sel, pulse);
            PulseVolume(_sfxSlider, _sfxStripe, _sfxLabel, sel, pulse);
        }

        static void PulseButton(Button btn, GameObject sel, float pulse)
        {
            if (btn == null) return;
            bool isSel = sel == btn.gameObject;
            var stripe = btn.transform.Find("SelectStripe");
            if (stripe != null)
            {
                if (stripe.gameObject.activeSelf != isSel) stripe.gameObject.SetActive(isSel);
                if (isSel) stripe.GetComponent<Image>().color = new Color(UIBuilder.BrightGold.r, UIBuilder.BrightGold.g, UIBuilder.BrightGold.b, pulse);
            }
            var bg = btn.GetComponent<Image>();
            var lbl = btn.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            if (isSel) { bg.color = UIBuilder.ItemBgSelected; lbl.color = UIBuilder.BrightGold; }
            else       { bg.color = UIBuilder.ItemBg;         lbl.color = Color.white; }
        }

        static void PulseVolume(Slider s, Image stripe, TextMeshProUGUI label, GameObject sel, float pulse)
        {
            if (s == null) return;
            bool isSel = sel == s.gameObject;
            if (stripe != null)
            {
                if (stripe.gameObject.activeSelf != isSel) stripe.gameObject.SetActive(isSel);
                if (isSel) stripe.color = new Color(UIBuilder.BrightGold.r, UIBuilder.BrightGold.g, UIBuilder.BrightGold.b, pulse);
            }
            if (label != null) label.color = isSel ? UIBuilder.BrightGold : UIBuilder.CreamText;
        }

        static void Click(System.Action a) { UiSfx.PlayClick(); a(); }

        static string FpsLabel() => "FPS COUNTER:  " + (AudioPrefs.ShowFps ? "ON" : "OFF");

        void ToggleFps()
        {
            AudioPrefs.ShowFps = !AudioPrefs.ShowFps;
            _fpsLabel.text = FpsLabel();
        }

        void Select(Button btn)
        {
            if (EventSystem.current != null && btn != null)
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }

        void PlayOpen(RectTransform target)
        {
            if (_anim != null) StopCoroutine(_anim);
            _anim = StartCoroutine(AnimateOpen(target));
        }

        IEnumerator AnimateOpen(RectTransform target)
        {
            if (target == null) yield break;
            const float duration = 0.18f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                float s = Mathf.Lerp(0.92f, 1.0f, t);
                target.localScale = new Vector3(s, s, 1);
                yield return null;
            }
            target.localScale = Vector3.one;
        }
    }
}
