using UnityEngine;

namespace BangkokLastSurvivor.Player
{
    /// <summary>
    /// Placeholder procedural animation for the primitive-based player model:
    /// limb swing while moving, crouch pose, and a red flash when damaged.
    /// Replace with <see cref="PlayerAnimatorBridge"/> + a real rigged model later.
    /// </summary>
    public class PlayerProceduralVisual : MonoBehaviour
    {
        [SerializeField] private PlayerMotor motor;
        [SerializeField] private PlayerStats stats;

        [Header("Model parts")]
        [SerializeField] private Transform modelRoot;
        [SerializeField] private Transform torso;
        [SerializeField] private Transform leftArm;
        [SerializeField] private Transform rightArm;
        [SerializeField] private Transform leftLeg;
        [SerializeField] private Transform rightLeg;

        [Header("Animation")]
        [SerializeField] private float strideFrequency = 1.6f;
        [SerializeField] private float maxLegSwing = 38f;
        [SerializeField] private float maxArmSwing = 32f;
        [SerializeField] private float crouchScaleY = 0.62f;
        [SerializeField] private float crouchLean = 18f;
        [SerializeField] private float deathTiltSpeed = 240f;

        [Header("Damage flash")]
        [SerializeField] private Color flashColor = new Color(1f, 0.15f, 0.1f);
        [SerializeField] private float flashDuration = 0.18f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private Renderer[] _renderers;
        private Color[] _baseColors;
        private MaterialPropertyBlock _mpb;
        private float _phase;
        private float _flashTimer;
        private float _deathTilt;

        public void Configure(PlayerMotor m, PlayerStats s, Transform root, Transform torsoT,
            Transform lArm, Transform rArm, Transform lLeg, Transform rLeg)
        {
            motor = m; stats = s; modelRoot = root; torso = torsoT;
            leftArm = lArm; rightArm = rArm; leftLeg = lLeg; rightLeg = rLeg;
        }

        private void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (stats == null) stats = GetComponentInParent<PlayerStats>();
            if (modelRoot == null) modelRoot = transform;

            _mpb = new MaterialPropertyBlock();
            _renderers = modelRoot.GetComponentsInChildren<Renderer>();
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
            {
                Material mat = _renderers[i].sharedMaterial;
                _baseColors[i] = mat != null && mat.HasProperty(BaseColorId) ? mat.GetColor(BaseColorId) : Color.white;
            }
        }

        private void OnEnable()
        {
            if (stats != null) stats.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (stats != null) stats.Damaged -= OnDamaged;
        }

        private void OnDamaged(float amount) => _flashTimer = flashDuration;

        private void LateUpdate()
        {
            if (motor == null) return;
            float dt = Time.deltaTime;

            // Limb swing scales with speed relative to walk speed.
            float speed = motor.CurrentSpeed;
            float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.01f, motor.RunSpeed));
            _phase += dt * strideFrequency * Mathf.PI * 2f * Mathf.Max(0.35f, speed / Mathf.Max(0.01f, motor.WalkSpeed)) * (speed > 0.1f ? 1f : 0f);
            float swing = Mathf.Sin(_phase) * Mathf.Lerp(0.25f, 1f, speed01) * (speed > 0.1f ? 1f : 0f);

            SetPitch(leftLeg, swing * maxLegSwing);
            SetPitch(rightLeg, -swing * maxLegSwing);
            SetPitch(leftArm, -swing * maxArmSwing);
            SetPitch(rightArm, swing * maxArmSwing);

            float crouch = motor.CrouchBlend;
            float runLean = motor.IsRunning ? 10f * speed01 : 0f;
            SetPitch(torso, crouch * crouchLean + runLean);
            modelRoot.localScale = new Vector3(1f, Mathf.Lerp(1f, crouchScaleY, crouch), 1f);

            // Fall over when dead, stand back up on respawn.
            bool dead = stats != null && stats.IsDead;
            _deathTilt = Mathf.MoveTowards(_deathTilt, dead ? 90f : 0f, deathTiltSpeed * dt);
            modelRoot.localRotation = Quaternion.Euler(-_deathTilt, 0f, 0f);

            UpdateFlash(dt);
        }

        private static void SetPitch(Transform t, float degrees)
        {
            if (t != null) t.localRotation = Quaternion.Euler(degrees, 0f, 0f);
        }

        private void UpdateFlash(float dt)
        {
            if (_flashTimer <= 0f && _mpb.isEmpty) return;
            _flashTimer = Mathf.Max(0f, _flashTimer - dt);
            float t = flashDuration > 0f ? _flashTimer / flashDuration : 0f;

            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i] == null) continue;
                if (t <= 0f)
                {
                    _renderers[i].SetPropertyBlock(null);
                }
                else
                {
                    _mpb.SetColor(BaseColorId, Color.Lerp(_baseColors[i], flashColor, t));
                    _renderers[i].SetPropertyBlock(_mpb);
                }
            }
            if (t <= 0f) _mpb.Clear();
        }
    }
}
