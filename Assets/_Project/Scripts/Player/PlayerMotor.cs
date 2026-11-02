using BangkokLastSurvivor.Input;
using UnityEngine;

namespace BangkokLastSurvivor.Player
{
    public enum MovementState
    {
        Idle,
        Walking,
        Running,
        Crouching,
        CrouchWalking,
        Airborne,
        Dead
    }

    /// <summary>
    /// CharacterController-based movement: camera-relative walk/run, crouch with ceiling check, gravity.
    /// Transform pivot is at the character's feet.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMotor : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerStats stats;
        [Tooltip("Movement is relative to this transform's yaw. Defaults to Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Speeds (m/s)")]
        [SerializeField] private float walkSpeed = 3.2f;
        [SerializeField] private float runSpeed = 6.2f;
        [SerializeField] private float crouchSpeed = 1.6f;
        [SerializeField] private float acceleration = 18f;
        [SerializeField] private float deceleration = 22f;
        [Tooltip("Degrees per second the character turns to face the move direction.")]
        [SerializeField] private float turnSpeed = 720f;

        [Header("Gravity")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundedStickVelocity = -2f;

        [Header("Crouch")]
        [SerializeField] private float standingHeight = 1.8f;
        [SerializeField] private float crouchHeight = 1.1f;
        [Tooltip("How fast the capsule height changes (m/s).")]
        [SerializeField] private float crouchTransitionSpeed = 6f;
        [SerializeField] private LayerMask ceilingMask = ~0;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private bool _crouchToggled;
        private readonly Collider[] _overlapBuffer = new Collider[8];

        public bool IsCrouching { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsGrounded => _controller != null && _controller.isGrounded;
        /// <summary>Current horizontal speed in m/s.</summary>
        public float CurrentSpeed => _horizontalVelocity.magnitude;
        public float WalkSpeed => walkSpeed;
        public float RunSpeed => runSpeed;
        public MovementState State { get; private set; }
        /// <summary>0 = standing, 1 = fully crouched (for visuals/animation).</summary>
        public float CrouchBlend { get; private set; }
        /// <summary>Set false to freeze player control (death, cutscenes, menus).</summary>
        public bool ControlEnabled { get; set; } = true;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            ApplyHeight(standingHeight);
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            bool alive = stats == null || !stats.IsDead;
            bool canControl = alive && ControlEnabled && input != null;

            Vector2 moveInput = canControl ? input.Move : Vector2.zero;

            UpdateCrouch(canControl, dt);

            // Camera-relative direction on the ground plane.
            Vector3 forward = Vector3.forward;
            Vector3 right = Vector3.right;
            if (cameraTransform != null)
            {
                forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
                if (forward.sqrMagnitude < 0.0001f)
                    forward = Vector3.ProjectOnPlane(cameraTransform.up, Vector3.up);
                forward.Normalize();
                right = Vector3.Cross(Vector3.up, forward);
            }
            Vector3 desiredDir = forward * moveInput.y + right * moveInput.x;
            float inputMagnitude = Mathf.Clamp01(desiredDir.magnitude);
            if (inputMagnitude > 0.001f) desiredDir /= desiredDir.magnitude;

            bool wantsRun = canControl && input.RunHeld && !IsCrouching && inputMagnitude > 0.1f;
            IsRunning = wantsRun && (stats == null || stats.CanRun);

            float targetSpeed = IsCrouching ? crouchSpeed : (IsRunning ? runSpeed : walkSpeed);
            Vector3 targetVelocity = desiredDir * (targetSpeed * inputMagnitude);
            float rate = targetVelocity.sqrMagnitude > _horizontalVelocity.sqrMagnitude ? acceleration : deceleration;
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * dt);

            if (inputMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(desiredDir, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * dt);
            }

            if (_controller.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = groundedStickVelocity;
            _verticalVelocity += gravity * dt;

            Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalVelocity) * dt;
            _controller.Move(motion);

            if (stats != null) stats.TickStamina(IsRunning && CurrentSpeed > 0.5f, dt);

            State = ResolveState(alive);
        }

        private void UpdateCrouch(bool canControl, float dt)
        {
            if (canControl && input.CrouchTogglePressed)
                _crouchToggled = !_crouchToggled;

            bool wantsCrouch = canControl && (_crouchToggled || input.CrouchHeld);

            if (!wantsCrouch && IsCrouching && !CanStandUp())
                wantsCrouch = true; // Blocked by a ceiling: stay down.
            else if (!wantsCrouch)
                _crouchToggled = false;

            IsCrouching = wantsCrouch;

            float targetHeight = IsCrouching ? crouchHeight : standingHeight;
            float height = Mathf.MoveTowards(_controller.height, targetHeight, crouchTransitionSpeed * dt);
            ApplyHeight(height);
            CrouchBlend = Mathf.InverseLerp(standingHeight, crouchHeight, height);
        }

        private void ApplyHeight(float height)
        {
            _controller.height = height;
            _controller.center = new Vector3(0f, height * 0.5f, 0f);
        }

        private bool CanStandUp()
        {
            float radius = _controller.radius * 0.9f;
            Vector3 pos = transform.position;
            Vector3 bottom = pos + Vector3.up * (crouchHeight - radius);
            Vector3 top = pos + Vector3.up * (standingHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, _overlapBuffer, ceilingMask,
                QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                Collider c = _overlapBuffer[i];
                if (c != _controller && !c.transform.IsChildOf(transform))
                    return false;
            }
            return true;
        }

        private MovementState ResolveState(bool alive)
        {
            if (!alive) return MovementState.Dead;
            if (!_controller.isGrounded && _verticalVelocity < -4f) return MovementState.Airborne;
            bool moving = CurrentSpeed > 0.2f;
            if (IsCrouching) return moving ? MovementState.CrouchWalking : MovementState.Crouching;
            if (!moving) return MovementState.Idle;
            return IsRunning ? MovementState.Running : MovementState.Walking;
        }

        /// <summary>Move the player instantly (spawn, respawn, doors, etc.).</summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            _controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _controller.enabled = true;
            _horizontalVelocity = Vector3.zero;
            _verticalVelocity = 0f;
            _crouchToggled = false;
        }

        public void SetCameraTransform(Transform cam) => cameraTransform = cam;
    }
}
