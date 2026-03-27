// v0.2
// Changes:
// - Added simple screen-space debug label for AI state display
// - Toggleable on/off
// - Shows AI state, dodge state, move target state, and ball possession
// - Updated toggle input to use Unity Input System

using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletTimeDodgeball.Gameplay
{
    public class AIDebugLabel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AIController aiController;
        [SerializeField] private Transform labelAnchor;
        [SerializeField] private Camera targetCamera;

        [Header("Display")]
        [SerializeField] private bool showLabel = true;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.2f, 0f);
        [SerializeField] private Key toggleKey = Key.F4;

        private GUIStyle labelStyle;

        private void Awake()
        {
            if (aiController == null)
            {
                aiController = GetComponent<AIController>();
            }

            if (labelAnchor == null)
            {
                labelAnchor = transform;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame)
            {
                showLabel = !showLabel;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        private void OnGUI()
        {
            if (!showLabel || aiController == null || targetCamera == null)
            {
                return;
            }

            Vector3 worldPos = labelAnchor.position + worldOffset;
            Vector3 screenPos = targetCamera.WorldToScreenPoint(worldPos);

            if (screenPos.z <= 0f)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 13,
                    normal = { textColor = Color.white }
                };
            }

            string text =
                $"AI: {aiController.DebugStateName}\n" +
                $"Ball: {(aiController.DebugHoldingBall ? "YES" : "NO")}\n" +
                $"Dodging: {(aiController.DebugIsDodging ? "YES" : "NO")}\n" +
                $"MoveTarget: {(aiController.DebugHasMoveTarget ? "YES" : "NO")}";

            Vector2 size = labelStyle.CalcSize(new GUIContent(text));
            float x = screenPos.x - size.x * 0.5f - 8f;
            float y = Screen.height - screenPos.y - size.y * 0.5f - 8f;

            GUI.Box(new Rect(x, y, size.x + 16f, size.y + 16f), text, labelStyle);
        }
    }
}
