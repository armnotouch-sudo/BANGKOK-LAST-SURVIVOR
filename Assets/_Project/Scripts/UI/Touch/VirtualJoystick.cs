using BangkokLastSurvivor.Input;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BangkokLastSurvivor.UI.Touch
{
    /// <summary>
    /// Floating on-screen joystick. Put it on a large transparent touch zone (e.g. left half of the screen);
    /// the joystick base jumps to where the finger lands. Pushing past <see cref="runThreshold"/> also runs.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private PlayerInputReader input;
        [Tooltip("Visual base of the stick (child of this zone).")]
        [SerializeField] private RectTransform stickBase;
        [SerializeField] private RectTransform stickKnob;
        [Tooltip("Max knob travel in canvas units.")]
        [SerializeField] private float radius = 110f;
        [Range(0f, 0.5f)]
        [SerializeField] private float deadZone = 0.12f;
        [Tooltip("Push the stick beyond this to run automatically. Set > 1 to disable.")]
        [SerializeField] private float runThreshold = 0.97f;

        private RectTransform _zone;
        private Vector2 _baseRestPos;
        private int _pointerId = int.MinValue;
        private bool _autoRun;

        public void Configure(PlayerInputReader reader, RectTransform baseRect, RectTransform knob)
        {
            input = reader; stickBase = baseRect; stickKnob = knob;
        }

        private void Awake()
        {
            _zone = (RectTransform)transform;
            if (stickBase != null) _baseRestPos = stickBase.anchoredPosition;
        }

        private void OnDisable() => Release();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pointerId != int.MinValue) return;
            _pointerId = eventData.pointerId;
            if (stickBase != null &&
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)stickBase.parent,
                    eventData.position, eventData.pressEventCamera, out Vector2 local))
            {
                stickBase.localPosition = local;
            }
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId || stickBase == null) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(stickBase, eventData.position,
                    eventData.pressEventCamera, out Vector2 local))
                return;

            Vector2 offset = Vector2.ClampMagnitude(local, radius);
            if (stickKnob != null) stickKnob.anchoredPosition = offset;

            Vector2 value = offset / radius;
            float mag = value.magnitude;
            value = mag < deadZone ? Vector2.zero : value.normalized * Mathf.InverseLerp(deadZone, 1f, mag);

            if (input != null)
            {
                input.SetVirtualMove(value);
                bool run = mag >= runThreshold;
                if (run != _autoRun)
                {
                    _autoRun = run;
                    input.SetVirtualStickRun(run);
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId == _pointerId) Release();
        }

        private void Release()
        {
            _pointerId = int.MinValue;
            if (stickKnob != null) stickKnob.anchoredPosition = Vector2.zero;
            if (stickBase != null) stickBase.anchoredPosition = _baseRestPos;
            if (input != null)
            {
                input.SetVirtualMove(Vector2.zero);
                if (_autoRun) input.SetVirtualStickRun(false);
            }
            _autoRun = false;
        }
    }
}
