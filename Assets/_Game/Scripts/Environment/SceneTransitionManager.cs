using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace SunlessReach.Environment
{
    public class SceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool _isTransitioning;
        private static Vector3 _spawnPosition;
        private static bool _hasSpawnPosition;

        public static Vector3 SpawnPosition => _spawnPosition;
        public static bool HasSpawnPosition => _hasSpawnPosition;

        public static void ClearSpawnPosition() => _hasSpawnPosition = false;

        public static void SetSpawnPosition(Vector3 pos)
        {
            _spawnPosition = pos;
            _hasSpawnPosition = true;
        }

        public void TransitionToScene(string sceneName, Vector3 spawnPos)
        {
            if (_isTransitioning) return;
            _spawnPosition = spawnPos;
            _hasSpawnPosition = true;
            StartCoroutine(DoTransition(sceneName));
        }

        private IEnumerator DoTransition(string sceneName)
        {
            _isTransitioning = true;

            yield return FadeTo(1f);

            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;

            yield return FadeTo(0f);

            _isTransitioning = false;
        }

        private IEnumerator FadeTo(float targetAlpha)
        {
            if (fadeOverlay == null) yield break;

            float startAlpha = fadeOverlay.alpha;
            float elapsed = 0;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeOverlay.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            fadeOverlay.alpha = targetAlpha;
        }
    }
}
