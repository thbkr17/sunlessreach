using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using SunlessReach.Data;
using SunlessReach.Player;
using SunlessReach.Environment;
using SunlessReach.Core;
using SunlessReach.UI;

namespace SunlessReach
{
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private GameState gameState;
        public GameState GameState => gameState;

        private static GameManager _instance;
        private Vector3 _sceneStartPosition;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (GetComponent<CombatFeedback>() == null) gameObject.AddComponent<CombatFeedback>();
            if (GetComponent<BossHealthUI>() == null) gameObject.AddComponent<BossHealthUI>();
            if (GetComponent<Environment.WarpSystem>() == null) gameObject.AddComponent<Environment.WarpSystem>();
            if (GetComponent<UI.FpsCounter>() == null) gameObject.AddComponent<UI.FpsCounter>();
            if (GetComponent<UI.TutorialPrompts>() == null) gameObject.AddComponent<UI.TutorialPrompts>();

            if (gameState == null)
            {
                var all = Resources.FindObjectsOfTypeAll<GameState>();
                if (all.Length > 0) gameState = all[0];
            }
        }

        private static readonly Vector3 CaveSpawnPosition = new Vector3(0f, 2f, 0f);

        private bool _sceneSetupDone;

        private void Start()
        {
            // Fallback for the scene the GameManager is first created in, before its sceneLoaded hook is live.
            if (this == _instance && !_sceneSetupDone) SetupGameScene();
        }

        // Runs on every gameplay-scene load (the persistent GameManager's Start only fires once).
        private void SetupGameScene()
        {
            _sceneSetupDone = true;

            // Kill zone at the bottom of the map (re-created each scene).
            CreateDeathBarrier();

            // Gloomy lighting + vignette, re-applied each scene.
            ApplyGloomyLighting();

            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                Vector3 spawn = SceneTransitionManager.HasSpawnPosition
                    ? SceneTransitionManager.SpawnPosition : CaveSpawnPosition;
                player.transform.position = spawn;
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) { rb.position = spawn; rb.linearVelocity = Vector3.zero; }
                _sceneStartPosition = spawn;
            }
            SceneTransitionManager.ClearSpawnPosition();

            if (gameState != null)
            {
                gameState.enemiesTotal = Mathf.Max(1, FindObjectsByType<Enemies.EnemyBase>(FindObjectsSortMode.None).Length);
                gameState.enemiesDefeated = 0;
            }
        }

        private void OnEnemyKilled(Vector3 _)
        {
            if (gameState != null) gameState.enemiesDefeated++;
        }

        private void OnPlayerDied()
        {
            if (gameState != null) gameState.deathCount++;
        }

        private void Update()
        {
            if (gameState != null && Time.timeScale > 0f)
                gameState.playTime += Time.deltaTime;
        }

        private void ApplyGloomyLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.06f, 0.07f, 0.10f);
            RenderSettings.ambientIntensity = 1f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.04f, 0.05f, 0.08f);
            RenderSettings.fogDensity = 0.018f;

            foreach (var light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                    light.intensity = 0.10f;
            }

            ApplyVignette();
        }

        private void ApplyVignette()
        {
            // Make the main camera render post-processing.
            var cam = Camera.main;
            if (cam != null)
            {
                var camData = cam.GetUniversalAdditionalCameraData();
                if (camData != null) camData.renderPostProcessing = true;
            }

            var volumeObj = new GameObject("GloomVolume");
            var volume = volumeObj.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            volume.weight = 1f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            var vignette = profile.Add<UnityEngine.Rendering.Universal.Vignette>(true);
            vignette.intensity.Override(0.66f);
            vignette.smoothness.Override(0.42f);
            vignette.color.Override(Color.black);
            vignette.rounded.Override(false);
        }

        private void CreateDeathBarrier()
        {
            // Tall black box starting at Y = -30 (below everything), so a fast fall can't tunnel through it.
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "KillZone_Bottom";
            barrier.transform.position = new Vector3(0f, -130f, 0f);
            barrier.transform.localScale = new Vector3(4000f, 200f, 4f);
            barrier.layer = LayerMask.NameToLayer("Hazard");

            var col = barrier.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            var mr = barrier.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.black);
                    if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.black);
                    mr.sharedMaterial = mat;
                }
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }

            barrier.AddComponent<Environment.DeathBarrier>();
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            EventBus.OnEnemyKilled += OnEnemyKilled;
            EventBus.OnPlayerDied += OnPlayerDied;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            EventBus.OnEnemyKilled -= OnEnemyKilled;
            EventBus.OnPlayerDied -= OnPlayerDied;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Only the surviving singleton runs setup.
            if (this != _instance) return;

            if (scene.name == "MainMenu")
            {
                _sceneSetupDone = false;   // re-setup the next gameplay scene we load into
                return;
            }

            _sceneSetupDone = false;
            SetupGameScene();
        }

        public void RespawnPlayer()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var health = player.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    Vector3 spawn = _sceneStartPosition;
                    if (gameState != null && gameState.lastCheckpointPosition != Vector3.zero)
                        spawn = gameState.lastCheckpointPosition;
                    health.Respawn(spawn);
                }
            }
        }
    }
}
