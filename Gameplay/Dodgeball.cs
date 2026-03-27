using UnityEngine;

namespace BulletTimeDodgeball.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Dodgeball : MonoBehaviour
    {
        public enum BallState
        {
            Idle,
            Held,
            Thrown
        }

        [SerializeField] private float heldCollisionDisableSeconds = 0.1f;

        private Rigidbody rb;
        private Collider ballCollider;
        private Transform heldParent;
        private float heldTimer;

        public BallState State { get; private set; } = BallState.Idle;
        public RoundActor Holder { get; private set; }
        public RoundActor LastThrower { get; private set; }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            ballCollider = GetComponent<Collider>();
        }

        private void Update()
        {
            if (heldTimer > 0f)
            {
                heldTimer -= Time.deltaTime;
                if (heldTimer <= 0f && ballCollider != null)
                {
                    ballCollider.enabled = true;
                }
            }
        }

        public bool TryPickup(Transform holdTransform, RoundActor newHolder)
        {
            if (State != BallState.Idle || holdTransform == null || newHolder == null)
            {
                return false;
            }

            Holder = newHolder;
            heldParent = holdTransform;
            State = BallState.Held;

            // Clear physics motion BEFORE making the body kinematic
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;

            transform.SetParent(heldParent);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;

            ballCollider.enabled = false;
            return true;
        }

        public bool Throw(Vector3 throwVelocity, RoundActor thrower)
        {
            if (State != BallState.Held || thrower == null || thrower != Holder)
            {
                return false;
            }

            LastThrower = thrower;
            Holder = null;
            State = BallState.Thrown;

            transform.SetParent(null);
            rb.isKinematic = false;
            rb.linearVelocity = throwVelocity;
            rb.angularVelocity = Vector3.zero;

            heldTimer = heldCollisionDisableSeconds;
            return true;
        }

        private void OnCollisionEnter(Collision collision)
        {
            RoundActor target = collision.collider.GetComponentInParent<RoundActor>();

            if (State == BallState.Thrown && target != null && target != LastThrower && !target.IsEliminated)
            {
                target.Eliminate(LastThrower);
                State = BallState.Idle;
                LastThrower = null;
                return;
            }

            if (State == BallState.Thrown)
            {
                State = BallState.Idle;
                LastThrower = null;
            }
        }
    }
}