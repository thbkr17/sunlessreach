using System.Collections;
using UnityEngine;
using SunlessReach.Combat;
using SunlessReach.Core;
using SunlessReach.Player;

namespace SunlessReach.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Damageable))]
    public abstract class BossBase : MonoBehaviour
    {
        [SerializeField] protected int maxHealth = 30;
        [SerializeField] protected float encounterRange = 16f;

        public virtual string BossName => "BOSS";

        // True while the boss fight is actually active (player in the arena, boss alive).
        public bool IsEngaged => encountered && !isDead;

        protected Rigidbody rb;
        protected Damageable damageable;
        protected Transform playerTransform;
        protected int currentPhase = 1;
        protected bool isDead;
        protected bool encountered;

        private Collider _bodyCollider;
        private bool _passThroughSet;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            damageable = GetComponent<Damageable>();
            _bodyCollider = GetComponent<Collider>();
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            damageable.Initialize(maxHealth);

            var hurtbox = GetComponentInChildren<EnemyHurtbox>(true);
            if (hurtbox != null)
                hurtbox.SetContactDamage(0); // walking into the boss does no damage - only its attacks hurt
        }

        protected virtual void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
                SetupPlayerPassThrough();
            }
        }

        // You walk through the boss body (so you can't get wedged); its attacks still hit and yours still land.
        private void SetupPlayerPassThrough()
        {
            if (_passThroughSet || playerTransform == null || _bodyCollider == null) return;
            var pc = playerTransform.GetComponent<Collider>();
            if (pc == null) pc = playerTransform.GetComponentInChildren<Collider>();
            if (pc == null) return;
            Physics.IgnoreCollision(_bodyCollider, pc, true);
            _passThroughSet = true;
        }

        protected virtual void OnEnable()
        {
            damageable.OnDamaged += OnDamaged;
            damageable.OnDeath += OnDeath;
            EventBus.OnPlayerRespawned += OnPlayerRespawned;
            StartCoroutine(WaitForEncounter());
        }

        protected virtual void OnDisable()
        {
            damageable.OnDamaged -= OnDamaged;
            damageable.OnDeath -= OnDeath;
            EventBus.OnPlayerRespawned -= OnPlayerRespawned;
        }

        // Respawned: re-arm the encounter so walking back in restarts the fight.
        private void OnPlayerRespawned()
        {
            if (isDead || !encountered) return;
            encountered = false;   // the old WaitForEncounter already exited; safe to start a fresh one

            damageable.Initialize(maxHealth);
            currentPhase = 1;
            OnFightReset();
            EventBus.RaiseBossHealthChanged(maxHealth, maxHealth);

            StartCoroutine(WaitForEncounter());
        }

        // Subclasses reset phase-dependent state here.
        protected virtual void OnFightReset() { }

        private IEnumerator WaitForEncounter()
        {
            while (!isDead && playerTransform == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) { playerTransform = p.transform; SetupPlayerPassThrough(); }
                yield return new WaitForSeconds(0.2f);
            }
            SetupPlayerPassThrough();
            while (!isDead && !encountered)
            {
                if (playerTransform != null && DistanceToPlayer() < encounterRange)
                {
                    encountered = true;
                    EventBus.RaiseBossEncountered(BossName, maxHealth);
                    EventBus.RaiseBossHealthChanged(damageable.CurrentHealth, maxHealth);
                    yield break;
                }
                yield return new WaitForSeconds(0.3f);
            }
        }

        protected virtual void OnDamaged(int damage, Vector3 dir, float force)
        {
            if (!encountered)
            {
                encountered = true;
                EventBus.RaiseBossEncountered(BossName, maxHealth);
            }
            EventBus.RaiseBossHealthChanged(damageable.CurrentHealth, maxHealth);

            float hpPercent = (float)damageable.CurrentHealth / maxHealth;

            if (hpPercent <= 0.3f && currentPhase < 3)
            {
                currentPhase = 3;
                OnPhaseChange(3);
                StartCoroutine(PhaseTransitionSpectacle(3));
            }
            else if (hpPercent <= 0.6f && currentPhase < 2)
            {
                currentPhase = 2;
                OnPhaseChange(2);
                StartCoroutine(PhaseTransitionSpectacle(2));
            }

            EventBus.RaiseEnemyDamaged(transform.position);
        }

        protected virtual IEnumerator PhaseTransitionSpectacle(int phase)
        {
            damageable.SetInvulnerable(1.2f);
            EventBus.RaiseScreenShake(0.7f, 0.45f);
            EventBus.RaiseBossPhaseChanged(phase);
            yield return null;
        }

        protected virtual void OnDeath()
        {
            isDead = true;
            rb.linearVelocity = Vector3.zero;
            EventBus.RaiseScreenFlash(new Color(1f, 1f, 1f, 0.85f), 0.9f);
            EventBus.RaiseScreenShake(1.0f, 0.6f);
            EventBus.RaiseBossDefeated();
        }

        protected abstract void OnPhaseChange(int newPhase);

        protected float DistanceToPlayer()
        {
            if (playerTransform == null) return float.MaxValue;
            return Vector3.Distance(transform.position, playerTransform.position);
        }

        protected Vector3 DirectionToPlayer()
        {
            if (playerTransform == null) return Vector3.zero;
            Vector3 dir = (playerTransform.position - transform.position);
            dir.z = 0;
            return dir.normalized;
        }

        protected void FacePlayer()
        {
            if (playerTransform == null) return;
            // Track facing without flipping scale (BoxColliders can't be mirrored).
        }
    }
}
