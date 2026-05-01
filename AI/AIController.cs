// v0.1
    // Initial prototype version
// v0.2.3
    // Changes:
    // - Replaced CharacterController movement with NavMeshAgent movement
    // - Added reachable-ball search using NavMesh path checks
    // - Added NavMesh-based repositioning
    // - Preserved predictive throw logic
    // - Kept light dodge behavior during reposition
    // - Removed ApplyFacing() method from Update()
    // - Adjusted player reference in Start()
    // - Fixed agent Awake() state
// v0.3
    // Changes:
    // - Adding debug text for AI state to see what action AI is performing, this is in response to a significant delay in AI throwing ball at player with clear sight line.

using UnityEngine;
using UnityEngine.AI;

namespace BulletTimeDodgeball.Gameplay
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(RoundActor))]
    public class AIController : MonoBehaviour
    {
        private enum AIState
        {
            AcquireBall,
            Reposition,
            Aim,
            Throw
        }

        [Header("References")]
        [SerializeField] private Transform holdPoint;
        [SerializeField] private Transform aimOrigin;
        [SerializeField] private RoundActor playerActor;

        [Header("Navigation")]
        [SerializeField] private float stoppingDistance = 0.25f;
        [SerializeField] private float ballSearchInterval = 0.35f;
        [SerializeField] private float repathInterval = 0.35f;
        [SerializeField] private float navSampleRadius = 2.0f;

        [Header("Ball Interaction")]
        [SerializeField] private float pickupRange = 2.2f;

        [Header("Positioning")]
        [SerializeField] private float desiredThrowDistance = 11f;
        [SerializeField] private float throwDistanceTolerance = 2.5f;
        [SerializeField] private float strafeOffsetDistance = 4f;
        [SerializeField] private float repositionRefreshInterval = 1.2f;
        [SerializeField] private LayerMask lineOfSightMask = ~0;

        [Header("Throwing")]
        [SerializeField] private float throwForce = 28f;
        [SerializeField] private float aimDuration = 0.55f;
        [SerializeField] private float throwCooldown = 0.8f;
        [SerializeField] private float aimInaccuracyRadius = 0.85f;
        [SerializeField] private float maxPredictionTime = 1.2f;

        [Header("Dodge")]
        [SerializeField] private float dodgeSpeed = 5.5f;
        [SerializeField] private float dodgeCooldown = 2f;
        [SerializeField] private float dodgeDuration = 0.35f;
        [SerializeField] private float dodgeTriggerDistance = 15f;
        [SerializeField] [Range(0f, 1f)] private float dodgeTriggerChancePerCheck = 0.02f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;

        private NavMeshAgent agent;
        private RoundActor actor;

        private AIState currentState = AIState.AcquireBall;
        private Dodgeball heldBall;
        private Dodgeball targetBall;

        private float ballSearchTimer;
        private float repathTimer;
        private float repositionRefreshTimer;
        private float aimTimer;
        private float throwCooldownTimer;
        private float dodgeCooldownTimer;
        private float dodgeTimer;

        private bool isDodging;
        private Vector3 dodgeDirection;

        private Vector3 currentMoveTarget;
        private bool hasMoveTarget;

        private Vector3 lastPlayerPosition;
        private Vector3 estimatedPlayerVelocity;
        private float repositionSideSign = 1f;

        public bool IsHoldingBall => heldBall != null;
        public string DebugStateName => currentState.ToString();
        public bool DebugIsDodging => isDodging;
        public bool DebugHasMoveTarget => hasMoveTarget;
        public bool DebugHoldingBall => heldBall != null;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            actor = GetComponent<RoundActor>();

            agent.enabled = false;

            if (holdPoint == null)
            {
                Debug.LogWarning("AIController: holdPoint is not assigned.");
            }

            if (aimOrigin == null)
            {
                aimOrigin = holdPoint != null ? holdPoint : transform;
            }

            if (playerActor == null)
            {
                RoundActor[] actors = FindObjectsByType<RoundActor>(FindObjectsSortMode.None);
                foreach (RoundActor foundActor in actors)
                {
                    if (foundActor != null && foundActor.IsPlayer)
                    {
                        playerActor = foundActor;
                        break;
                    }
                }
            }

            agent.updateRotation = false;
            agent.stoppingDistance = stoppingDistance;
        }

        void Start()
        {
            if (playerActor == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerActor = player.GetComponent<RoundActor>();
            }
        }

        private void Update()
        {
            if (actor.IsEliminated)
            {
                if (agent.enabled)
                {
                    agent.isStopped = true;
                }
                return;
            }

            if (GameManager.Instance != null && GameManager.Instance.IsRoundInputLocked)
            {
                if (agent.enabled)
                {
                    agent.isStopped = true;
                    agent.ResetPath();
                }
                return;
            }

            if (throwCooldownTimer > 0f)
            {
                throwCooldownTimer -= Time.deltaTime;
            }

            if (dodgeCooldownTimer > 0f)
            {
                dodgeCooldownTimer -= Time.deltaTime;
            }

            if (isDodging)
            {
                dodgeTimer -= Time.deltaTime;
                if (dodgeTimer <= 0f)
                {
                    isDodging = false;
                    if (agent.enabled)
                    {
                        agent.isStopped = false;
                    }
                }
            }

            UpdatePlayerVelocityEstimate();

            switch (currentState)
            {
                case AIState.AcquireBall:
                    TickAcquireBall();
                    break;

                case AIState.Reposition:
                    TickReposition();
                    break;

                case AIState.Aim:
                    TickAim();
                    break;

                case AIState.Throw:
                    TickThrow();
                    break;
            }

            ApplyDodgeMovement();
        }

        private void UpdatePlayerVelocityEstimate()
        {
            if (playerActor == null)
            {
                estimatedPlayerVelocity = Vector3.zero;
                return;
            }

            float dt = Mathf.Max(Time.deltaTime, 0.0001f);
            Vector3 currentPlayerPosition = playerActor.transform.position;
            estimatedPlayerVelocity = (currentPlayerPosition - lastPlayerPosition) / dt;
            lastPlayerPosition = currentPlayerPosition;
        }

        private void TickAcquireBall()
        {
            if (heldBall != null)
            {
                ChangeState(AIState.Reposition);
                return;
            }

            ballSearchTimer -= Time.deltaTime;
            repathTimer -= Time.deltaTime;

            if (targetBall == null || targetBall.State != Dodgeball.BallState.Idle)
            {
                targetBall = null;
            }

            if (ballSearchTimer <= 0f)
            {
                ballSearchTimer = ballSearchInterval;
                targetBall = FindBestReachableIdleBall();
            }

            if (targetBall == null)
            {
                StopNavigation();

                if (playerActor != null)
                {
                    FaceToward(playerActor.transform.position);
                }

                return;
            }

            Vector3 ballTarget = targetBall.transform.position;
            SetMoveTarget(ballTarget);

            if (repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentMoveTarget);
                }
            }

            if (Vector3.Distance(transform.position, targetBall.transform.position) <= pickupRange)
            {
                bool pickedUp = targetBall.TryPickup(holdPoint, actor);
                if (pickedUp)
                {
                    heldBall = targetBall;
                    targetBall = null;
                    ClearMoveTarget();
                    StopNavigation();
                    ChangeState(AIState.Reposition);
                }
            }

            if (playerActor != null)
            {
                FaceToward(playerActor.transform.position);
            }
        }

        private void TickReposition()
        {
            if (heldBall == null)
            {
                ChangeState(AIState.AcquireBall);
                return;
            }

            if (playerActor == null)
            {
                StopNavigation();
                return;
            }

            TryDodge();

            repositionRefreshTimer -= Time.deltaTime;
            repathTimer -= Time.deltaTime;

            if (!hasMoveTarget || repositionRefreshTimer <= 0f)
            {
                repositionRefreshTimer = repositionRefreshInterval;
                currentMoveTarget = CalculateRepositionTarget();
                hasMoveTarget = true;
            }

            if (!isDodging && repathTimer <= 0f)
            {
                repathTimer = repathInterval;
                if (agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.SetDestination(currentMoveTarget);
                }
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerActor.transform.position);
            bool inThrowBand = Mathf.Abs(distanceToPlayer - desiredThrowDistance) <= throwDistanceTolerance;
            bool hasLineOfSight = HasLineOfSightToPlayer();

            if (!isDodging && inThrowBand && hasLineOfSight && HasArrived())
            {
                ClearMoveTarget();
                StopNavigation();
                ChangeState(AIState.Aim);
                return;
            }

            FaceToward(playerActor.transform.position);
        }

        private void TickAim()
        {
            if (heldBall == null)
            {
                ChangeState(AIState.AcquireBall);
                return;
            }

            if (playerActor == null)
            {
                ChangeState(AIState.Reposition);
                return;
            }

            if (!HasLineOfSightToPlayer())
            {
                ChangeState(AIState.Reposition);
                return;
            }

            StopNavigation();

            Vector3 predictedTarget = GetPredictedPlayerPosition();
            FaceToward(predictedTarget);

            aimTimer += Time.deltaTime;
            if (aimTimer >= aimDuration)
            {
                ChangeState(AIState.Throw);
            }
        }

        private void TickThrow()
        {
            if (heldBall == null)
            {
                ChangeState(AIState.AcquireBall);
                return;
            }

            if (playerActor == null)
            {
                ChangeState(AIState.Reposition);
                return;
            }

            StopNavigation();
            FaceToward(playerActor.transform.position);

            if (throwCooldownTimer > 0f)
            {
                return;
            }

            Vector3 predictedTarget = GetPredictedPlayerPosition();
            Vector3 throwDirection = (predictedTarget - GetAimPosition()).normalized;

            if (throwDirection.sqrMagnitude <= 0.0001f)
            {
                throwDirection = transform.forward;
            }

            bool didThrow = heldBall.Throw(throwDirection * throwForce, actor);
            if (didThrow)
            {
                heldBall = null;
                throwCooldownTimer = throwCooldown;
            }

            ChangeState(AIState.AcquireBall);
        }

        private Dodgeball FindBestReachableIdleBall()
        {
            Dodgeball[] balls = FindObjectsByType<Dodgeball>(FindObjectsSortMode.None);
            Dodgeball bestBall = null;
            float bestDistanceSqr = float.MaxValue;

            Vector3 origin = transform.position;
            if (agent.enabled && agent.isOnNavMesh)
            {
                origin = agent.nextPosition;
            }

            NavMeshPath path = new NavMeshPath();

            foreach (Dodgeball ball in balls)
            {
                if (ball == null || ball.State != Dodgeball.BallState.Idle)
                {
                    continue;
                }

                Vector3 target = ball.transform.position;

                if (!NavMesh.SamplePosition(target, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                bool hasPath = NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path);
                if (!hasPath || path.status != NavMeshPathStatus.PathComplete)
                {
                    continue;
                }

                float distanceSqr = (ball.transform.position - transform.position).sqrMagnitude;
                if (distanceSqr < bestDistanceSqr)
                {
                    bestDistanceSqr = distanceSqr;
                    bestBall = ball;
                }
            }

            return bestBall;
        }

        private Vector3 CalculateRepositionTarget()
        {
            if (playerActor == null)
            {
                return transform.position;
            }

            if (Random.value < 0.2f)
            {
                repositionSideSign *= -1f;
            }

            Vector3 toAI = transform.position - playerActor.transform.position;
            toAI.y = 0f;

            if (toAI.sqrMagnitude < 0.01f)
            {
                toAI = transform.right;
            }

            Vector3 radialDirection = toAI.normalized;
            Vector3 sideDirection = Vector3.Cross(Vector3.up, radialDirection).normalized;

            Vector3 desiredBase = playerActor.transform.position + radialDirection * desiredThrowDistance;
            Vector3 strafeOffset = sideDirection * strafeOffsetDistance * repositionSideSign;
            Vector3 rawTarget = desiredBase + strafeOffset;

            if (NavMesh.SamplePosition(rawTarget, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            Vector3 fallback = playerActor.transform.position + radialDirection * desiredThrowDistance;
            if (NavMesh.SamplePosition(fallback, out hit, navSampleRadius, NavMesh.AllAreas))
            {
                return hit.position;
            }

            return transform.position;
        }

        private Vector3 GetPredictedPlayerPosition()
        {
            if (playerActor == null)
            {
                return transform.position + transform.forward * 5f;
            }

            Vector3 aimStart = GetAimPosition();
            Vector3 playerCenter = playerActor.transform.position + Vector3.up * 1.0f;

            float distance = Vector3.Distance(aimStart, playerCenter);
            float predictionTime = throwForce > 0.01f ? distance / throwForce : 0f;
            predictionTime = Mathf.Clamp(predictionTime, 0f, maxPredictionTime);

            Vector3 predicted = playerCenter + estimatedPlayerVelocity * predictionTime;
            Vector2 error = Random.insideUnitCircle * aimInaccuracyRadius;
            predicted += new Vector3(error.x, 0f, error.y);

            return predicted;
        }

        private bool HasLineOfSightToPlayer()
        {
            if (playerActor == null)
            {
                return false;
            }

            Vector3 origin = GetAimPosition();
            Vector3 target = playerActor.transform.position + Vector3.up * 1.0f;
            Vector3 direction = target - origin;
            float distance = direction.magnitude;

            if (distance <= 0.01f)
            {
                return true;
            }

            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, lineOfSightMask, QueryTriggerInteraction.Ignore))
            {
                RoundActor hitActor = hit.collider.GetComponentInParent<RoundActor>();
                return hitActor == playerActor;
            }

            return true;
        }

        private void TryDodge()
        {
            if (playerActor == null) return;
            if (isDodging) return;
            if (dodgeCooldownTimer > 0f) return;

            float distance = Vector3.Distance(transform.position, playerActor.transform.position);
            if (distance > dodgeTriggerDistance) return;

            if (Random.value > dodgeTriggerChancePerCheck) return;

            Vector3 toPlayer = playerActor.transform.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude <= 0.0001f) return;

            Vector3 side = Vector3.Cross(Vector3.up, toPlayer.normalized);
            float sign = Random.value < 0.5f ? -1f : 1f;

            dodgeDirection = side * sign;
            isDodging = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;

            if (agent.enabled)
            {
                agent.isStopped = true;
            }
        }

        private void ApplyDodgeMovement()
        {
            if (!isDodging)
            {
                return;
            }

            Vector3 delta = dodgeDirection * dodgeSpeed * Time.deltaTime;

            if (agent.enabled && agent.isOnNavMesh)
            {
                agent.Move(delta);
            }
            else
            {
                transform.position += delta;
            }

            if (playerActor != null)
            {
                FaceToward(playerActor.transform.position);
            }
        }

        private void FaceToward(Vector3 worldPoint)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                agent.angularSpeed * Time.deltaTime);
        }

        private Vector3 GetAimPosition()
        {
            return aimOrigin != null ? aimOrigin.position : transform.position + Vector3.up * 1.2f;
        }

        private void SetMoveTarget(Vector3 worldPoint)
        {
            currentMoveTarget = worldPoint;
            hasMoveTarget = true;
        }

        private void ClearMoveTarget()
        {
            hasMoveTarget = false;
            currentMoveTarget = transform.position;
        }

        private void StopNavigation()
        {
            if (agent.enabled)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private bool HasArrived()
        {
            if (!agent.enabled || !agent.isOnNavMesh)
            {
                return true;
            }

            if (agent.pathPending)
            {
                return false;
            }

            return agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, 0.15f);
        }

        private void ChangeState(AIState nextState)
        {
            currentState = nextState;
            ballSearchTimer = 0f;
            repathTimer = 0f;

            switch (currentState)
            {
                case AIState.AcquireBall:
                    aimTimer = 0f;
                    break;

                case AIState.Reposition:
                    aimTimer = 0f;
                    repositionRefreshTimer = 0f;
                    break;

                case AIState.Aim:
                    aimTimer = 0f;
                    break;

                case AIState.Throw:
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawDebug)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            if (hasMoveTarget)
            {
                Gizmos.DrawSphere(currentMoveTarget, 0.25f);
                Gizmos.DrawLine(transform.position, currentMoveTarget);
            }

            if (playerActor != null)
            {
                Gizmos.color = Color.red;
                Vector3 aimStart = aimOrigin != null ? aimOrigin.position : transform.position + Vector3.up * 1.2f;
                Vector3 predicted = Application.isPlaying ? GetPredictedPlayerPosition() : playerActor.transform.position + Vector3.up;
                Gizmos.DrawLine(aimStart, predicted);
                Gizmos.DrawSphere(predicted, 0.2f);
            }
        }
    }
}
