using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SunlessReach.Core;

namespace SunlessReach.UI
{
    public class CombatFeedback : MonoBehaviour
    {
        private Canvas _flashCanvas;
        private Image _flashImage;
        private Coroutine _hitstopCoroutine;

        private void OnEnable()
        {
            EventBus.OnHitstop += OnHitstop;
            EventBus.OnScreenFlash += OnScreenFlash;
        }

        private void OnDisable()
        {
            EventBus.OnHitstop -= OnHitstop;
            EventBus.OnScreenFlash -= OnScreenFlash;
        }

        private void OnHitstop(float duration)
        {
            if (_hitstopCoroutine != null) StopCoroutine(_hitstopCoroutine);
            _hitstopCoroutine = StartCoroutine(DoHitstop(duration));
        }

        private IEnumerator DoHitstop(float duration)
        {
            float prev = Time.timeScale;
            if (prev <= 0.001f) yield break; // game already paused
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(duration);
            if (Mathf.Approximately(Time.timeScale, 0.05f))
                Time.timeScale = prev;
            _hitstopCoroutine = null;
        }

        private void OnScreenFlash(Color color, float duration)
        {
            EnsureFlashUI();
            StartCoroutine(DoFlash(color, duration));
        }

        private IEnumerator DoFlash(Color color, float duration)
        {
            if (_flashImage == null) yield break;
            float startA = color.a;
            _flashImage.color = color;
            float t = 0f;
            while (t < duration && _flashImage != null)
            {
                t += Time.unscaledDeltaTime;
                float a = Mathf.Lerp(startA, 0f, t / duration);
                _flashImage.color = new Color(color.r, color.g, color.b, a);
                yield return null;
            }
            if (_flashImage != null)
                _flashImage.color = new Color(color.r, color.g, color.b, 0f);
        }

        private void EnsureFlashUI()
        {
            if (_flashImage != null) return;

            var canvasObj = new GameObject("FlashCanvas");
            canvasObj.transform.SetParent(transform, false);
            _flashCanvas = canvasObj.AddComponent<Canvas>();
            _flashCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _flashCanvas.sortingOrder = 1000;
            canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            var imgObj = new GameObject("FlashImage");
            imgObj.transform.SetParent(canvasObj.transform, false);
            var rect = imgObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            _flashImage = imgObj.AddComponent<Image>();
            _flashImage.color = new Color(1f, 1f, 1f, 0f);
            _flashImage.raycastTarget = false;
        }
    }
}
