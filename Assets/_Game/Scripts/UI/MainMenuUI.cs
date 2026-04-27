using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

namespace SunlessReach.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Data.GameState gameState;

        Transform _mainPanel, _optionsPanel, _controlsPanel;
        RectTransform _mainFrameRect, _optionsFrameRect, _controlsFrameRect;
        Button[] _mainButtons;
        Button _backButton, _controlsBackButton, _fpsButton;
        TextMeshProUGUI _fpsLabel;
        Slider _musicSlider, _sfxSlider;
        Image _musicStripe, _sfxStripe;
        TextMeshProUGUI _musicLabel, _sfxLabel;
        GameObject _lastSelected;

        void Awake()
        {
            _mainPanel     = transform.Find("MainPanel");
            _optionsPanel  = transform.Find("OptionsPanel");
            _controlsPanel = transform.Find("ControlsPanel");
            _mainFrameRect     = (RectTransform)_mainPanel.Find("FramePanel");
            _optionsFrameRect  = (RectTransform)_optionsPanel.Find("FramePanel");
            _controlsFrameRect = (RectTransform)_controlsPanel.Find("FramePanel");

            var btns = _mainFrameRect.Find("Buttons");
            _mainButtons = new[]
            {
                btns.Find("Btn_PlayGame").GetComponent<Button>(),
                btns.Find("Btn_Options").GetComponent<Button>(),
                btns.Find("Btn_Controls").GetComponent<Button>(),
                btns.Find("Btn_QuitGame").GetComponent<Button>(),
            };
            _mainButtons[0].onClick.AddListener(() => Click(PlayGame));
            _mainButtons[1].onClick.AddListener(() => Click(OpenOptions));
            _mainButtons[2].onClick.AddListener(() => Click(OpenControls));
            _mainButtons[3].onClick.AddListener(() => Click(QuitGame));
            for (int i = 0; i < _mainButtons.Length; i++)
            {
                var nav = new Navigation { mode = Navigation.Mode.Explicit };
                if (i > 0) nav.selectOnUp = _mainButtons[i - 1];
                if (i < _mainButtons.Length - 1) nav.selectOnDown = _mainButtons[i + 1];
                _mainButtons[i].navigation = nav;
            }

            var vol = _optionsFrameRect.Find("VolumeRows");
            var musicRow = vol.Find("Vol_Music");
            var sfxRow   = vol.Find("Vol_Sfx");
            _musicSlider = musicRow.Find("Slider").GetComponent<Slider>();
            _sfxSlider   = sfxRow.Find("Slider").GetComponent<Slider>();
            _musicStripe = musicRow.Find("SelectStripe").GetComponent<Image>();
            _sfxStripe   = sfxRow.Find("SelectStripe").GetComponent<Image>();
            _musicLabel  = musicRow.Find("Label").GetComponent<TextMeshProUGUI>();
            _sfxLabel    = sfxRow.Find("Label").GetComponent<TextMeshProUGUI>();
            _musicSlider.value = Core.AudioPrefs.MusicVolume;
            _sfxSlider.value   = Core.AudioPrefs.SfxVolume;
            _musicSlider.onValueChanged.AddListener(v => Core.AudioPrefs.MusicVolume = v);
            _sfxSlider.onValueChanged.AddListener(v => Core.AudioPrefs.SfxVolume = v);

            _fpsButton = vol.Find("Btn_Fps").GetComponent<Button>();
            _fpsLabel  = _fpsButton.transform.Find("Label").GetComponent<TextMeshProUGUI>();
            _fpsLabel.text = FpsLabel();
            _fpsButton.onClick.AddListener(() => Click(ToggleFps));

            _backButton = _optionsFrameRect.Find("BackContainer/Btn_OptionsBack").GetComponent<Button>();
            _backButton.onClick.AddListener(() => Click(CloseOptions));

            _controlsBackButton = _controlsFrameRect.Find("BackContainer/Btn_ControlsBack").GetComponent<Button>();
            _controlsBackButton.onClick.AddListener(() => Click(CloseControls));

            _musicSlider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnDown = _sfxSlider };
            _sfxSlider.navigation   = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _musicSlider, selectOnDown = _fpsButton };
            _fpsButton.navigation   = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _sfxSlider,   selectOnDown = _backButton };
            _backButton.navigation  = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = _fpsButton };
            _controlsBackButton.navigation = new Navigation { mode = Navigation.Mode.Explicit };
        }

        void Start()
        {
            _optionsPanel.gameObject.SetActive(false);
            _controlsPanel.gameObject.SetActive(false);
            Select(_mainButtons[0]);
            StartCoroutine(AnimateOpen(_mainFrameRect));
        }

        void Update()
        {
            var sel = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            if (sel != _lastSelected)
            {
                if (sel != null && _lastSelected != null) Core.UiSfx.PlayScroll();
                _lastSelected = sel;
            }
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            foreach (var b in _mainButtons) PulseButton(b, sel, pulse);
            PulseButton(_backButton, sel, pulse);
            PulseButton(_controlsBackButton, sel, pulse);
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

        static void Click(System.Action a) { Core.UiSfx.PlayClick(); a(); }

        static string FpsLabel() => "FPS COUNTER:  " + (Core.AudioPrefs.ShowFps ? "ON" : "OFF");

        void ToggleFps()
        {
            Core.AudioPrefs.ShowFps = !Core.AudioPrefs.ShowFps;
            _fpsLabel.text = FpsLabel();
        }

        void Select(Button btn)
        {
            if (EventSystem.current != null && btn != null)
                EventSystem.current.SetSelectedGameObject(btn.gameObject);
        }

        IEnumerator AnimateOpen(RectTransform target)
        {
            if (target == null) yield break;
            const float duration = 0.22f;
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

        public void PlayGame()
        {
            if (gameState != null) gameState.ResetToDefaults();
            SceneManager.LoadScene("GameScene");
        }

        public void OpenOptions()
        {
            _mainPanel.gameObject.SetActive(false);
            _optionsPanel.gameObject.SetActive(true);
            Select(_backButton);
            StopAllCoroutines();
            StartCoroutine(AnimateOpen(_optionsFrameRect));
        }

        public void CloseOptions()
        {
            _optionsPanel.gameObject.SetActive(false);
            _mainPanel.gameObject.SetActive(true);
            Select(_mainButtons[1]);
            StopAllCoroutines();
            StartCoroutine(AnimateOpen(_mainFrameRect));
        }

        public void OpenControls()
        {
            _mainPanel.gameObject.SetActive(false);
            _controlsPanel.gameObject.SetActive(true);
            Select(_controlsBackButton);
            StopAllCoroutines();
            StartCoroutine(AnimateOpen(_controlsFrameRect));
        }

        public void CloseControls()
        {
            _controlsPanel.gameObject.SetActive(false);
            _mainPanel.gameObject.SetActive(true);
            Select(_mainButtons[2]);
            StopAllCoroutines();
            StartCoroutine(AnimateOpen(_mainFrameRect));
        }

        public void NewGame() => PlayGame();

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
