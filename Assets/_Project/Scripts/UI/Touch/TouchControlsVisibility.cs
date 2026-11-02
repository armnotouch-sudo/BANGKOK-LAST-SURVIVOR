using BangkokLastSurvivor.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BangkokLastSurvivor.UI.Touch
{
    /// <summary>
    /// Shows the touch controls on mobile / touchscreen devices and hides them on PC.
    /// Press F1 on PC to toggle them for testing with the mouse.
    /// </summary>
    public class TouchControlsVisibility : MonoBehaviour
    {
        [SerializeField] private GameObject touchControlsRoot;
        [Tooltip("Optional: shown only while touch controls are hidden (e.g. keyboard help).")]
        [SerializeField] private GameObject pcOnlyRoot;
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Force-show in the Editor (useful with the Device Simulator).")]
        [SerializeField] private bool alwaysShowInEditor;

        private void Start()
        {
            bool show = Application.isMobilePlatform || Touchscreen.current != null;
#if UNITY_EDITOR
            show |= alwaysShowInEditor;
#endif
            SetVisible(show);
        }

        private void Update()
        {
            if (input != null && input.ToggleTouchUIPressed && touchControlsRoot != null)
                SetVisible(!touchControlsRoot.activeSelf);
        }

        public void SetVisible(bool visible)
        {
            if (touchControlsRoot != null) touchControlsRoot.SetActive(visible);
            if (pcOnlyRoot != null) pcOnlyRoot.SetActive(!visible);
        }
    }
}
