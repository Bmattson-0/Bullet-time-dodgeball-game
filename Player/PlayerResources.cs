using UnityEngine;

namespace BulletTimeDodgeball.Player
{
    public class PlayerResources : MonoBehaviour
    {
        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float staminaRegenPerSecond = 20f;
        [SerializeField] private float staminaRegenDelay = 0.7f;

        [Header("Focus (Bullet Time)")]
        [SerializeField] private float maxFocus = 100f;
        [SerializeField] private float focusDrainPerSecond = 30f;
        [SerializeField] private float focusRegenPerSecond = 12f;
        [SerializeField] private float focusRegenDelay = 1.0f;

        public float MaxStamina => maxStamina;
        public float MaxFocus => maxFocus;
        public float CurrentStamina { get; private set; }
        public float CurrentFocus { get; private set; }

        private float staminaRegenBlockTimer;
        private float focusRegenBlockTimer;
        private bool staminaRegenSuppressed;

        public float StaminaNormalized => maxStamina <= 0f ? 0f : CurrentStamina / maxStamina;
        public float FocusNormalized => maxFocus <= 0f ? 0f : CurrentFocus / maxFocus;

        private void Awake()
        {
            CurrentStamina = maxStamina;
            CurrentFocus = maxFocus;
        }

        private void Update()
        {
            TickStaminaRegen();
            TickFocusRegen();
        }

        public bool SpendStamina(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (CurrentStamina < amount)
            {
                return false;
            }

            CurrentStamina -= amount;
            staminaRegenBlockTimer = staminaRegenDelay;
            return true;
        }

        public bool HasStamina(float amount)
        {
            return CurrentStamina >= amount;
        }

        public void SetStaminaRegenSuppressed(bool suppressed)
        {
            staminaRegenSuppressed = suppressed;
        }

        public bool SpendFocusPerSecond(float amountPerSecond)
        {
            float amount = amountPerSecond * Time.unscaledDeltaTime;
            if (amount <= 0f)
            {
                return true;
            }

            if (CurrentFocus <= 0f)
            {
                CurrentFocus = 0f;
                focusRegenBlockTimer = focusRegenDelay;
                return false;
            }

            CurrentFocus = Mathf.Max(0f, CurrentFocus - amount);
            focusRegenBlockTimer = focusRegenDelay;
            return CurrentFocus > 0f;
        }

        private void TickStaminaRegen()
        {
            if (staminaRegenSuppressed)
            {
                return;
            }

            if (staminaRegenBlockTimer > 0f)
            {
                staminaRegenBlockTimer -= Time.deltaTime;
                return;
            }

            if (CurrentStamina >= maxStamina)
            {
                return;
            }

            CurrentStamina = Mathf.Min(maxStamina, CurrentStamina + staminaRegenPerSecond * Time.deltaTime);
        }

        private void TickFocusRegen()
        {
            if (focusRegenBlockTimer > 0f)
            {
                focusRegenBlockTimer -= Time.unscaledDeltaTime;
                return;
            }

            if (CurrentFocus >= maxFocus)
            {
                return;
            }

            CurrentFocus = Mathf.Min(maxFocus, CurrentFocus + focusRegenPerSecond * Time.unscaledDeltaTime);
        }
    }
}