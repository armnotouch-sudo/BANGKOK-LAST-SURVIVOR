using System.Collections.Generic;
using BangkokLastSurvivor.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BangkokLastSurvivor.UI.Touch
{
    /// <summary>
    /// Transparent touch zone for the camera: one-finger horizontal drag rotates,
    /// two-finger pinch zooms. Usually covers the right half of the screen behind the buttons.
    /// </summary>
    public class TouchCameraArea : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Degrees per screen pixel of drag.")]
        [SerializeField] private float rotateSensitivity = 0.25f;
        [Tooltip("Zoom units per screen pixel of pinch.")]
        [SerializeField] private float pinchSensitivity = 0.012f;

        private readonly Dictionary<int, Vector2> _pointers = new Dictionary<int, Vector2>();
        private float _lastPinchDistance;

        public void Configure(PlayerInputReader reader) => input = reader;

        private void OnDisable()
        {
            _pointers.Clear();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointers[eventData.pointerId] = eventData.position;
            if (_pointers.Count == 2) _lastPinchDistance = CurrentPinchDistance();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_pointers.ContainsKey(eventData.pointerId)) return;
            Vector2 previous = _pointers[eventData.pointerId];
            _pointers[eventData.pointerId] = eventData.position;
            if (input == null) return;

            // Normalise for screen density so feel is similar on phones and tablets.
            float dpiScale = Screen.dpi > 0f ? 160f / Screen.dpi : 1f;

            if (_pointers.Count == 1)
            {
                input.AddCameraRotate((eventData.position.x - previous.x) * rotateSensitivity * dpiScale);
            }
            else if (_pointers.Count >= 2)
            {
                float distance = CurrentPinchDistance();
                input.AddCameraZoom((distance - _lastPinchDistance) * pinchSensitivity * dpiScale);
                _lastPinchDistance = distance;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _pointers.Remove(eventData.pointerId);
            if (_pointers.Count == 2) _lastPinchDistance = CurrentPinchDistance();
        }

        private float CurrentPinchDistance()
        {
            Vector2 a = default, b = default;
            int i = 0;
            foreach (Vector2 p in _pointers.Values)
            {
                if (i == 0) a = p; else if (i == 1) b = p; else break;
                i++;
            }
            return Vector2.Distance(a, b);
        }
    }
}
