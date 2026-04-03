// v0.1
// Initial prototype version
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace BulletTimeDodgeball.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Round Reset")]
        [SerializeField] private float roundResetDelay = 1.5f;
        [SerializeField] private Key hardResetKey = Key.F5;

        [Header("Round Timer")]
        [SerializeField] private float roundDurationSeconds = 90f;

        private static int playerScore;
        private static int enemyScore;
        private float roundStartUnscaledTime;
        private bool isResettingRound;

        public static int PlayerScore => playerScore;
        public static int EnemyScore => enemyScore;
        public float RoundDurationSeconds => roundDurationSeconds;
        public float RoundElapsedSeconds => Mathf.Max(0f, Time.unscaledTime - roundStartUnscaledTime);
        public float RoundTimeRemainingSeconds => Mathf.Max(0f, roundDurationSeconds - RoundElapsedSeconds);
        public bool HasRoundTimer => roundDurationSeconds > 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            roundStartUnscaledTime = Time.unscaledTime;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[hardResetKey].wasPressedThisFrame)
            {
                ForceResetRound();
            }

            if (!isResettingRound && HasRoundTimer && RoundTimeRemainingSeconds <= 0f)
            {
                isResettingRound = true;
                StartCoroutine(ResetRoundAfterDelay());
            }
        }

        public void HandleElimination(RoundActor eliminatedActor, RoundActor eliminatedBy)
        {
            if (isResettingRound)
            {
                return;
            }

            if (eliminatedActor != null)
            {
                if (eliminatedActor.IsPlayer)
                {
                    enemyScore++;
                }
                else
                {
                    playerScore++;
                }
            }

            isResettingRound = true;
            StartCoroutine(ResetRoundAfterDelay());
        }

        public void ForceResetRound()
        {
            if (isResettingRound)
            {
                return;
            }

            isResettingRound = true;
            StartCoroutine(ResetRoundImmediate());
        }

        private IEnumerator ResetRoundAfterDelay()
        {
            yield return new WaitForSeconds(roundResetDelay);
            RestoreTimeDefaults();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private IEnumerator ResetRoundImmediate()
        {
            yield return null;
            RestoreTimeDefaults();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void RestoreTimeDefaults()
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
        }
    }
}
