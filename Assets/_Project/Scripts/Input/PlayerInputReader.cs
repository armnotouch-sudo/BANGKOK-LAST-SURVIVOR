using UnityEngine;
using UnityEngine.InputSystem;

namespace BangkokLastSurvivor.Input
{
    /// <summary>
    /// Single entry point for all player input. Merges keyboard/mouse/gamepad (Input System actions)
    /// with virtual on-screen touch controls, so gameplay code never reads devices directly.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PlayerInputReader : MonoBehaviour
    {
        [Header("Mouse")]
        [Tooltip("Degrees of camera yaw per pixel of right-mouse drag.")]
        [SerializeField] private float mouseRotateSensitivity = 0.25f;
        [Tooltip("Zoom units per mouse wheel notch (120 = one notch on Windows).")]
        [SerializeField] private float mouseZoomSensitivity = 1f / 120f;

        [Header("Keyboard / Gamepad")]
        [Tooltip("Degrees per second when holding Q/E or the right stick.")]
        [SerializeField] private float keyRotateSpeed = 120f;
        [Tooltip("Zoom units per second when holding +/- or D-pad up/down.")]
        [SerializeField] private float keyZoomSpeed = 4f;

        private InputAction _move;
        private InputAction _run;
        private InputAction _crouchToggle;
        private InputAction _crouchHold;
        private InputAction _rotateAxis;
        private InputAction _zoomAxis;
        private InputAction _respawn;
        private InputAction _toggleTouchUI;

        // Virtual (touch) state written by UI widgets.
        private Vector2 _virtualMove;
        private bool _virtualRun;
        private bool _virtualStickRun;
        private bool _virtualCrouchToggleRequested;
        private float _virtualRotateDelta;
        private float _virtualZoomDelta;
        private bool _virtualRespawnRequested;

        /// <summary>Movement input, magnitude 0..1 (x = right, y = forward).</summary>
        public Vector2 Move { get; private set; }
        public bool RunHeld { get; private set; }
        public bool CrouchHeld { get; private set; }
        /// <summary>True for exactly one frame when crouch toggle was pressed.</summary>
        public bool CrouchTogglePressed { get; private set; }
        /// <summary>Camera yaw change this frame, in degrees.</summary>
        public float CameraRotateDelta { get; private set; }
        /// <summary>Camera zoom change this frame. Positive = zoom in.</summary>
        public float CameraZoomDelta { get; private set; }
        public bool RespawnPressed { get; private set; }
        public bool ToggleTouchUIPressed { get; private set; }

        private void Awake()
        {
            _move = new InputAction("Move", InputActionType.Value);
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
            _move.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
            _move.AddBinding("<Gamepad>/leftStick");

            _run = new InputAction("Run", InputActionType.Button);
            _run.AddBinding("<Keyboard>/leftShift");
            _run.AddBinding("<Keyboard>/rightShift");
            _run.AddBinding("<Gamepad>/leftStickPress");
            _run.AddBinding("<Gamepad>/rightTrigger");

            _crouchToggle = new InputAction("CrouchToggle", InputActionType.Button);
            _crouchToggle.AddBinding("<Keyboard>/c");
            _crouchToggle.AddBinding("<Gamepad>/buttonEast");

            _crouchHold = new InputAction("CrouchHold", InputActionType.Button);
            _crouchHold.AddBinding("<Keyboard>/leftCtrl");

            _rotateAxis = new InputAction("RotateAxis", InputActionType.Value);
            _rotateAxis.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/q").With("Positive", "<Keyboard>/e");
            _rotateAxis.AddBinding("<Gamepad>/rightStick/x");

            _zoomAxis = new InputAction("ZoomAxis", InputActionType.Value);
            _zoomAxis.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/minus").With("Positive", "<Keyboard>/equals");
            _zoomAxis.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/numpadMinus").With("Positive", "<Keyboard>/numpadPlus");
            _zoomAxis.AddCompositeBinding("1DAxis")
                .With("Negative", "<Gamepad>/dpad/down").With("Positive", "<Gamepad>/dpad/up");

            _respawn = new InputAction("Respawn", InputActionType.Button);
            _respawn.AddBinding("<Keyboard>/r");
            _respawn.AddBinding("<Gamepad>/start");

            _toggleTouchUI = new InputAction("ToggleTouchUI", InputActionType.Button);
            _toggleTouchUI.AddBinding("<Keyboard>/f1");
        }

        private void OnEnable()
        {
            _move.Enable(); _run.Enable(); _crouchToggle.Enable(); _crouchHold.Enable();
            _rotateAxis.Enable(); _zoomAxis.Enable(); _respawn.Enable(); _toggleTouchUI.Enable();
        }

        private void OnDisable()
        {
            _move.Disable(); _run.Disable(); _crouchToggle.Disable(); _crouchHold.Disable();
            _rotateAxis.Disable(); _zoomAxis.Disable(); _respawn.Disable(); _toggleTouchUI.Disable();
        }

        private void OnDestroy()
        {
            _move.Dispose(); _run.Dispose(); _crouchToggle.Dispose(); _crouchHold.Dispose();
            _rotateAxis.Dispose(); _zoomAxis.Dispose(); _respawn.Dispose(); _toggleTouchUI.Dispose();
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            Vector2 move = _move.ReadValue<Vector2>() + _virtualMove;
            Move = Vector2.ClampMagnitude(move, 1f);

            RunHeld = _run.IsPressed() || _virtualRun || _virtualStickRun;
            CrouchHeld = _crouchHold.IsPressed();
            CrouchTogglePressed = _crouchToggle.WasPressedThisFrame() || _virtualCrouchToggleRequested;
            RespawnPressed = _respawn.WasPressedThisFrame() || _virtualRespawnRequested;
            ToggleTouchUIPressed = _toggleTouchUI.WasPressedThisFrame();

            float rotate = _rotateAxis.ReadValue<float>() * keyRotateSpeed * dt + _virtualRotateDelta;
            float zoom = _zoomAxis.ReadValue<float>() * keyZoomSpeed * dt + _virtualZoomDelta;

            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.rightButton.isPressed)
                    rotate += mouse.delta.ReadValue().x * mouseRotateSensitivity;
                zoom += mouse.scroll.ReadValue().y * mouseZoomSensitivity;
            }

            CameraRotateDelta = rotate;
            CameraZoomDelta = zoom;

            // Virtual one-shot / per-frame values are consumed here.
            _virtualCrouchToggleRequested = false;
            _virtualRespawnRequested = false;
            _virtualRotateDelta = 0f;
            _virtualZoomDelta = 0f;
        }

        // ---- API used by on-screen touch controls ----

        public void SetVirtualMove(Vector2 value) => _virtualMove = Vector2.ClampMagnitude(value, 1f);
        public void SetVirtualRun(bool held) => _virtualRun = held;
        /// <summary>Run triggered by pushing the virtual stick to its edge (kept separate from the run button).</summary>
        public void SetVirtualStickRun(bool held) => _virtualStickRun = held;
        public void RequestCrouchToggle() => _virtualCrouchToggleRequested = true;
        public void RequestRespawn() => _virtualRespawnRequested = true;
        public void AddCameraRotate(float degrees) => _virtualRotateDelta += degrees;
        public void AddCameraZoom(float amount) => _virtualZoomDelta += amount;
    }
}
