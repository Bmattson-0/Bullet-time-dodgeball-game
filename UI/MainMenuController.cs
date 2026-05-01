using BulletTimeDodgeball.Gameplay;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace BulletTimeDodgeball.UI
{
    public class MainMenuController : MonoBehaviour
    {
        private enum MenuScreen
        {
            Main,
            Pause,
            Settings
        }

        [Header("Scene Flow")]
        [SerializeField] private bool showMainMenuOnBoot = true;
        [SerializeField] private int mainMenuSceneBuildIndex = 0;

        [Header("Audio")]
        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private string masterVolumeParam = "MasterVolume";

        [Header("Gameplay Settings")]
        [SerializeField] private float minLookSensitivity = 0.05f;
        [SerializeField] private float maxLookSensitivity = 0.4f;

        public static bool IsAnyMenuOpen { get; private set; }

        private MenuScreen currentScreen = MenuScreen.Main;
        private MenuScreen previousScreen = MenuScreen.Main;

        private bool gameplayStarted;
        private float masterVolume = 0.8f;
        private float lookSensitivity = 0.14f;
        private Player.PlayerController cachedPlayerController;

        private GUIStyle titleStyle;
        private GUIStyle panelStyle;

        private void Start()
        {
            if (showMainMenuOnBoot)
            {
                OpenMainMenu();
            }
        }

        private void Update()
        {
            if (!gameplayStarted)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsAnyMenuOpen && currentScreen == MenuScreen.Pause)
                {
                    ResumeGameplay();
                }
                else if (!IsAnyMenuOpen)
                {
                    OpenPauseMenu();
                }
            }
        }

        private void OnGUI()
        {
            if (!IsAnyMenuOpen)
            {
                return;
            }

            EnsureStyles();
            DrawBackdrop();

            if (currentScreen == MenuScreen.Settings)
            {
                DrawSettingsMenu();
                return;
            }

            DrawMainOrPauseMenu();
        }

        private void DrawMainOrPauseMenu()
        {
            Rect panelRect = new Rect(Screen.width * 0.5f - 150f, Screen.height * 0.5f - 120f, 300f, 240f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            string title = currentScreen == MenuScreen.Main ? "BULLET TIME DODGEBALL" : "PAUSED";
            GUI.Label(new Rect(panelRect.x, panelRect.y + 12f, panelRect.width, 34f), title, titleStyle);

            if (GUI.Button(new Rect(panelRect.x + 50f, panelRect.y + 60f, 200f, 36f), currentScreen == MenuScreen.Main ? "Play" : "Restart"))
            {
                if (currentScreen == MenuScreen.Main)
                {
                    StartGameplay();
                }
                else
                {
                    ResumeTimeDefaults();
                    SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                }
            }

            if (GUI.Button(new Rect(panelRect.x + 50f, panelRect.y + 106f, 200f, 36f), "Settings"))
            {
                previousScreen = currentScreen;
                currentScreen = MenuScreen.Settings;
            }

            if (GUI.Button(new Rect(panelRect.x + 50f, panelRect.y + 152f, 200f, 36f), "Exit"))
            {
                if (currentScreen == MenuScreen.Main)
                {
                    Application.Quit();
                }
                else
                {
                    ReturnToMainMenu();
                }
            }
        }

        private void DrawSettingsMenu()
        {
            Rect panelRect = new Rect(Screen.width * 0.5f - 180f, Screen.height * 0.5f - 150f, 360f, 300f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(panelRect.x, panelRect.y + 12f, panelRect.width, 34f), "SETTINGS", titleStyle);

            GUI.Label(new Rect(panelRect.x + 28f, panelRect.y + 64f, 304f, 22f), $"Audio Volume: {(masterVolume * 100f):0}%");
            masterVolume = GUI.HorizontalSlider(new Rect(panelRect.x + 28f, panelRect.y + 88f, 304f, 24f), masterVolume, 0f, 1f);
            ApplyVolume(masterVolume);

            GUI.Label(new Rect(panelRect.x + 28f, panelRect.y + 120f, 304f, 22f), $"Look Sensitivity: {lookSensitivity:0.00}");
            lookSensitivity = GUI.HorizontalSlider(new Rect(panelRect.x + 28f, panelRect.y + 144f, 304f, 24f), lookSensitivity, minLookSensitivity, maxLookSensitivity);
            ApplyLookSensitivity();

            GUI.Label(new Rect(panelRect.x + 28f, panelRect.y + 176f, 304f, 22f), $"Video Quality: {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
            if (GUI.Button(new Rect(panelRect.x + 28f, panelRect.y + 200f, 146f, 28f), "Lower Quality"))
            {
                QualitySettings.DecreaseLevel();
            }
            if (GUI.Button(new Rect(panelRect.x + 186f, panelRect.y + 200f, 146f, 28f), "Higher Quality"))
            {
                QualitySettings.IncreaseLevel();
            }

            if (GUI.Button(new Rect(panelRect.x + 80f, panelRect.y + 246f, 200f, 34f), "Back"))
            {
                currentScreen = previousScreen;
            }
        }

        private void OpenMainMenu()
        {
            gameplayStarted = false;
            currentScreen = MenuScreen.Main;
            previousScreen = MenuScreen.Main;
            OpenMenu();
        }

        private void OpenPauseMenu()
        {
            currentScreen = MenuScreen.Pause;
            previousScreen = MenuScreen.Pause;
            OpenMenu();
        }

        private void StartGameplay()
        {
            gameplayStarted = true;
            ResumeGameplay();
        }

        private void ResumeGameplay()
        {
            IsAnyMenuOpen = false;
            ResumeTimeDefaults();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void ReturnToMainMenu()
        {
            ResumeTimeDefaults();

            if (mainMenuSceneBuildIndex >= 0 && mainMenuSceneBuildIndex != SceneManager.GetActiveScene().buildIndex)
            {
                SceneManager.LoadScene(mainMenuSceneBuildIndex);
                return;
            }

            OpenMainMenu();
        }

        private void OpenMenu()
        {
            IsAnyMenuOpen = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void ResumeTimeDefaults()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }

        private void ApplyVolume(float volume01)
        {
            if (masterMixer == null)
            {
                AudioListener.volume = volume01;
                return;
            }

            float db = Mathf.Lerp(-80f, 0f, volume01);
            masterMixer.SetFloat(masterVolumeParam, db);
        }

        private void ApplyLookSensitivity()
        {
            if (cachedPlayerController == null)
            {
                cachedPlayerController = FindFirstObjectByType<Player.PlayerController>();
            }

            if (cachedPlayerController != null)
            {
                cachedPlayerController.SetLookSensitivity(lookSensitivity);
            }
        }

        private void DrawBackdrop()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void EnsureStyles()
        {
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 20,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = Color.white }
                };
            }

            if (panelStyle == null)
            {
                panelStyle = new GUIStyle(GUI.skin.box);
            }
        }
    }
}
