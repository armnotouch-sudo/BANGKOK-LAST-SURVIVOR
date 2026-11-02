using BangkokLastSurvivor.Input;
using UnityEngine;

namespace BangkokLastSurvivor.Player
{
    /// <summary>
    /// Handles death -> respawn for testing. Also respawns if the player falls out of the world.
    /// </summary>
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerStats stats;
        [Tooltip("Respawn location. If empty, the starting position is used.")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float killHeight = -30f;
        [Tooltip("Seconds after death before respawn input is accepted.")]
        [SerializeField] private float respawnDelay = 1f;

        private Vector3 _startPos;
        private Quaternion _startRot;
        private float _deadTime;

        public bool CanRespawn => stats != null && stats.IsDead && Time.time - _deadTime >= respawnDelay;

        private void Awake()
        {
            if (input == null) input = GetComponent<PlayerInputReader>();
            if (motor == null) motor = GetComponent<PlayerMotor>();
            if (stats == null) stats = GetComponent<PlayerStats>();
            _startPos = transform.position;
            _startRot = transform.rotation;
        }

        private void OnEnable()
        {
            if (stats != null) stats.Died += OnDied;
        }

        private void OnDisable()
        {
            if (stats != null) stats.Died -= OnDied;
        }

        private void OnDied() => _deadTime = Time.time;

        private void Update()
        {
            if (transform.position.y < killHeight)
            {
                Respawn();
                return;
            }

            if (CanRespawn && input != null && input.RespawnPressed)
                Respawn();
        }

        public void Respawn()
        {
            Vector3 pos = spawnPoint != null ? spawnPoint.position : _startPos;
            Quaternion rot = spawnPoint != null ? spawnPoint.rotation : _startRot;
            motor.Teleport(pos, rot);
            stats.ResetStats();
        }
    }
}
