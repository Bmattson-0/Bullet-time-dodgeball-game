using UnityEngine;

namespace BulletTimeDodgeball.Gameplay
{
    public class RoundActor : MonoBehaviour
    {
        [SerializeField] private bool isPlayer;
        [SerializeField] private bool godMode;

        public bool IsPlayer => isPlayer;
        public bool IsEliminated { get; private set; }
        public bool GodMode => godMode;
        public bool IsInvulnerable => godMode;

        public void SetGodMode(bool enabled)
        {
            godMode = enabled;
        }

        public void Eliminate(RoundActor eliminatedBy)
        {
            if (IsEliminated)
            {
                return;
            }

            if (IsInvulnerable)
            {
                return;
            }

            IsEliminated = true;
            GameManager.Instance?.HandleElimination(this, eliminatedBy);
        }
    }
}