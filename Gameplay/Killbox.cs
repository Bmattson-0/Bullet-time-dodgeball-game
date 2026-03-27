using UnityEngine;

namespace BulletTimeDodgeball.Gameplay
{
    public class Killbox : MonoBehaviour
    {
        [Header("Options")]
        [SerializeField] private bool resetRoundOnActorEscape = false;

        private void OnTriggerEnter(Collider other)
        {
            RoundActor actor = other.GetComponentInParent<RoundActor>();
            if (actor != null && !actor.IsEliminated)
            {
                if (resetRoundOnActorEscape)
                {
                    GameManager.Instance?.ForceResetRound();
                }
                else
                {
                    actor.Eliminate(null);
                }

                return;
            }

            Dodgeball ball = other.GetComponentInParent<Dodgeball>();
            if (ball != null)
            {
                Destroy(ball.gameObject);
            }
        }
    }
}