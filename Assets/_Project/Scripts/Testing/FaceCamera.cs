using UnityEngine;

namespace BangkokLastSurvivor.Testing
{
    /// <summary>Keeps a world-space label turned toward the main camera (yaw only).</summary>
    public class FaceCamera : MonoBehaviour
    {
        private Transform _cam;

        private void LateUpdate()
        {
            if (_cam == null)
            {
                if (Camera.main == null) return;
                _cam = Camera.main.transform;
            }
            Vector3 fwd = Vector3.ProjectOnPlane(_cam.forward, Vector3.up);
            if (fwd.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }
    }
}
