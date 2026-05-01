// v0.1
// Initial prototype version
// v0.3
// Changes:
// - Moved HUD from top-left debug panel to below the crosshair
// - Added color-coded bars for stamina, focus, and charge
// - Added cleaner transparent styling
// - Kept bullet time / holding ball state text for debugging
// v0.4
// Changes:
// - Lightweight gameplay HUD with resources, player speed, and centered crosshair
// - Added top scoreboard (player vs enemy) and round timer

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
        [SerializeField] private GameManager gameManager;

        [Header("Display")]
        [SerializeField] private bool showHud = true;
        [SerializeField] private bool showCrosshair = true;
        [SerializeField] private bool showScoreboard = true;

        [Header("Layout")]
        [SerializeField] private float barWidth = 120f;
        [SerializeField] private float barHeight = 10f;
        [SerializeField] private float barSpacing = 8f;
        [SerializeField] private float hudOffsetBelowCrosshair = 28f;

        private GUIStyle labelStyle;
        private GUIStyle centerInfoStyle;
        private GUIStyle topInfoStyle;
        private GUIStyle centerAnnouncementStyle;

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

            if (gameManager == null)
            {
                gameManager = FindFirstObjectByType<GameManager>();
            }

            staminaTexture = MakeTexture(new Color(0.2f, 0.5f, 1f, 0.9f));
            focusTexture = MakeTexture(new Color(0.2f, 0.85f, 0.35f, 0.9f));
            chargeTexture = MakeTexture(new Color(1f, 0.55f, 0.15f, 0.9f));
            bgTexture = MakeTexture(new Color(0f, 0f, 0f, 0.4f));
            crosshairTexture = MakeTexture(Color.white);
        }

        private void OnGUI()
        {
            if (!showHud)
            {
                return;
            }

            EnsureStyles();

            if (showScoreboard)
            {
                DrawTopScoreboard();
            }

            DrawCenterHud();

            if (showCrosshair)
            {
                DrawCrosshair();
            }

            DrawRoundFlowAnnouncement();
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

            if (centerInfoStyle == null)
            {
                centerInfoStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 12,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.95f) }
                };
            }

            if (topInfoStyle == null)
            {
                topInfoStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.UpperCenter,
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
                };
            }

            if (centerAnnouncementStyle == null)
            {
                centerAnnouncementStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 32,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.98f) }
                };
            }
        }

        private void DrawTopScoreboard()
        {
            float timer = gameManager != null ? gameManager.RoundTimeRemainingSeconds : 0f;
            int minutes = Mathf.FloorToInt(timer / 60f);
            int seconds = Mathf.FloorToInt(timer % 60f);
            string timerText = gameManager != null && gameManager.HasRoundTimer
                ? $"{minutes:00}:{seconds:00}"
                : "--:--";

            string scoreText = $"PLAYER {GameManager.PlayerScore}  -  {GameManager.EnemyScore} ENEMY";

            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 170f, 8f, 340f, 24f), bgTexture);
            GUI.Label(new Rect(Screen.width * 0.5f - 170f, 8f, 340f, 24f), scoreText, topInfoStyle);

            GUI.DrawTexture(new Rect(Screen.width * 0.5f - 55f, 34f, 110f, 22f), bgTexture);
            GUI.Label(new Rect(Screen.width * 0.5f - 55f, 34f, 110f, 22f), timerText, topInfoStyle);
        }

        private void DrawCenterHud()
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

            float speed = playerController != null ? playerController.CurrentSpeed : 0f;
            bool btActive = bulletTimeController != null && bulletTimeController.IsActive;
            string centerInfo = $"SPD {speed:0.0}  |  BT {(btActive ? "ON" : "OFF")}";
            GUI.Label(new Rect(centerX - 120f, startY + (barHeight + barSpacing) * 3f + 4f, 240f, 18f), centerInfo, centerInfoStyle);
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

        private void DrawRoundFlowAnnouncement()
        {
            if (gameManager == null)
            {
                return;
            }

            float centerX = Screen.width * 0.5f;
            float centerY = Screen.height * 0.38f;

            if (gameManager.CurrentRoundFlowState == GameManager.RoundFlowState.Countdown)
            {
                string countdownText = gameManager.CountdownTimeRemainingSeconds > 0f
                    ? gameManager.CountdownDisplayValue.ToString()
                    : "GO!";

                GUI.DrawTexture(new Rect(centerX - 95f, centerY - 32f, 190f, 64f), bgTexture);
                GUI.Label(new Rect(centerX - 95f, centerY - 32f, 190f, 64f), countdownText, centerAnnouncementStyle);
            }
            else if (gameManager.CurrentRoundFlowState == GameManager.RoundFlowState.RoundEnd)
            {
                GUI.DrawTexture(new Rect(centerX - 180f, centerY - 44f, 360f, 90f), bgTexture);
                GUI.Label(new Rect(centerX - 180f, centerY - 44f, 360f, 44f), "ROUND END", centerAnnouncementStyle);
                GUI.Label(new Rect(centerX - 180f, centerY + 2f, 360f, 28f), $"PLAYER {GameManager.PlayerScore} - {GameManager.EnemyScore} ENEMY", topInfoStyle);
            }
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
