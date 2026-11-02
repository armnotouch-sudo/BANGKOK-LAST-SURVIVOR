using BangkokLastSurvivor.Player;
using UnityEngine;

namespace BangkokLastSurvivor.Testing
{
    /// <summary>
    /// Test pickup (first-aid kit): heals on touch, then reappears after a delay.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class HealPickup : MonoBehaviour
    {
        [SerializeField] private float healAmount = 35f;
        [SerializeField] private float respawnSeconds = 8f;
        [SerializeField] private GameObject visual;
        [SerializeField] private float spinSpeed = 90f;

        private float _hiddenUntil = -1f;
        private Collider _collider;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _collider.isTrigger = true;
            if (!TryGetComponent(out Rigidbody rb)) rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            if (visual == null && transform.childCount > 0) visual = transform.GetChild(0).gameObject;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hiddenUntil > Time.time) return;
            PlayerStats stats = other.GetComponentInParent<PlayerStats>();
            if (stats == null || stats.IsDead || stats.Health >= stats.MaxHealth) return;

            stats.Heal(healAmount);
            _hiddenUntil = Time.time + respawnSeconds;
            SetAvailable(false);
        }

        private void Update()
        {
            if (_hiddenUntil > 0f && Time.time >= _hiddenUntil)
            {
                _hiddenUntil = -1f;
                SetAvailable(true);
            }
            if (visual != null) visual.transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void SetAvailable(bool available)
        {
            if (visual != null) visual.SetActive(available);
            _collider.enabled = available;
        }
    }
}
