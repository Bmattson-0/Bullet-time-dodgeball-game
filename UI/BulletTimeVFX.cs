// v0.3
// Bullet-time visual overlay controller

using UnityEngine;
using UnityEngine.UI;

namespace BulletTimeDodgeball.Gameplay
{
    public class BulletTimeVFX : MonoBehaviour
    {
        [SerializeField] private BulletTimeController bulletTime;
        [SerializeField] private Image overlay;

        [Header("Visuals")]
        [SerializeField] private float targetAlpha = 0.35f;
        [SerializeField] private float fadeSpeed = 5f;

        private float currentAlpha;

        private void Awake()
        {
            if (overlay != null)
            {
                currentAlpha = overlay.color.a;
            }
        }

        private void Update()
        {
            if (bulletTime == null || overlay == null)
                return;

            float target = bulletTime.IsActive ? targetAlpha : 0f;

            currentAlpha = Mathf.MoveTowards(currentAlpha, target, fadeSpeed * Time.deltaTime);

            Color c = overlay.color;
            c.a = currentAlpha;
            overlay.color = c;
        }
    }
}
