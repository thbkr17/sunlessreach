using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using TMPro;
using SunlessReach.Core;
using SunlessReach.Data;

namespace SunlessReach.UI
{
    public class VictoryScreen : MonoBehaviour
    {
        [SerializeField] private GameObject victoryPanel;

        private static readonly Color OverlayColor       = new Color(0.04f, 0.05f, 0.08f, 0.92f);
        private static readonly Color PanelInteriorColor = new Color(0.07f, 0.09f, 0.13f, 0.97f);
        private static readonly Color FrameGold          = new Color(0.78f, 0.62f, 0.28f, 1f);
        private static readonly Color BrightGold         = new Color(1.00f, 0.82f, 0.40f, 1f);
        private static readonly Color CreamText          = new Color(0.92f, 0.86f, 0.74f, 1f);
        private static readonly Color ItemBg             = new Color(0.10f, 0.12f, 0.16f, 0.85f);
        private static readonly Color ItemBgSelected     = new Color(0.16f, 0.20f, 0.26f, 1.00f);

        private RectTransform _frameRect;
        private Button _returnButton;
        private Image _returnBg;
        private Image _returnStripe;
        private TextMeshProUGUI _returnLabel;
        private TextMeshProUGUI _secretsValue;
        private TextMeshProUGUI _enemiesValue;
        private TextMeshProUGUI _deathsValue;
        private TextMeshProUGUI _timeValue;
        private GameState _gameState;

        private void Start()
        {
            // Hide any pre-existing scene panel; we build our own styled UI
            if (victoryPanel != null) victoryPanel.SetActive(false);
            victoryPanel = null;
        }

        private void OnEnable()
        {
            EventBus.OnVictory += ShowVictory;
        }

        private void OnDisable()
        {
            EventBus.OnVictory -= ShowVictory;
        }

        private void Update()
        {
            if (_returnButton == null || victoryPanel == null || !victoryPanel.activeSelf) return;
            UpdateButtonHighlight();
        }

        private void ShowVictory()
        {
            EnsureUI();
            UpdateStats();
            victoryPanel.SetActive(true);
            Time.timeScale = 0;
            if (EventSystem.current != null && _returnButton != null)
                EventSystem.current.SetSelectedGameObject(_returnButton.gameObject);
            StopAllCoroutines();
            StartCoroutine(AnimateOpen());
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1;
            SceneManager.LoadScene("MainMenu");
        }

        private const string Val = "<color=#FFD166><b>";   // open tag for a stat value
        private const string EndVal = "</b></color>";

        private void UpdateStats()
        {
            if (_gameState == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameState>();
                if (all.Length > 0) _gameState = all[0];
            }
            int secrets = 0, enemies = 0, deaths = 0;
            float time = 0f;
            if (_gameState != null)
            {
                secrets = _gameState.secretsTotal > 0
                    ? Mathf.Clamp(Mathf.RoundToInt(100f * _gameState.secretsCollected / _gameState.secretsTotal), 0, 100) : 0;
                enemies = _gameState.enemiesTotal > 0
                    ? Mathf.Clamp(Mathf.RoundToInt(100f * _gameState.enemiesDefeated / _gameState.enemiesTotal), 0, 100) : 0;
                deaths = _gameState.deathCount;
                time = _gameState.playTime;
            }
            if (_secretsValue != null) _secretsValue.text = $"SECRETS COLLECTED   {Val}{secrets}%{EndVal}";
            if (_enemiesValue != null) _enemiesValue.text = $"ENEMIES DEFEATED   {Val}{enemies}%{EndVal}";
            if (_deathsValue  != null) _deathsValue.text  = $"DEATHS   {Val}{deaths}{EndVal}";
            if (_timeValue    != null) _timeValue.text    = $"TIME   {Val}{FormatTime(time)}{EndVal}";
        }

        private static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int h = s / 3600, m = (s % 3600) / 60, sec = s % 60;
            return h > 0 ? $"{h}:{m:00}:{sec:00}" : $"{m:00}:{sec:00}";
        }

        private TextMeshProUGUI MakeStatLine(Transform parent, string name, float yFromTop)
        {
            var go = CreateText(parent, name, "", 26,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, yFromTop), new Vector2(700, 34));
            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.color = CreamText;
            tmp.fontStyle = FontStyles.Bold;
            tmp.characterSpacing = 4;
            return tmp;
        }

        private void EnsureUI()
        {
            if (_frameRect != null && victoryPanel != null) return;
            BuildUI();
        }

        private void BuildUI()
        {
            victoryPanel = new GameObject("VictoryPanel");
            victoryPanel.transform.SetParent(transform, false);
            var panelRect = victoryPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            victoryPanel.AddComponent<Image>().color = OverlayColor;

            var frame = new GameObject("FramePanel");
            frame.transform.SetParent(victoryPanel.transform, false);
            _frameRect = frame.AddComponent<RectTransform>();
            _frameRect.anchorMin = new Vector2(0.5f, 0.5f);
            _frameRect.anchorMax = new Vector2(0.5f, 0.5f);
            _frameRect.pivot = new Vector2(0.5f, 0.5f);
            _frameRect.sizeDelta = new Vector2(760, 600);
            _frameRect.anchoredPosition = Vector2.zero;
            frame.AddComponent<Image>().color = PanelInteriorColor;

            CreateEdge(frame.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1f), new Vector2(0, 2), FrameGold);
            CreateEdge(frame.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0f), new Vector2(0, 2), FrameGold);
            CreateEdge(frame.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0f, 0.5f), new Vector2(2, 0), FrameGold);
            CreateEdge(frame.transform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1f, 0.5f), new Vector2(2, 0), FrameGold);

            CreateDiamond(frame.transform, new Vector2(0, 1), new Vector2(16, -16), 14, BrightGold);
            CreateDiamond(frame.transform, new Vector2(1, 1), new Vector2(-16, -16), 14, BrightGold);
            CreateDiamond(frame.transform, new Vector2(0, 0), new Vector2(16, 16), 14, BrightGold);
            CreateDiamond(frame.transform, new Vector2(1, 0), new Vector2(-16, 16), 14, BrightGold);

            var title = CreateText(frame.transform, "Title", "VICTORY", 64,
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -100), new Vector2(700, 90));
            var titleTmp = title.GetComponent<TextMeshProUGUI>();
            titleTmp.color = BrightGold;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.characterSpacing = 18;

            var rule = new GameObject("TitleRule");
            rule.transform.SetParent(frame.transform, false);
            var ruleRect = rule.AddComponent<RectTransform>();
            ruleRect.anchorMin = new Vector2(0.5f, 1f);
            ruleRect.anchorMax = new Vector2(0.5f, 1f);
            ruleRect.pivot = new Vector2(0.5f, 0.5f);
            ruleRect.sizeDelta = new Vector2(360, 1);
            ruleRect.anchoredPosition = new Vector2(0, -170);
            rule.AddComponent<Image>().color = FrameGold;
            CreateDiamond(frame.transform, new Vector2(0.5f, 1f), new Vector2(0, -170), 12, BrightGold);

            _secretsValue = MakeStatLine(frame.transform, "SecretsLine", -220);
            _enemiesValue = MakeStatLine(frame.transform, "EnemiesLine", -272);
            _deathsValue  = MakeStatLine(frame.transform, "DeathsLine",  -324);
            _timeValue    = MakeStatLine(frame.transform, "TimeLine",    -376);

            BuildReturnButton(frame.transform);

            victoryPanel.SetActive(false);
        }

        private void BuildReturnButton(Transform parent)
        {
            var btnObj = new GameObject("ReturnBtn");
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0, 60);
            rect.sizeDelta = new Vector2(360, 64);

            _returnBg = btnObj.AddComponent<Image>();
            _returnBg.color = ItemBg;

            _returnButton = btnObj.AddComponent<Button>();
            _returnButton.transition = Selectable.Transition.None;
            _returnButton.onClick.AddListener(ReturnToMenu);

            var hairlineColor = new Color(FrameGold.r, FrameGold.g, FrameGold.b, 0.35f);
            CreateEdge(btnObj.transform, new Vector2(0, 1), new Vector2(1, 1),
                new Vector2(0.5f, 1f), new Vector2(0, 1), hairlineColor);
            CreateEdge(btnObj.transform, new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(0.5f, 0f), new Vector2(0, 1), hairlineColor);

            var stripeObj = new GameObject("SelectStripe");
            stripeObj.transform.SetParent(btnObj.transform, false);
            var stripeRect = stripeObj.AddComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0, 0);
            stripeRect.anchorMax = new Vector2(0, 1);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.sizeDelta = new Vector2(4, 0);
            stripeRect.anchoredPosition = Vector2.zero;
            _returnStripe = stripeObj.AddComponent<Image>();
            _returnStripe.color = BrightGold;
            stripeObj.SetActive(false);

            var labelObj = new GameObject("Label");
            labelObj.transform.SetParent(btnObj.transform, false);
            var lrect = labelObj.AddComponent<RectTransform>();
            lrect.anchorMin = Vector2.zero;
            lrect.anchorMax = Vector2.one;
            lrect.offsetMin = new Vector2(24, 0);
            lrect.offsetMax = new Vector2(-24, 0);
            _returnLabel = labelObj.AddComponent<TextMeshProUGUI>();
            _returnLabel.text = "Return to Menu";
            _returnLabel.fontSize = 28;
            _returnLabel.fontStyle = FontStyles.Bold;
            _returnLabel.color = Color.white;
            _returnLabel.alignment = TextAlignmentOptions.Center;
            _returnLabel.enableWordWrapping = false;

            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            _returnButton.navigation = nav;
        }

        private void UpdateButtonHighlight()
        {
            bool isSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _returnButton.gameObject;
            float pulse = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 5f);
            if (_returnStripe != null)
            {
                if (_returnStripe.gameObject.activeSelf != isSelected)
                    _returnStripe.gameObject.SetActive(isSelected);
                if (isSelected)
                    _returnStripe.color = new Color(BrightGold.r, BrightGold.g, BrightGold.b, pulse);
            }
            if (isSelected)
            {
                _returnBg.color = ItemBgSelected;
                _returnLabel.color = BrightGold;
            }
            else
            {
                _returnBg.color = ItemBg;
                _returnLabel.color = Color.white;
            }
        }

        private IEnumerator AnimateOpen()
        {
            if (_frameRect == null) yield break;
            const float duration = 0.4f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                t = 1f - Mathf.Pow(1f - t, 3f);
                float scale = Mathf.Lerp(0.9f, 1.0f, t);
                _frameRect.localScale = new Vector3(scale, scale, 1);
                yield return null;
            }
            _frameRect.localScale = Vector3.one;
        }

        private void CreateEdge(Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Color color) { UIBuilder.Edge(parent, anchorMin, anchorMax, pivot, sizeDelta, color); }

        private Image CreateDiamond(Transform parent, Vector2 anchor, Vector2 anchoredPos, float size, Color color) { return UIBuilder.Diamond(parent, anchor, anchoredPos, size, color); }

        private GameObject CreateText(Transform parent, string name, string text, int fontSize,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size) { return UIBuilder.Label(parent, name, text, fontSize, anchorMin, anchorMax, position, size); }
    }
}
