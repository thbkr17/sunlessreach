using UnityEngine;
using SunlessReach.Data;

namespace SunlessReach.Player
{
    public class PlayerHealing : MonoBehaviour
    {
        [SerializeField] private GameState gameState;
        [SerializeField] private float healTime = 0.8f;
        [SerializeField] private AudioClip healClip;        // "Use Item" SFX
        [SerializeField] private float healVolume = 1f;

        [Header("Heal halo FX")]
        [SerializeField] private float haloRadius = 0.7f;
        [SerializeField] private float haloDotSize = 0.35f;
        [SerializeField] private Color haloColor = new Color(0.55f, 0.8f, 1f, 1f);   // light blue
        [SerializeField] private Vector3 haloOffset = new Vector3(0f, 0.7f, 0f);

        [Header("Crouch while healing")]
        [SerializeField] private Transform visualRoot;            // KnightVisual
        [SerializeField] private float crouchScaleY = 0.7f;
        [SerializeField] private float crouchDropY = 0f;

        private PlayerController _controller;
        private PlayerInputHandler _input;
        private Rigidbody _rb;
        private float _healTimer;
        private bool _isHealing;
        private GameObject _haloInstance;
        private Vector3 _visualBaseScale;
        private Vector3 _visualBasePos;

        private void Awake()
        {
            _controller = GetComponent<PlayerController>();
            _input = GetComponent<PlayerInputHandler>();
            if (gameState == null)
                gameState = Resources.FindObjectsOfTypeAll<GameState>()[0];
            _rb = GetComponent<Rigidbody>();
            if (visualRoot == null) visualRoot = transform.Find("KnightVisual");
            if (visualRoot != null)
            {
                _visualBaseScale = visualRoot.localScale;
                _visualBasePos = visualRoot.localPosition;
            }
        }

        private void Update()
        {
            if (_controller.CurrentState == PlayerState.Dead) return;

            if (_input.HealHeld && _controller.IsGrounded && !_isHealing &&
                gameState.currentSouls >= gameState.soulsPerHeal &&
                gameState.currentHearts < gameState.maxHearts)
            {
                StartHealing();
            }

            if (_isHealing)
            {
                if (!_input.HealHeld || !_controller.IsGrounded)
                {
                    CancelHeal();
                    return;
                }

                _rb.linearVelocity = new Vector3(0, _rb.linearVelocity.y, 0);
                _healTimer += Time.deltaTime;

                if (_healTimer >= healTime)
                {
                    CompleteHeal();
                }
            }
        }

        private void StartHealing()
        {
            _isHealing = true;
            _healTimer = 0;
            _controller.ForceState(PlayerState.Healing);
            SpawnHalo();
            ApplyCrouch(true);
        }

        private void CancelHeal()
        {
            _isHealing = false;
            _healTimer = 0;
            _controller.ForceState(PlayerState.Idle);
            ClearHalo();
            ApplyCrouch(false);
        }

        private void CompleteHeal()
        {
            _isHealing = false;
            _healTimer = 0;
            gameState.TryHeal();
            if (healClip != null) Core.UiSfx.Play(healClip, healVolume);
            _controller.ForceState(PlayerState.Idle);
            ClearHalo();
            ApplyCrouch(false);
        }

        private void ApplyCrouch(bool crouch)
        {
            if (visualRoot == null) return;
            if (crouch)
            {
                visualRoot.localScale = new Vector3(_visualBaseScale.x, _visualBaseScale.y * crouchScaleY, _visualBaseScale.z);
                visualRoot.localPosition = _visualBasePos + Vector3.down * crouchDropY;
            }
            else
            {
                visualRoot.localScale = _visualBaseScale;
                visualRoot.localPosition = _visualBasePos;
            }
        }

        // Procedural blue heal halo.

        private void SpawnHalo()
        {
            ClearHalo();

            _haloInstance = new GameObject("HealHalo");
            _haloInstance.transform.SetParent(transform, false);
            _haloInstance.transform.localPosition = haloOffset;

            var ps = _haloInstance.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 1f;
            main.loop = true;
            main.startLifetime = 0.6f;
            main.startSpeed = 0f;
            main.startSize = haloDotSize;
            main.startColor = haloColor;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 200;
            main.playOnAwake = false;

            var emission = ps.emission;
            emission.rateOverTime = 28f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = haloRadius;
            shape.radiusThickness = 0.15f;
            shape.arc = 360f;
            shape.rotation = new Vector3(0f, 0f, 0f);   // ring lies in XY, faces the camera

            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(2f);  // gentle shimmer

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(1f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0.9f)));

            var rend = _haloInstance.GetComponent<ParticleSystemRenderer>();
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.material = GetHaloMaterial();
            rend.sortingOrder = 50;
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            rend.receiveShadows = false;

            ps.Play(true);
        }

        private void ClearHalo()
        {
            if (_haloInstance == null) return;
            _haloInstance.transform.SetParent(null, true);
            var ps = _haloInstance.GetComponent<ParticleSystem>();
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            Destroy(_haloInstance, 1.0f);
            _haloInstance = null;
        }

        private static Material _haloMat;
        private static Material GetHaloMaterial()
        {
            if (_haloMat != null) return _haloMat;
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Particles/Standard Unlit");
            _haloMat = new Material(shader);
            // Additive transparent (URP).
            if (_haloMat.HasProperty("_Surface")) _haloMat.SetFloat("_Surface", 1f);   // transparent
            if (_haloMat.HasProperty("_Blend")) _haloMat.SetFloat("_Blend", 2f);        // additive
            if (_haloMat.HasProperty("_BlendOp")) _haloMat.SetFloat("_BlendOp", 0f);
            if (_haloMat.HasProperty("_SrcBlend")) _haloMat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            if (_haloMat.HasProperty("_DstBlend")) _haloMat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            if (_haloMat.HasProperty("_ZWrite")) _haloMat.SetFloat("_ZWrite", 0f);
            _haloMat.renderQueue = 3000;
            var tex = GetSoftDotTexture();
            if (_haloMat.HasProperty("_BaseMap")) _haloMat.SetTexture("_BaseMap", tex);
            if (_haloMat.HasProperty("_MainTex")) _haloMat.SetTexture("_MainTex", tex);
            return _haloMat;
        }

        private static Texture2D _softDot;
        private static Texture2D GetSoftDotTexture()
        {
            if (_softDot != null) return _softDot;
            const int N = 64;
            _softDot = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[N * N];
            for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float dx = (x + 0.5f) / N - 0.5f, dy = (y + 0.5f) / N - 0.5f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;        // 0 centre .. 1 edge
                float a = Mathf.Clamp01(1f - d);
                a = a * a;                                            // soft falloff
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
            _softDot.SetPixels(px);
            _softDot.Apply();
            return _softDot;
        }
    }
}
