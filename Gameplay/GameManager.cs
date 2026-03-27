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

        private bool isResettingRound;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[hardResetKey].wasPressedThisFrame)
            {
                ForceResetRound();
            }
        }

        public void HandleElimination(RoundActor eliminatedActor, RoundActor eliminatedBy)
        {
            if (isResettingRound)
            {
                return;
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
