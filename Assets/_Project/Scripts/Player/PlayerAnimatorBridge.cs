using UnityEngine;

namespace BangkokLastSurvivor.Player
{
    /// <summary>
    /// Feeds motor state into an Animator when a rigged character model is added later.
    /// Only parameters that exist on the controller are written, so any controller works.
    /// Expected (optional) parameters: Speed (float), IsCrouching (bool), IsGrounded (bool),
    /// IsRunning (bool), IsDead (bool).
    /// </summary>
    public class PlayerAnimatorBridge : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private float speedDampTime = 0.1f;

        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int CrouchId = Animator.StringToHash("IsCrouching");
        private static readonly int GroundedId = Animator.StringToHash("IsGrounded");
        private static readonly int RunningId = Animator.StringToHash("IsRunning");
        private static readonly int DeadId = Animator.StringToHash("IsDead");

        private bool _hasSpeed, _hasCrouch, _hasGrounded, _hasRunning, _hasDead;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (stats == null) stats = GetComponentInParent<PlayerStats>();
            CacheParameters();
        }

        private void CacheParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == SpeedId && p.type == AnimatorControllerParameterType.Float) _hasSpeed = true;
                else if (p.nameHash == CrouchId && p.type == AnimatorControllerParameterType.Bool) _hasCrouch = true;
                else if (p.nameHash == GroundedId && p.type == AnimatorControllerParameterType.Bool) _hasGrounded = true;
                else if (p.nameHash == RunningId && p.type == AnimatorControllerParameterType.Bool) _hasRunning = true;
                else if (p.nameHash == DeadId && p.type == AnimatorControllerParameterType.Bool) _hasDead = true;
            }
        }

        private void Update()
        {
            if (animator == null || motor == null) return;
            if (_hasSpeed) animator.SetFloat(SpeedId, motor.CurrentSpeed, speedDampTime, Time.deltaTime);
            if (_hasCrouch) animator.SetBool(CrouchId, motor.IsCrouching);
            if (_hasGrounded) animator.SetBool(GroundedId, motor.IsGrounded);
            if (_hasRunning) animator.SetBool(RunningId, motor.IsRunning);
            if (_hasDead) animator.SetBool(DeadId, stats != null && stats.IsDead);
        }
    }
}
