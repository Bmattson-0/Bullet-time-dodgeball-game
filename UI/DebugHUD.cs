// v0.1
// Initial prototype version
// v0.3
// Changes:
// - Moved HUD from top-left debug panel to below the crosshair
// - Added color-coded bars for stamina, focus, and charge
// - Added cleaner transparent styling
// - Kept bullet time / holding ball state text for debugging

using BulletTimeDodgeball.Gameplay;
using BulletTimeDodgeball.Player;
using UnityEngine;

namespace BulletTimeDodgeball.UI
{
    public class DebugHUD : MonoBehaviour
    {
        [SerializeField] private PlayerResources resources;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private BulletTimeController bulletTimeController;

        [Header("Display")]
        [SerializeField] private bool showHud = true;
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private bool showStatusText = true;

        [Header("Layout")]
        [SerializeField] private float barWidth = 120f;
        [SerializeField] private float barHeight = 10f;
        [SerializeField] private float barSpacing = 8f;
        [SerializeField] private float hudOffsetBelowCrosshair = 28f;

        private GUIStyle labelStyle;
        private GUIStyle statusStyle;

        private Texture2D staminaTexture;
        private Texture2D focusTexture;
        private Texture2D chargeTexture;
        private Texture2D bgTexture;
        private Texture2D crosshairTexture;

        private void Awake()
        {
            if (resources == null)
            {
                resources = FindFirstObjectByType<PlayerResources>();
            }

            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
            }

            if (bulletTimeController == null)
            {
                bulletTimeController = FindFirstObjectByType<BulletTimeController>();
            }

            staminaTexture = MakeTexture(new Color(0.2f, 0.5f, 1f, 0.9f));   // blue
            focusTexture = MakeTexture(new Color(0.2f, 0.85f, 0.35f, 0.9f)); // green
            chargeTexture = MakeTexture(new Color(1f, 0.55f, 0.15f, 0.9f));  // orange
            bgTexture = MakeTexture(new Color(0f, 0f, 0f, 0.4f));
            crosshairTexture = MakeTexture(Color.white);
        }

        private void OnGUI()
        {
            EnsureStyles();

            if (showCrosshair)
            {
                DrawCrosshair();
            }

            if (showHud)
            {
                DrawCrosshairHud();
            }
        }

        private void EnsureStyles()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 11,
                    normal = { textColor = Color.white }
                };
            }

            if (statusStyle == null)
            {
                statusStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.9f) }
                };
            }
        }

        private void DrawCrosshairHud()
        {
            float stamina01 = resources != null ? resources.StaminaNormalized : 0f;
            float focus01 = resources != null ? resources.FocusNormalized : 0f;
            float charge01 = playerController != null ? playerController.ThrowChargeNormalized : 0f;

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;

            float startY = centerY + hudOffsetBelowCrosshair;
            float leftX = centerX - barWidth * 0.5f;

            DrawLabeledBar(leftX, startY, barWidth, barHeight, stamina01, staminaTexture, "STAMINA");
            DrawLabeledBar(leftX, startY + (barHeight + barSpacing), barWidth, barHeight, focus01, focusTexture, "FOCUS");
            DrawLabeledBar(leftX, startY + (barHeight + barSpacing) * 2f, barWidth, barHeight, charge01, chargeTexture, "CHARGE");

            if (showStatusText)
            {
                bool btActive = bulletTimeController != null && bulletTimeController.IsActive;
                bool hasBall = playerController != null && playerController.IsHoldingBall;
                bool isDodging = playerController != null && playerController.IsDodging;

                string status = $"BT {(btActive ? "ON" : "OFF")}  |  BALL {(hasBall ? "YES" : "NO")}  |  DODGE {(isDodging ? "YES" : "NO")}";
                GUI.Label(
                    new Rect(centerX - 120f, startY + (barHeight + barSpacing) * 3f + 4f, 240f, 18f),
                    status,
                    statusStyle);
            }
        }

        private void DrawLabeledBar(float x, float y, float width, float height, float value01, Texture2D fillTexture, string label)
        {
            GUI.DrawTexture(new Rect(x, y, width, height), bgTexture);
            GUI.DrawTexture(new Rect(x + 1f, y + 1f, (width - 2f) * Mathf.Clamp01(value01), height - 2f), fillTexture);
            GUI.Label(new Rect(x, y - 14f, width, 14f), label, labelStyle);
        }

        private void DrawCrosshair()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            const float size = 10f;
            const float thickness = 2f;

            GUI.color = new Color(1f, 1f, 1f, 0.95f);
            GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - size, thickness, size * 2f), crosshairTexture);
            GUI.DrawTexture(new Rect(centerX - size, centerY - thickness * 0.5f, size * 2f, thickness), crosshairTexture);
            GUI.color = Color.white;
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
