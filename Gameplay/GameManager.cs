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
        public enum RoundFlowState
        {
            Countdown,
            InRound,
            RoundEnd
        }

        public static GameManager Instance { get; private set; }

        [Header("Round Reset")]
        [SerializeField] private float roundResetDelay = 1.5f;
        [SerializeField] private Key hardResetKey = Key.F5;

        [Header("Round Timer")]
        [SerializeField] private float roundDurationSeconds = 90f;
        [SerializeField] private float roundStartCountdownSeconds = 3f;
        [SerializeField] private float goDisplaySeconds = 0.5f;
        [SerializeField] private float roundEndCooldownSeconds = 2.5f;

        private static int playerScore;
        private static int enemyScore;
        private float roundStartUnscaledTime;
        private float countdownStartUnscaledTime;
        private bool isResettingRound;
        private RoundFlowState roundFlowState;

        public static int MatchPlayerScore => playerScore;
        public static int MatchEnemyScore => enemyScore;
        public RoundFlowState CurrentRoundFlowState => roundFlowState;
        public bool IsRoundInputLocked => roundFlowState != RoundFlowState.InRound;
        public float CurrentRoundDurationSeconds => roundDurationSeconds;
        public float CurrentRoundElapsedSeconds => Mathf.Max(0f, Time.unscaledTime - roundStartUnscaledTime);
        public float CurrentRoundTimeRemainingSeconds => Mathf.Max(0f, roundDurationSeconds - CurrentRoundElapsedSeconds);
        public bool IsRoundTimerEnabled => roundDurationSeconds > 0f;
        public float CountdownTimeRemainingSeconds => roundStartCountdownSeconds - (Time.unscaledTime - countdownStartUnscaledTime);
        public int CountdownDisplayValue => Mathf.Clamp(Mathf.CeilToInt(CountdownTimeRemainingSeconds), 0, Mathf.CeilToInt(roundStartCountdownSeconds));

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            roundStartUnscaledTime = Time.unscaledTime;
            countdownStartUnscaledTime = Time.unscaledTime;
            roundFlowState = roundStartCountdownSeconds > 0f ? RoundFlowState.Countdown : RoundFlowState.InRound;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[hardResetKey].wasPressedThisFrame)
            {
                ForceResetRound();
            }

            if (roundFlowState == RoundFlowState.Countdown && CountdownTimeRemainingSeconds <= -goDisplaySeconds)
            {
                roundFlowState = RoundFlowState.InRound;
                roundStartUnscaledTime = Time.unscaledTime;
            }

            if (roundFlowState != RoundFlowState.InRound)
            {
                return;
            }

            if (!isResettingRound && IsRoundTimerEnabled && CurrentRoundTimeRemainingSeconds <= 0f)
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

            roundFlowState = RoundFlowState.RoundEnd;
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
            float waitSeconds = Mathf.Max(roundEndCooldownSeconds, roundResetDelay);
            yield return new WaitForSeconds(waitSeconds);
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
