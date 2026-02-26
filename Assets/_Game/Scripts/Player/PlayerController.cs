using UnityEngine;
using SunlessReach.Core;

namespace SunlessReach.Player
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(PlayerInputHandler))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float jumpForce = 10f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.15f;
        [SerializeField] private float fallGravityMultiplier = 1.5f;

        [Header("Wall Jump")]
        [SerializeField] private float wallSlideSpeed = 2f;
        [SerializeField] private float wallJumpForceX = 8f;
        [SerializeField] private float wallJumpForceY = 10f;
        [SerializeField] private float wallJumpLockTime = 0.15f;
        [SerializeField] private float wallCheckDistance = 0.55f;
        [SerializeField] private float wallClingTime = 0.15f;

        [Header("Ground Detection")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private LayerMask groundLayers;
        [SerializeField] private float groundCheckRadius = 0.15f;

        public PlayerState CurrentState { get; private set; } = PlayerState.Idle;
        public bool IsGrounded { get; private set; }
        public bool IsTouchingWall { get; private set; }
        public bool IsWallSliding { get; private set; }
        public int FacingDirection { get; private set; } = 1; // 1 = right, -1 = left
        public int WallDirection { get; private set; } // 1 = wall on right, -1 = wall on left

        private Rigidbody _rb;
        private PlayerInputHandler _input;
        private PlayerAbilities _abilities;
        private PlayerAnimationEvents _animEvents;

        private float _coyoteTimeCounter;
        private float _jumpBufferCounter;
        private bool _hasJumped;
        private float _lockedUntil; // for states that lock movement
        private float _wallClingTimer;
        private bool _wasOnWall;
        private bool _justWallJumped;
        private int _lastWallJumpDir; // tracks which wall was last jumped from

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _input = GetComponent<PlayerInputHandler>();
            _abilities = GetComponent<PlayerAbilities>();
            _animEvents = GetComponentInChildren<PlayerAnimationEvents>();

            // 2D-in-3D: lock Z and rotation.
            _rb.constraints = RigidbodyConstraints.FreezePositionZ |
                              RigidbodyConstraints.FreezeRotation;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

            // Stronger gravity for a snappier jump.
            Physics.gravity = new Vector3(0, -14f, 0);

            // Fallback if groundLayers wasn't serialized.
            if (groundLayers == 0)
                groundLayers = LayerMask.GetMask("Ground", "Platform");

            // Fallback if GroundCheck wasn't assigned.
            if (groundCheck == null)
            {
                var gc = transform.Find("GroundCheck");
                if (gc != null) groundCheck = gc;
            }
        }

        private void Update()
        {
            if (CurrentState == PlayerState.Dead) return;

            CheckGround();
            CheckWall();
            UpdateTimers();
            HandleStateTransitions();
        }

        private void FixedUpdate()
        {
            if (CurrentState == PlayerState.Dead) return;
            if (CurrentState == PlayerState.Dashing) return;
            if (CurrentState == PlayerState.Healing) return;

            // During wall jump lock, skip movement override but still apply gravity/wall slide
            if (Time.time >= _lockedUntil)
            {
                ApplyMovement();
            }

            // Wall cling/slide: freeze briefly then slide down slowly
            if (IsWallSliding)
            {
                var vel = _rb.linearVelocity;
                vel.x = 0; // stop horizontal movement so player doesn't stick to wall
                if (_wallClingTimer > 0)
                    vel.y = 0; // cling: freeze in place
                else
                    vel.y = -wallSlideSpeed; // slide down at fixed speed
                _rb.linearVelocity = vel;
            }

            // Apply extra gravity when falling so the character doesn't float
            if (_rb.linearVelocity.y < 0 && !IsWallSliding)
            {
                _rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }

        private void CheckGround()
        {
            // CheckSphere, not SphereCast - SphereCast ignores colliders it already overlaps.
            Vector3 origin = groundCheck != null ? groundCheck.position : transform.position;
            IsGrounded = Physics.CheckSphere(origin, groundCheckRadius, groundLayers);

            if (IsGrounded)
            {
                _coyoteTimeCounter = coyoteTime;
                _hasJumped = false;
                _justWallJumped = false;
                _lastWallJumpDir = 0;
                if (_abilities != null)
                {
                    _abilities.ResetAirAbilities();
                }
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }
        }

        private void CheckWall()
        {
            Vector3 origin = transform.position + Vector3.up * 0.5f;

            bool wallRight = Physics.Raycast(origin, Vector3.right, wallCheckDistance, groundLayers);
            bool wallLeft = Physics.Raycast(origin, Vector3.left, wallCheckDistance, groundLayers);

            IsTouchingWall = wallRight || wallLeft;
            WallDirection = wallRight ? 1 : wallLeft ? -1 : 0;

            if (_justWallJumped && Time.time >= _lockedUntil)
                _justWallJumped = false;

            float moveX = _input.MoveInput.x;
            bool pushingAway = (WallDirection == 1 && moveX < -0.1f) || (WallDirection == -1 && moveX > 0.1f);
            bool onWallInAir = IsTouchingWall && !IsGrounded && !_justWallJumped && !pushingAway;

            if (onWallInAir && !_wasOnWall)
            {
                _wallClingTimer = wallClingTime;
            }

            if (onWallInAir)
            {
                _wallClingTimer -= Time.deltaTime;
                IsWallSliding = true;
            }
            else
            {
                IsWallSliding = false;
                _wallClingTimer = 0;
            }

            _wasOnWall = onWallInAir;
        }

        private void UpdateTimers()
        {
            if (_input.JumpPressed)
                _jumpBufferCounter = jumpBufferTime;
            else
                _jumpBufferCounter -= Time.deltaTime;
        }

        private void HandleStateTransitions()
        {
            if (Time.time < _lockedUntil) return;

            // Jump buffering
            if (_jumpBufferCounter > 0)
            {
                if (_coyoteTimeCounter > 0 && !_hasJumped)
                {
                    Jump();
                }
                else if ((IsWallSliding || (IsTouchingWall && !IsGrounded)) && WallDirection != _lastWallJumpDir)
                {
                    WallJump();
                }
                else if (_abilities != null && _abilities.CanDoubleJump())
                {
                    _abilities.DoDoubleJump(jumpForce * 0.85f);
                    _jumpBufferCounter = 0;
                    CurrentState = PlayerState.Jumping;
                }
            }

            // Let Attacking fall back to Idle/Running once the lock expires, so the next attack reads as a fresh transition.
            if (CurrentState != PlayerState.Dashing && CurrentState != PlayerState.Healing)
            {
                if (IsGrounded)
                {
                    CurrentState = Mathf.Abs(_input.MoveInput.x) > 0.1f ? PlayerState.Running : PlayerState.Idle;
                }
                else if (IsWallSliding)
                {
                    CurrentState = PlayerState.WallSliding;
                }
                else
                {
                    CurrentState = _rb.linearVelocity.y > 0.1f ? PlayerState.Jumping : PlayerState.Falling;
                }
            }
        }

        private void Jump()
        {
            var vel = _rb.linearVelocity;
            vel.y = jumpForce;
            _rb.linearVelocity = vel;
            _coyoteTimeCounter = 0;
            _jumpBufferCounter = 0;
            _hasJumped = true;
            CurrentState = PlayerState.Jumping;
            _animEvents?.PlayFootstep();
        }

        private void WallJump()
        {
            int awayDir = -WallDirection;
            _rb.linearVelocity = new Vector3(awayDir * wallJumpForceX, wallJumpForceY, 0);
            FacingDirection = awayDir;
            _jumpBufferCounter = 0;
            _hasJumped = true;
            _justWallJumped = true;
            _lastWallJumpDir = WallDirection;
            _lockedUntil = Time.time + wallJumpLockTime; // briefly lock input so player doesn't cancel the jump
            CurrentState = PlayerState.Jumping;
            _animEvents?.PlayFootstep();

            if (_abilities != null)
                _abilities.ResetAirAbilities();
        }

        private void ApplyMovement()
        {
            float moveX = _input.MoveInput.x;
            var vel = _rb.linearVelocity;
            vel.x = moveX * moveSpeed;
            _rb.linearVelocity = vel;

            // Update facing (don't flip root scale - it breaks BoxColliders).
            if (moveX > 0.1f)
                FacingDirection = 1;
            else if (moveX < -0.1f)
                FacingDirection = -1;
        }

        public void LockState(PlayerState state, float duration)
        {
            CurrentState = state;
            _lockedUntil = Time.time + duration;
        }

        public void SetVelocity(Vector3 velocity)
        {
            _rb.linearVelocity = velocity;
        }

        public void ForceState(PlayerState state)
        {
            CurrentState = state;
        }

        public void ApplyKnockback(Vector3 direction, float force)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.AddForce(direction.normalized * force, ForceMode.VelocityChange);
            LockState(PlayerState.TakingDamage, 0.2f);
        }
    }
}
