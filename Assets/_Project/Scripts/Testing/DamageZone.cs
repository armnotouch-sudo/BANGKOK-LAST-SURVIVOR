using BangkokLastSurvivor.Player;
using UnityEngine;

namespace BangkokLastSurvivor.Testing
{
    /// <summary>
    /// Test hazard (e.g. fire / broken electric wire): damages the player every second while inside.
    /// Needs a trigger collider. A kinematic Rigidbody is added so trigger events always fire.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DamageZone : MonoBehaviour
    {
        [SerializeField] private float damagePerSecond = 15f;

        private PlayerStats _occupant;

        private void Reset() => GetComponent<Collider>().isTrigger = true;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (!TryGetComponent(out Rigidbody rb)) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats != null) _occupant = stats;
        }

        private void OnTriggerExit(Collider other)
        {
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats != null && stats == _occupant) _occupant = null;
        }

        private void Update()
        {
            if (_occupant != null) _occupant.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }
}
