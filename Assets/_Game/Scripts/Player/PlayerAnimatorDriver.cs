using UnityEngine;

namespace SunlessReach.Player
{
    [RequireComponent(typeof(PlayerController))]
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerAnimatorDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visual;
        [SerializeField] private float turnSpeedDegPerSec = 720f;

        // Re-applied in Awake in case the scene Animator lost its controller/avatar.
        [SerializeField] private RuntimeAnimatorController animatorController;
        [SerializeField] private Avatar animatorAvatar;

        // Model yaw for facing right / left.
        const float YawRight = 90f;
        const float YawLeft  = 270f;

        static readonly int SpeedHash    = Animator.StringToHash("Speed");
        static readonly int GroundedHash = Animator.StringToHash("Grounded");
        // One trigger per direction (a single trigger + a direction int races on the transition).
        static readonly int AttackRHash = Animator.StringToHash("AttackR");
        static readonly int AttackLHash = Animator.StringToHash("AttackL");
        static readonly int AttackUHash = Animator.StringToHash("AttackU");
        static readonly int AttackDHash = Animator.StringToHash("AttackD");

        private PlayerController _ctrl;
        private PlayerCombat _combat;
        private Rigidbody _rb;
        private PlayerState _lastState = PlayerState.Idle;

        private void Awake()
        {
            _ctrl   = GetComponent<PlayerController>();
            _combat = GetComponent<PlayerCombat>();
            _rb     = GetComponent<Rigidbody>();
            if (animator == null) animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {
                if (animator.runtimeAnimatorController == null && animatorController != null)
                    animator.runtimeAnimatorController = animatorController;
                if (animator.avatar == null && animatorAvatar != null)
                    animator.avatar = animatorAvatar;
            }
        }

        private void Update()
        {
            if (animator != null && _rb != null)
            {
                animator.SetFloat(SpeedHash, Mathf.Abs(_rb.linearVelocity.x));
                animator.SetBool(GroundedHash, _ctrl.IsGrounded);

                // Fire the directional attack trigger once when we enter Attacking.
                if (_ctrl.CurrentState == PlayerState.Attacking && _lastState != PlayerState.Attacking)
                {
                    animator.ResetTrigger(AttackRHash);
                    animator.ResetTrigger(AttackLHash);
                    animator.ResetTrigger(AttackUHash);
                    animator.ResetTrigger(AttackDHash);
                    int hash = AttackRHash;
                    if (_combat != null)
                    {
                        switch (_combat.LastAttackDirection)
                        {
                            case AttackDirection.Right: hash = AttackRHash; break;
                            case AttackDirection.Left:  hash = AttackLHash; break;
                            case AttackDirection.Up:    hash = AttackUHash; break;
                            case AttackDirection.Down:  hash = AttackDHash; break;
                        }
                    }
                    animator.SetTrigger(hash);
                }
                _lastState = _ctrl.CurrentState;
            }

            if (visual != null)
            {
                float target = _ctrl.FacingDirection >= 0 ? YawRight : YawLeft;
                var euler = visual.localRotation.eulerAngles;
                float newY = Mathf.MoveTowardsAngle(euler.y, target, turnSpeedDegPerSec * Time.deltaTime);
                visual.localRotation = Quaternion.Euler(0f, newY, 0f);
            }
        }
    }
}
