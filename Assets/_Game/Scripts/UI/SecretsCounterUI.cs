using UnityEngine;
using TMPro;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class SecretsCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private void Awake()
        {
            if (label == null) label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            EventBus.OnGamePaused += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            EventBus.OnGamePaused -= Refresh;
        }

        private void Refresh()
        {
            if (label == null) return;
            var gs = FindAnyObjectByType<GameManager>()?.GameState;
            if (gs == null)
            {
                label.text = "Secrets: 0 / 5";
                return;
            }
            label.text = $"Secrets: {gs.secretsCollected} / {gs.secretsTotal}";
        }
    }
}
