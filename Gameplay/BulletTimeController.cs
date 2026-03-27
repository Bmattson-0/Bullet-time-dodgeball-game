// v0.1
// Initial prototype version
using BulletTimeDodgeball.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletTimeDodgeball.Gameplay
{
    public class BulletTimeController : MonoBehaviour
    {
        [SerializeField] private PlayerResources playerResources;

        [Header("Time Scale")]
        [SerializeField] [Range(0.05f, 1f)] private float bulletTimeScale = 0.25f;
        [SerializeField] private float focusDrainPerSecond = 30f;

        public bool IsActive { get; private set; }
        public float BulletTimeScale => bulletTimeScale;

        private float baseFixedDeltaTime;

        private void Awake()
        {
            baseFixedDeltaTime = Time.fixedDeltaTime;

            if (playerResources == null)
            {
                playerResources = GetComponent<PlayerResources>();
            }
        }

        private void Update()
        {
            bool togglePressed = Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame;

            // Toggle on key press
            if (togglePressed)
            {
                if (IsActive)
                {
                    DisableBulletTime();
                }
                else
                {
                    // Only enable if we have focus
                    if (playerResources != null && playerResources.FocusNormalized > 0f)
                    {
                        EnableBulletTime();
                    }
                }
            }

            // If active, drain focus continuously
            if (IsActive && playerResources != null)
            {
                bool stillHasFocus = playerResources.SpendFocusPerSecond(focusDrainPerSecond);

                if (!stillHasFocus)
                {
                    DisableBulletTime();
                }
            }
        }

        private void EnableBulletTime()
        {
            if (IsActive)
            {
                return;
            }

            IsActive = true;
            Time.timeScale = bulletTimeScale;
            Time.fixedDeltaTime = baseFixedDeltaTime * bulletTimeScale;
        }

        public void DisableBulletTime()
        {
            if (!IsActive && Time.timeScale == 1f)
            {
                return;
            }

            IsActive = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = baseFixedDeltaTime;
        }

        private void OnDisable()
        {
            DisableBulletTime();
        }
    }
}
