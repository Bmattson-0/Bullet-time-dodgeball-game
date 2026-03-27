using BulletTimeDodgeball.Player;
using UnityEngine;

namespace BulletTimeDodgeball.Gameplay
{
    [RequireComponent(typeof(CharacterController))]
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

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6.5f;
        [SerializeField] private float acceleration = 24f;
        [SerializeField] private float rotationSpeed = 540f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float stoppingDistance = 0.25f;

        [Header("Ball Interaction")]
        [SerializeField] private float pickupRange = 2.2f;
        [SerializeField] private float ballSearchInterval = 0.35f;
        [SerializeField] private float stuckCheckInterval = 0.5f;
        [SerializeField] private float stuckDistanceThreshold = 0.2f;
        [SerializeField] private float maxStuckTime = 1.5f;
        [SerializeField] private float ignoredBallDuration = 2.5f;

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
        [SerializeField] private float dodgeSpeedMultiplier = 1.5f;
        [SerializeField] private float dodgeCooldown = 2f;
        [SerializeField] private float dodgeDuration = 0.4f;
        [SerializeField] private float dodgeTriggerDistance = 15f;
        [SerializeField] [Range(0f, 1f)] private float dodgeTriggerChancePerCheck = 0.02f;

        [Header("Debug")]
        [SerializeField] private bool drawDebug = true;

        private CharacterController characterController;
        private RoundActor actor;

        private AIState currentState = AIState.AcquireBall;
        private Dodgeball heldBall;
        private Dodgeball targetBall;

        private Vector3 planarVelocity;
        private float verticalVelocity;

        private float ballSearchTimer;
        private float repositionRefreshTimer;
        private float aimTimer;
        private float throwCooldownTimer;

        private Vector3 currentMoveTarget;
        private bool hasMoveTarget;

        private Vector3 lastPlayerPosition;
        private Vector3 estimatedPlayerVelocity;

        private Vector3 desiredFacingDirection;
        private bool hasDesiredFacing;

        private float repositionSideSign = 1f;

        private float dodgeCooldownTimer;
        private float dodgeTimer;
        private bool isDodging;
        private Vector3 dodgeDirection;

        private float stuckCheckTimer;
        private float accumulatedStuckTime;
        private Vector3 lastStuckCheckPosition;

        private Dodgeball ignoredBall;
        private float ignoredBallTimer;

        public bool IsHoldingBall => heldBall != null;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            actor = GetComponent<RoundActor>();

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

            lastStuckCheckPosition = transform.position;
        }

        private void Start()
        {
            if (playerActor != null)
            {
                lastPlayerPosition = playerActor.transform.position;
            }
        }

        private void Update()
        {
            if (ignoredBallTimer > 0f)
            {
                ignoredBallTimer -= Time.deltaTime;
                if (ignoredBallTimer <= 0f)
                {
                    ignoredBall = null;
                }
            }
            
            if (actor.IsEliminated)
            {
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

            ApplyMovement();
            ApplyFacing();
            TickStuckDetection();
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

            if (targetBall == null || targetBall.State != Dodgeball.BallState.Idle)
            {
                targetBall = null;
            }

            if (ballSearchTimer <= 0f)
            {
                ballSearchTimer = ballSearchInterval;
                targetBall = FindNearestIdleBall();
            }

            if (targetBall == null)
            {
                StopPlanarMotion();

                if (playerActor != null)
                {
                    SetFacingTarget(playerActor.transform.position);
                }

                return;
            }

            Vector3 ballPosition = targetBall.transform.position;
            SetMoveTarget(ballPosition);

            float distanceToBall = Vector3.Distance(transform.position, ballPosition);
            if (distanceToBall <= pickupRange)
            {
                bool pickedUp = targetBall.TryPickup(holdPoint, actor);
                if (pickedUp)
                {
                    heldBall = targetBall;
                    targetBall = null;
                    ClearMoveTarget();
                    ResetStuckDetection();
                    ChangeState(AIState.Reposition);
                }
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
                StopPlanarMotion();
                return;
            }

            TryDodge();

            repositionRefreshTimer -= Time.deltaTime;

            if (!hasMoveTarget || repositionRefreshTimer <= 0f)
            {
                repositionRefreshTimer = repositionRefreshInterval;
                currentMoveTarget = CalculateRepositionTarget();
                hasMoveTarget = true;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, playerActor.transform.position);
            bool inThrowBand = Mathf.Abs(distanceToPlayer - desiredThrowDistance) <= throwDistanceTolerance;
            bool hasLineOfSight = HasLineOfSightToPlayer();

            if (!isDodging && inThrowBand && hasLineOfSight)
            {
                ClearMoveTarget();
                ResetStuckDetection();
                ChangeState(AIState.Aim);
                return;
            }

            SetMoveTarget(currentMoveTarget);
            SetFacingTarget(playerActor.transform.position);
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

            StopPlanarMotion();

            Vector3 predictedTarget = GetPredictedPlayerPosition();
            SetFacingTarget(predictedTarget);

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

            if (throwCooldownTimer > 0f)
            {
                StopPlanarMotion();
                SetFacingTarget(playerActor.transform.position);
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

        private void ApplyMovement()
        {
            if (isDodging)
            {
                Vector3 dodgeVelocity = dodgeDirection * moveSpeed * dodgeSpeedMultiplier;

                if (characterController.isGrounded && verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }

                verticalVelocity += gravity * Time.deltaTime;
                Vector3 dodgeFrameVelocity = dodgeVelocity + Vector3.up * verticalVelocity;
                characterController.Move(dodgeFrameVelocity * Time.deltaTime);

                if (!hasDesiredFacing && playerActor != null)
                {
                    SetFacingTarget(playerActor.transform.position);
                }

                planarVelocity = Vector3.zero;
                return;
            }

            Vector3 targetPlanarVelocity = Vector3.zero;

            if (hasMoveTarget)
            {
                Vector3 toTarget = currentMoveTarget - transform.position;
                toTarget.y = 0f;

                float distance = toTarget.magnitude;
                if (distance > stoppingDistance)
                {
                    Vector3 moveDirection = toTarget.normalized;
                    targetPlanarVelocity = moveDirection * moveSpeed;

                    if (!hasDesiredFacing)
                    {
                        SetFacingTarget(transform.position + moveDirection);
                    }
                }
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, acceleration * Time.deltaTime);
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 normalFrameVelocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(normalFrameVelocity * Time.deltaTime);
        }

        private void TickStuckDetection()
        {
            bool shouldMonitor = currentState == AIState.AcquireBall || currentState == AIState.Reposition;

            if (!shouldMonitor || !hasMoveTarget || isDodging)
            {
                ResetStuckDetection();
                return;
            }

            stuckCheckTimer -= Time.deltaTime;
            if (stuckCheckTimer > 0f)
            {
                return;
            }

            float movedDistance = Vector3.Distance(transform.position, lastStuckCheckPosition);

            if (movedDistance < stuckDistanceThreshold)
            {
                accumulatedStuckTime += stuckCheckInterval;
            }
            else
            {
                accumulatedStuckTime = 0f;
            }

            lastStuckCheckPosition = transform.position;
            stuckCheckTimer = stuckCheckInterval;

            if (accumulatedStuckTime >= maxStuckTime)
            {
                HandleStuckState();
            }
        }

        private void HandleStuckState()
        {
            accumulatedStuckTime = 0f;
            stuckCheckTimer = stuckCheckInterval;
            lastStuckCheckPosition = transform.position;

            if (currentState == AIState.AcquireBall)
            {
                if (targetBall != null)
                {
                    ignoredBall = targetBall;
                    ignoredBallTimer = ignoredBallDuration;
                }

                targetBall = null;
                ClearMoveTarget();
                ballSearchTimer = 0f;
            }
            else if (currentState == AIState.Reposition)
            {
                repositionSideSign *= -1f;
                repositionRefreshTimer = 0f;
                ClearMoveTarget();
            }
        }

        private void ResetStuckDetection()
        {
            accumulatedStuckTime = 0f;
            stuckCheckTimer = stuckCheckInterval;
            lastStuckCheckPosition = transform.position;
        }

        private Dodgeball FindNearestIdleBall()
        {
            Dodgeball[] balls = FindObjectsByType<Dodgeball>(FindObjectsSortMode.None);
            Dodgeball bestBall = null;
            float bestDistanceSqr = float.MaxValue;

            foreach (Dodgeball ball in balls)
            {
                if (ball == null || ball.State != Dodgeball.BallState.Idle)
                {
                    continue;
                }

                if (ignoredBall != null && ball == ignoredBall)
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

            Vector3 target = desiredBase + strafeOffset;
            target.y = transform.position.y;

            return target;
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

            ClearMoveTarget();
            ResetStuckDetection();
        }

        private Vector3 GetAimPosition()
        {
            return aimOrigin != null ? aimOrigin.position : transform.position + Vector3.up * 1.2f;
        }

        private void SetFacingTarget(Vector3 worldPoint)
        {
            Vector3 direction = worldPoint - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.0001f)
            {
                desiredFacingDirection = direction.normalized;
                hasDesiredFacing = true;
            }
        }

        private void ApplyFacing()
        {
            if (!hasDesiredFacing)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(desiredFacingDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);

            hasDesiredFacing = false;
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

        private void StopPlanarMotion()
        {
            planarVelocity = Vector3.zero;
        }

        private void ChangeState(AIState nextState)
        {
            currentState = nextState;
            ResetStuckDetection();

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