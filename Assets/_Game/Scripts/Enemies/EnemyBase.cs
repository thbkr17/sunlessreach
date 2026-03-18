using UnityEngine;
using SunlessReach.Data;
using SunlessReach.Combat;
using SunlessReach.Core;

namespace SunlessReach.Enemies
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Damageable))]
    [RequireComponent(typeof(KnockbackHandler))]
    public abstract class EnemyBase : MonoBehaviour
    {
        [SerializeField] protected EnemyData enemyData;
        [SerializeField] protected Transform groundCheck;
        [SerializeField] protected LayerMask groundLayers;

        // Child holding the model; rotated when facing flips. Null = no visual flip.
        [Header("Visual Facing (optional)")]
        [SerializeField] protected Transform visual;
        // Model yaw for facing right / left.
        [SerializeField] protected float visualYawRight = 90f;
        [SerializeField] protected float visualYawLeft  = -90f;

        [Header("Drop Prefabs")]
        [SerializeField] protected GameObject soulPickupPrefab;
        [SerializeField] protected GameObject moneyPickupPrefab;
        [SerializeField] protected int goldDropAmount = 5;

        // Delay before the corpse is destroyed, so the death animation can finish.
        [SerializeField] protected float deathDelay = 0.1f;

        protected Rigidbody rb;
        protected Damageable damageable;
        protected KnockbackHandler knockback;
        protected Transform playerTransform;
        protected bool isGrounded;
        protected int facingDirection = 1;

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody>();
            damageable = GetComponent<Damageable>();
            knockback = GetComponent<KnockbackHandler>();

            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask("Ground", "Platform");
            if (groundCheck == null)
            {
                var gc = transform.Find("GroundCheck");
                if (gc != null) groundCheck = gc;
            }

            if (enemyData != null)
                damageable.Initialize(enemyData.maxHealth);
        }

        protected virtual void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                playerTransform = player.transform;
        }

        protected virtual void OnEnable()
        {
            damageable.OnDamaged += OnDamaged;
            damageable.OnDeath += OnDeath;
        }

        protected virtual void OnDisable()
        {
            damageable.OnDamaged -= OnDamaged;
            damageable.OnDeath -= OnDeath;
        }

        protected virtual void Update()
        {
            CheckGround();
            // Freeze the body while it's dead so the death animation plays in place.
            if (damageable.IsDead)
            {
                var v = rb.linearVelocity;
                v.x = 0f;
                rb.linearVelocity = v;
            }
            else if (!knockback.InKnockback)
            {
                UpdateAI();
            }
            UpdateVisualFacing();
        }

        protected void UpdateVisualFacing()
        {
            if (visual == null) return;
            float yaw = facingDirection >= 0 ? visualYawRight : visualYawLeft;
            visual.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        protected abstract void UpdateAI();

        protected void CheckGround()
        {
            Vector3 origin = groundCheck != null ? groundCheck.position : transform.position;
            isGrounded = Physics.Raycast(origin, Vector3.down, 1.5f, groundLayers);
        }

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

        protected void FaceDirection(float dirX)
        {
            if (dirX > 0.1f) facingDirection = 1;
            else if (dirX < -0.1f) facingDirection = -1;
        }

        protected virtual void OnDamaged(int damage, Vector3 dir, float force)
        {
            EventBus.RaiseEnemyDamaged(transform.position);
        }

        protected virtual void OnDeath()
        {
            EventBus.RaiseEnemyKilled(transform.position);
            SpawnDrops();
            Destroy(gameObject, deathDelay);
        }

        protected void SpawnDrops()
        {
            if (soulPickupPrefab != null)
            {
                var soul = Instantiate(soulPickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                var pickup = soul.GetComponent<Pickups.SoulPickup>();
                if (pickup != null) pickup.SetAmount(1);
            }

            if (moneyPickupPrefab != null)
            {
                var money = Instantiate(moneyPickupPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
                var moneyPickup = money.GetComponent<Pickups.MoneyPickup>();
                if (moneyPickup != null) moneyPickup.SetAmount(goldDropAmount);
            }
        }
    }
}
