using UnityEngine;

namespace BangkokLastSurvivor.Core
{
    /// <summary>
    /// Per-scene startup settings (frame rate, screen sleep). Extend with save/load,
    /// settings, etc. in later milestones.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] private int mobileTargetFrameRate = 60;

        private void Awake()
        {
            if (Application.isMobilePlatform)
            {
                Application.targetFrameRate = mobileTargetFrameRate;
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
            }
        }
    }
}
