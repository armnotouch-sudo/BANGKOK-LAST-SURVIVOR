using BangkokLastSurvivor.Input;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BangkokLastSurvivor.UI.Touch
{
    public enum TouchAction
    {
        /// <summary>Hold to run.</summary>
        Run,
        /// <summary>Tap to toggle crouch.</summary>
        CrouchToggle,
        /// <summary>Tap to respawn after death.</summary>
        Respawn,
        /// <summary>Hold to rotate camera left.</summary>
        RotateLeft,
        /// <summary>Hold to rotate camera right.</summary>
        RotateRight,
        /// <summary>Hold to zoom in.</summary>
        ZoomIn,
        /// <summary>Hold to zoom out.</summary>
        ZoomOut
    }

    /// <summary>
    /// On-screen button that drives <see cref="PlayerInputReader"/>. Supports multi-touch
    /// (each button tracks its own pointer), so you can move and press buttons at the same time.
    /// </summary>
    public class TouchActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private TouchAction action;
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color pressedColor = new Color(1f, 0.85f, 0.3f, 0.7f);
        [SerializeField] private float rotateSpeed = 110f;
        [SerializeField] private float zoomSpeed = 3f;
        [Tooltip("For CrouchToggle: tint as pressed while the player is crouching.")]
        [SerializeField] private BangkokLastSurvivor.Player.PlayerMotor motorForToggleState;

        private int _pointerId = int.MinValue;
        private bool _held;

        public void Configure(PlayerInputReader reader, TouchAction act, Graphic graphic,
            BangkokLastSurvivor.Player.PlayerMotor motor = null)
        {
            input = reader; action = act; targetGraphic = graphic; motorForToggleState = motor;
        }

        private void Awake()
        {
            if (targetGraphic == null) targetGraphic = GetComponent<Graphic>();
        }

        private void OnDisable() => SetHeld(false);

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_held) return;
            _pointerId = eventData.pointerId;
            SetHeld(true);

            if (input == null) return;
            if (action == TouchAction.CrouchToggle) input.RequestCrouchToggle();
            else if (action == TouchAction.Respawn) input.RequestRespawn();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) SetHeld(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            // Releasing a hold button when the finger slides off avoids "stuck" input.
            if (eventData.pointerId == _pointerId && action != TouchAction.Run) SetHeld(false);
        }

        private void SetHeld(bool held)
        {
            _held = held;
            if (!held) _pointerId = int.MinValue;
            if (action == TouchAction.Run && input != null) input.SetVirtualRun(held);
        }

        private void Update()
        {
            if (input != null && _held)
            {
                float dt = Time.unscaledDeltaTime;
                switch (action)
                {
                    case TouchAction.RotateLeft: input.AddCameraRotate(-rotateSpeed * dt); break;
                    case TouchAction.RotateRight: input.AddCameraRotate(rotateSpeed * dt); break;
                    case TouchAction.ZoomIn: input.AddCameraZoom(zoomSpeed * dt); break;
                    case TouchAction.ZoomOut: input.AddCameraZoom(-zoomSpeed * dt); break;
                }
            }

            if (targetGraphic != null)
            {
                bool lit = _held || (action == TouchAction.CrouchToggle && motorForToggleState != null &&
                                     motorForToggleState.IsCrouching);
                targetGraphic.color = lit ? pressedColor : normalColor;
            }
        }
    }
}
