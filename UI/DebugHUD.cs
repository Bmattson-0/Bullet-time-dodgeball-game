// v0.1
// Initial prototype version
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

        private GUIStyle labelStyle;
        private Texture2D barTexture;
        private Texture2D bgTexture;

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

            barTexture = MakeTexture(new Color(0.25f, 0.8f, 1f, 0.95f));
            bgTexture = MakeTexture(new Color(0f, 0f, 0f, 0.55f));
        }

        private void OnGUI()
        {
            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 14,
                    normal = { textColor = Color.white }
                };
            }
            
            if (showHud)
            {
                DrawResourceHud();
            }

            if (showCrosshair)
            {
                DrawCrosshair();
            }
        }

        private void DrawResourceHud()
        {
            const float panelX = 16f;
            const float panelY = 16f;
            const float panelWidth = 320f;
            const float panelHeight = 160f;
            const float barWidth = 200f;
            const float barHeight = 14f;

            GUI.DrawTexture(new Rect(panelX, panelY, panelWidth, panelHeight), bgTexture);

            float stamina01 = resources != null ? resources.StaminaNormalized : 0f;
            float focus01 = resources != null ? resources.FocusNormalized : 0f;
            float charge01 = playerController != null ? playerController.ThrowChargeNormalized : 0f;

            float staminaValue = resources != null ? resources.CurrentStamina : 0f;
            float staminaMax = resources != null ? resources.MaxStamina : 0f;
            float focusValue = resources != null ? resources.CurrentFocus : 0f;
            float focusMax = resources != null ? resources.MaxFocus : 0f;

            GUI.Label(new Rect(panelX + 10f, panelY + 10f, 280f, 24f), "Debug HUD", labelStyle);

            DrawBar(panelX + 10f, panelY + 38f, barWidth, barHeight, stamina01);
            GUI.Label(new Rect(panelX + 220f, panelY + 34f, 90f, 24f), $"Stamina {staminaValue:0}/{staminaMax:0}", labelStyle);

            DrawBar(panelX + 10f, panelY + 68f, barWidth, barHeight, focus01);
            GUI.Label(new Rect(panelX + 220f, panelY + 64f, 90f, 24f), $"Focus {focusValue:0}/{focusMax:0}", labelStyle);

            DrawBar(panelX + 10f, panelY + 98f, barWidth, barHeight, charge01);
            GUI.Label(new Rect(panelX + 220f, panelY + 94f, 90f, 24f), $"Charge {(charge01 * 100f):0}%", labelStyle);

            bool btActive = bulletTimeController != null && bulletTimeController.IsActive;
            bool hasBall = playerController != null && playerController.IsHoldingBall;
            GUI.Label(new Rect(panelX + 10f, panelY + 124f, 300f, 24f), $"BulletTime: {(btActive ? "ON" : "OFF")} | HoldingBall: {(hasBall ? "YES" : "NO")}", labelStyle);
        }

        private void DrawBar(float x, float y, float width, float height, float value01)
        {
            GUI.DrawTexture(new Rect(x, y, width, height), bgTexture);
            GUI.DrawTexture(new Rect(x + 1f, y + 1f, (width - 2f) * Mathf.Clamp01(value01), height - 2f), barTexture);
        }

        private static void DrawCrosshair()
        {
            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.5f;
            const float size = 10f;
            const float thickness = 2f;

            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(centerX - thickness * 0.5f, centerY - size, thickness, size * 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(centerX - size, centerY - thickness * 0.5f, size * 2f, thickness), Texture2D.whiteTexture);
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
