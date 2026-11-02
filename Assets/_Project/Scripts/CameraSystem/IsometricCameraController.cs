using BangkokLastSurvivor.Input;
using UnityEngine;

namespace BangkokLastSurvivor.CameraSystem
{
    /// <summary>
    /// Isometric-style 3D follow camera: fixed pitch, rotatable yaw, smooth follow and zoom.
    /// Works with perspective (distance zoom) or orthographic (size zoom) cameras.
    /// Put this on the camera GameObject.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.0f, 0f);
        [SerializeField] private PlayerInputReader input;

        [Header("Angles")]
        [Range(20f, 80f)]
        [SerializeField] private float pitch = 50f;
        [SerializeField] private float startYaw = 45f;
        [Tooltip("Higher = snappier yaw rotation.")]
        [SerializeField] private float yawSmoothing = 12f;

        [Header("Zoom")]
        [SerializeField] private float minDistance = 7f;
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private float startDistance = 16f;
        [Tooltip("Distance change per unit of zoom input.")]
        [SerializeField] private float zoomStep = 2.5f;
        [SerializeField] private float zoomSmoothing = 10f;
        [SerializeField] private float minOrthoSize = 4f;
        [SerializeField] private float maxOrthoSize = 16f;

        [Header("Follow")]
        [SerializeField] private float followSmoothTime = 0.12f;

        private Camera _camera;
        private float _targetYaw;
        private float _yaw;
        private float _targetZoom01;
        private float _zoom01;
        private Vector3 _focus;
        private Vector3 _focusVelocity;

        public float Yaw => _yaw;
        /// <summary>0 = fully zoomed in, 1 = fully zoomed out.</summary>
        public float Zoom01 => _zoom01;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _targetYaw = _yaw = startYaw;
            _targetZoom01 = _zoom01 = Mathf.InverseLerp(minDistance, maxDistance, startDistance);
            if (target != null) _focus = target.position + targetOffset;
            Apply();
        }

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap && target != null)
            {
                _focus = target.position + targetOffset;
                _focusVelocity = Vector3.zero;
            }
        }

        public void Rotate(float degrees) => _targetYaw += degrees;

        /// <summary>Positive = zoom in.</summary>
        public void Zoom(float amount)
        {
            float range = Mathf.Max(0.01f, maxDistance - minDistance);
            _targetZoom01 = Mathf.Clamp01(_targetZoom01 - amount * zoomStep / range);
        }

        private void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;

            if (input != null)
            {
                if (Mathf.Abs(input.CameraRotateDelta) > 0f) Rotate(input.CameraRotateDelta);
                if (Mathf.Abs(input.CameraZoomDelta) > 0f) Zoom(input.CameraZoomDelta);
            }

            _yaw = Mathf.LerpAngle(_yaw, _targetYaw, 1f - Mathf.Exp(-yawSmoothing * dt));
            _zoom01 = Mathf.Lerp(_zoom01, _targetZoom01, 1f - Mathf.Exp(-zoomSmoothing * dt));

            if (target != null)
                _focus = Vector3.SmoothDamp(_focus, target.position + targetOffset, ref _focusVelocity,
                    followSmoothTime, Mathf.Infinity, Time.deltaTime);

            Apply();
        }

        private void Apply()
        {
            Quaternion rot = Quaternion.Euler(pitch, _yaw, 0f);
            float distance = Mathf.Lerp(minDistance, maxDistance, _zoom01);
            if (_camera != null && _camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Lerp(minOrthoSize, maxOrthoSize, _zoom01);
                distance = maxDistance; // keep far enough to avoid near-plane clipping
            }
            transform.SetPositionAndRotation(_focus - rot * Vector3.forward * distance, rot);
        }
    }
}
