using BangkokLastSurvivor.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BangkokLastSurvivor.UI
{
    /// <summary>
    /// Thai HUD: health bar, stamina bar, movement state, and death screen.
    /// </summary>
    public class PlayerHUD : MonoBehaviour
    {
        [Header("Source")]
        [SerializeField] private PlayerStats stats;
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerRespawn respawn;

        [Header("Health")]
        [SerializeField] private Image healthFill;
        [SerializeField] private Image healthLagFill;
        [SerializeField] private Text healthText;

        [Header("Stamina")]
        [SerializeField] private Image staminaFill;
        [SerializeField] private Text staminaText;

        [Header("Status")]
        [SerializeField] private Text stateText;
        [SerializeField] private Image damageVignette;
        [SerializeField] private GameObject deathPanel;
        [SerializeField] private Text deathHintText;

        [Header("Colors")]
        [SerializeField] private Color healthHigh = new Color(0.30f, 0.85f, 0.35f);
        [SerializeField] private Color healthLow = new Color(0.90f, 0.20f, 0.15f);
        [SerializeField] private Color staminaNormal = new Color(0.95f, 0.78f, 0.20f);
        [SerializeField] private Color staminaExhausted = new Color(0.55f, 0.45f, 0.30f);

        private float _lagValue = 1f;
        private float _vignette;

        public void Configure(PlayerStats s, PlayerMotor m, PlayerRespawn r,
            Image hpFill, Image hpLag, Text hpText, Image stFill, Text stText,
            Text state, Image vignette, GameObject death, Text deathHint)
        {
            stats = s; motor = m; respawn = r;
            healthFill = hpFill; healthLagFill = hpLag; healthText = hpText;
            staminaFill = stFill; staminaText = stText;
            stateText = state; damageVignette = vignette; deathPanel = death; deathHintText = deathHint;
        }

        private void OnEnable()
        {
            if (stats != null) stats.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (stats != null) stats.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount)
        {
            _vignette = Mathf.Clamp01(_vignette + 0.25f + amount / 50f);
        }

        private void Update()
        {
            if (stats == null) return;
            float dt = Time.unscaledDeltaTime;

            float hp = stats.HealthNormalized;
            if (healthFill != null)
            {
                healthFill.fillAmount = hp;
                healthFill.color = Color.Lerp(healthLow, healthHigh, hp);
            }
            _lagValue = hp > _lagValue ? hp : Mathf.MoveTowards(_lagValue, hp, dt * 0.5f);
            if (healthLagFill != null) healthLagFill.fillAmount = _lagValue;
            if (healthText != null)
                healthText.text = $"เลือด {Mathf.CeilToInt(stats.Health)}/{Mathf.CeilToInt(stats.MaxHealth)}";

            if (staminaFill != null)
            {
                staminaFill.fillAmount = stats.StaminaNormalized;
                staminaFill.color = stats.IsExhausted ? staminaExhausted : staminaNormal;
            }
            if (staminaText != null)
                staminaText.text = stats.IsExhausted ? "เหนื่อยหอบ!" : $"แรง {Mathf.CeilToInt(stats.Stamina)}";

            if (stateText != null && motor != null)
                stateText.text = "สถานะ: " + StateToThai(motor.State);

            _vignette = Mathf.MoveTowards(_vignette, 0f, dt * 1.2f);
            if (damageVignette != null)
            {
                float lowHpPulse = hp < 0.3f && !stats.IsDead
                    ? (0.15f + 0.1f * Mathf.Sin(Time.unscaledTime * 4f)) * (1f - hp / 0.3f)
                    : 0f;
                Color c = damageVignette.color;
                c.a = Mathf.Clamp01(Mathf.Max(_vignette * 0.6f, lowHpPulse));
                damageVignette.color = c;
            }

            if (deathPanel != null && deathPanel.activeSelf != stats.IsDead)
                deathPanel.SetActive(stats.IsDead);
            if (deathHintText != null && stats.IsDead)
                deathHintText.text = respawn != null && respawn.CanRespawn
                    ? "กด R หรือแตะปุ่ม \"เกิดใหม่\" เพื่อเล่นต่อ"
                    : "...";
        }

        public static string StateToThai(MovementState state)
        {
            switch (state)
            {
                case MovementState.Idle: return "ยืน";
                case MovementState.Walking: return "เดิน";
                case MovementState.Running: return "วิ่ง";
                case MovementState.Crouching: return "หมอบ";
                case MovementState.CrouchWalking: return "ย่อง";
                case MovementState.Airborne: return "ตก";
                case MovementState.Dead: return "เสียชีวิต";
                default: return state.ToString();
            }
        }
    }
}
