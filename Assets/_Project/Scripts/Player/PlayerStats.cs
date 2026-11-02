using System;
using UnityEngine;

namespace BangkokLastSurvivor.Player
{
    /// <summary>
    /// Health and stamina for the player. Other systems (damage, items, hunger later)
    /// should go through the public API here instead of touching the values directly.
    /// </summary>
    public class PlayerStats : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [Tooltip("Stamina used per second while running.")]
        [SerializeField] private float runStaminaCost = 18f;
        [Tooltip("Stamina recovered per second when not running.")]
        [SerializeField] private float staminaRegen = 14f;
        [Tooltip("Seconds after running stops before stamina starts recovering.")]
        [SerializeField] private float staminaRegenDelay = 0.8f;
        [Tooltip("After running out, stamina must recover to this fraction before running again.")]
        [Range(0f, 1f)]
        [SerializeField] private float exhaustedRecoverFraction = 0.25f;

        private float _regenTimer;

        public float MaxHealth => maxHealth;
        public float Health { get; private set; }
        public float MaxStamina => maxStamina;
        public float Stamina { get; private set; }
        public bool IsDead => Health <= 0f;
        public bool IsExhausted { get; private set; }

        public float HealthNormalized => maxHealth > 0f ? Health / maxHealth : 0f;
        public float StaminaNormalized => maxStamina > 0f ? Stamina / maxStamina : 0f;

        /// <summary>(current, max)</summary>
        public event Action<float, float> HealthChanged;
        /// <summary>Amount of damage taken (positive).</summary>
        public event Action<float> Damaged;
        public event Action Died;
        public event Action Revived;

        private void Awake()
        {
            Health = maxHealth;
            Stamina = maxStamina;
        }

        private void Start()
        {
            HealthChanged?.Invoke(Health, maxHealth);
        }

        /// <summary>True if the player currently has enough stamina to run.</summary>
        public bool CanRun => !IsDead && !IsExhausted && Stamina > 0f;

        /// <summary>Called every frame by the motor with whether the player is actually running.</summary>
        public void TickStamina(bool running, float deltaTime)
        {
            if (IsDead) return;

            if (running)
            {
                Stamina = Mathf.Max(0f, Stamina - runStaminaCost * deltaTime);
                _regenTimer = staminaRegenDelay;
                if (Stamina <= 0f) IsExhausted = true;
            }
            else if (_regenTimer > 0f)
            {
                _regenTimer -= deltaTime;
            }
            else
            {
                Stamina = Mathf.Min(maxStamina, Stamina + staminaRegen * deltaTime);
            }

            if (IsExhausted && Stamina >= maxStamina * exhaustedRecoverFraction)
                IsExhausted = false;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Health = Mathf.Max(0f, Health - amount);
            Damaged?.Invoke(amount);
            HealthChanged?.Invoke(Health, maxHealth);
            if (Health <= 0f) Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            Health = Mathf.Min(maxHealth, Health + amount);
            HealthChanged?.Invoke(Health, maxHealth);
        }

        /// <summary>Restore full health/stamina after death (used by respawn).</summary>
        public void ResetStats()
        {
            bool wasDead = IsDead;
            Health = maxHealth;
            Stamina = maxStamina;
            IsExhausted = false;
            _regenTimer = 0f;
            HealthChanged?.Invoke(Health, maxHealth);
            if (wasDead) Revived?.Invoke();
        }
    }
}
