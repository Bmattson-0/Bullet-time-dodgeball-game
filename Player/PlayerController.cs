// v0.1
    // Initial prototype version
// v0.2.1
    // Changes:
    // - Adding Player Dodge Mechanic via TickDodgeTimers() HandleDodgeInput() and updates to Update() and HandleMovement()
    // - For is.Dodging renamed framevelocity to dodgeframevelocity to prevent error
using BulletTimeDodgeball.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BulletTimeDodgeball.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(RoundActor))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform holdPoint;
        [SerializeField] private PlayerResources resources;
        [SerializeField] private BulletTimeController bulletTime;

        [Header("Movement")]
        [SerializeField] private float walkSpeed = 7f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float acceleration = 35f;
        [SerializeField] private float deceleration = 45f;
        [SerializeField] private float airControlMultiplier = 0.55f;
        [SerializeField] private float jumpHeight = 1.35f;
        [SerializeField] private float gravity = -25f;
        [SerializeField] private float lookSensitivity = 0.14f;
        [SerializeField] private float maxLookAngle = 85f;
        [SerializeField] [Range(0.25f, 1f)] private float playerMoveScaleDuringBulletTime = 0.55f;
        [SerializeField] [Range(0.25f, 1f)] private float playerLookScaleDuringBulletTime = 0.65f;

        [Header("Dodge")]
        [SerializeField] private float dodgeSpeed = 14f;
        [SerializeField] private float dodgeDuration = 0.15f;
        [SerializeField] private float dodgeCooldown = 0.45f;
        [SerializeField] private float dodgeStaminaCost = 18f;
        [SerializeField] private float dodgeTapWindow = 0.25f;

        [Header("Costs")]
        [SerializeField] private float sprintStaminaDrainPerSecond = 12f;
        [SerializeField] private float jumpStaminaCost = 12f;
        [SerializeField] private float minThrowStaminaCost = 8f;
        [SerializeField] private float maxThrowStaminaCost = 30f;

        [Header("Ball Interaction")]
        [SerializeField] private float pickupRange = 3f;
        [SerializeField] private float pickupRadius = 0.3f;
        [SerializeField] private float minThrowForce = 18f;
        [SerializeField] private float maxThrowForce = 38f;
        [SerializeField] private float throwChargeTime = 1f;
        [SerializeField] private LayerMask pickupMask = ~0;

        [Header("God Mode")]
        [SerializeField] private Key godModeToggleKey = Key.F3;
        [SerializeField] private float godModeFlySpeed = 10f;
        [SerializeField] private float godModeSprintFlySpeed = 16f;

        private CharacterController characterController;
        private RoundActor actor;
        private Dodgeball heldBall;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float pitch;
        private float throwCharge01;
        private bool isChargingThrow;
        private bool loggedMissingCamera;

        private bool isDodging;
        private float dodgeTimer;
        private float dodgeCooldownTimer;
        private Vector3 dodgeDirection;
        private float lastLeftTapTime = -999f;
        private float lastRightTapTime = -999f;

        public bool IsChargingThrow => isChargingThrow;
        public float ThrowChargeNormalized => throwCharge01;
        public bool IsHoldingBall => heldBall != null;
        public bool IsGodMode => actor != null && actor.GodMode;
        public bool IsDodging => isDodging;
                
        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            actor = GetComponent<RoundActor>();

            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            if (resources == null)
            {
                resources = GetComponent<PlayerResources>();
            }

            if (bulletTime == null)
            {
                bulletTime = GetComponent<BulletTimeController>();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current[godModeToggleKey].wasPressedThisFrame)
            {
                actor.SetGodMode(!actor.GodMode);
                verticalVelocity = 0f;
                planarVelocity = Vector3.zero;
            }

            if (actor.IsEliminated)
            {
                return;
            }

            TickDodgeTimers();
            HandleDodgeInput();
            
            HandleMovement();
            HandleLook();
            HandleBallInteraction();
        }

        private void HandleLook()
        {
            if (playerCamera == null)
            {
                if (!loggedMissingCamera)
                {
                    Debug.LogWarning("PlayerController: No camera assigned/found. Assign a Camera to playerCamera.");
                    loggedMissingCamera = true;
                }

                return;
            }

            Vector2 lookDelta = Vector2.zero;
            if (Mouse.current != null)
            {
                lookDelta = Mouse.current.delta.ReadValue();
            }

            float mouseX = lookDelta.x * lookSensitivity;
            float mouseY = lookDelta.y * lookSensitivity;

            if (bulletTime != null && bulletTime.IsActive)
            {
                mouseX *= playerLookScaleDuringBulletTime;
                mouseY *= playerLookScaleDuringBulletTime;
            }

            pitch = Mathf.Clamp(pitch - mouseY, -maxLookAngle, maxLookAngle);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private void HandleMovement()
        {
            float inputX = 0f;
            float inputZ = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed) inputX -= 1f;
                if (Keyboard.current.dKey.isPressed) inputX += 1f;
                if (Keyboard.current.sKey.isPressed) inputZ -= 1f;
                if (Keyboard.current.wKey.isPressed) inputZ += 1f;
            }

            Vector3 moveInput = (transform.right * inputX + transform.forward * inputZ).normalized;
            bool hasMoveInput = moveInput.sqrMagnitude > 0.001f;
            
            if (isDodging)
            {
                if (characterController.isGrounded && verticalVelocity < 0f)
                {
                    verticalVelocity = -2f;
                }
            
                float finalDodgeSpeed = dodgeSpeed;
                if (bulletTime != null && bulletTime.IsActive)
                {
                    finalDodgeSpeed *= playerMoveScaleDuringBulletTime / Mathf.Max(bulletTime.BulletTimeScale, 0.001f);
                }
            
                verticalVelocity += gravity * Time.deltaTime;
                Vector3 dodgeVelocity = dodgeDirection * finalDodgeSpeed;
                Vector3 dodgeFrameVelocity = dodgeVelocity + Vector3.up * verticalVelocity;
                characterController.Move(dodgeFrameVelocity * Time.deltaTime);
            
                planarVelocity = Vector3.zero;
                return;
            }
            
            if (actor.GodMode)
            {
                HandleGodModeMovement(moveInput, hasMoveInput);
                return;
            }

            bool isSprinting = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed && hasMoveInput;

            if (isSprinting && resources != null)
            {
                bool canSpendSprint = resources.SpendStamina(sprintStaminaDrainPerSecond * Time.unscaledDeltaTime);
                if (!canSpendSprint)
                {
                    isSprinting = false;
                }
            }

            float speed = isSprinting ? sprintSpeed : walkSpeed;
            if (bulletTime != null && bulletTime.IsActive)
            {
                speed *= playerMoveScaleDuringBulletTime / Mathf.Max(bulletTime.BulletTimeScale, 0.001f);
            }

            Vector3 targetPlanarVelocity = moveInput * speed;

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            if (characterController.isGrounded && jumpPressed)
            {
                bool canJump = resources == null || resources.SpendStamina(jumpStaminaCost);
                if (canJump)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                }
            }

            float rate = targetPlanarVelocity.sqrMagnitude > 0.01f ? acceleration : deceleration;
            if (!characterController.isGrounded)
            {
                rate *= airControlMultiplier;
            }

            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, rate * Time.deltaTime);
            verticalVelocity += gravity * Time.deltaTime;
            Vector3 frameVelocity = planarVelocity + Vector3.up * verticalVelocity;
            characterController.Move(frameVelocity * Time.deltaTime);
        }

        private void TickDodgeTimers()
        {
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
        }
        
        private void HandleDodgeInput()
        {
            if (actor == null || actor.IsEliminated || actor.GodMode)
            {
                return;
            }
        
            if (Keyboard.current == null)
            {
                return;
            }
        
            float now = Time.unscaledTime;
        
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                if (now - lastLeftTapTime <= dodgeTapWindow)
                {
                    TryStartDodge(-transform.right);
                }
        
                lastLeftTapTime = now;
            }
        
            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                if (now - lastRightTapTime <= dodgeTapWindow)
                {
                    TryStartDodge(transform.right);
                }
        
                lastRightTapTime = now;
            }
        }

        private void TryStartDodge(Vector3 worldDirection)
        {
            if (isDodging)
            {
                return;
            }
        
            if (dodgeCooldownTimer > 0f)
            {
                return;
            }
        
            if (!characterController.isGrounded)
            {
                return;
            }
        
            if (resources != null && !resources.SpendStamina(dodgeStaminaCost))
            {
                return;
            }
        
            isDodging = true;
            dodgeTimer = dodgeDuration;
            dodgeCooldownTimer = dodgeCooldown;
            dodgeDirection = worldDirection.normalized;
        
            // Cancel charged throw if dodging mid-charge
            isChargingThrow = false;
            throwCharge01 = 0f;
        }
        
        private void HandleGodModeMovement(Vector3 moveInput, bool hasMoveInput)
        {
            float verticalInput = 0f;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.spaceKey.isPressed) verticalInput += 1f;
                if (Keyboard.current.leftCtrlKey.isPressed || Keyboard.current.cKey.isPressed) verticalInput -= 1f;
            }

            bool sprintFly = Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
            float flySpeed = sprintFly ? godModeSprintFlySpeed : godModeFlySpeed;

            Vector3 move = moveInput;
            move += Vector3.up * verticalInput;

            if (move.sqrMagnitude > 1f)
            {
                move.Normalize();
            }

            planarVelocity = Vector3.zero;
            verticalVelocity = 0f;

            characterController.Move(move * flySpeed * Time.deltaTime);
        }

        private void HandleBallInteraction()
        {
            bool pickupPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            if (pickupPressed && heldBall == null)
            {
                TryPickupBall();
            }

            if (heldBall == null)
            {
                isChargingThrow = false;
                throwCharge01 = 0f;

                if (resources != null)
                {
                    resources.SetStaminaRegenSuppressed(false);
                }

                return;
            }

            bool startedCharge = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool releasedThrow = Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
            bool cancelCharge = Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame;

            if (startedCharge)
            {
                isChargingThrow = true;
                throwCharge01 = 0f;
            }

            if (isChargingThrow && Mouse.current != null && Mouse.current.leftButton.isPressed)
            {
                throwCharge01 += Time.unscaledDeltaTime / Mathf.Max(throwChargeTime, 0.01f);
                throwCharge01 = Mathf.Clamp01(throwCharge01);
            }

            if (isChargingThrow && cancelCharge)
            {
                isChargingThrow = false;
                throwCharge01 = 0f;
            }

            if (isChargingThrow && releasedThrow)
            {
                float requestedThrowForce = Mathf.Lerp(minThrowForce, maxThrowForce, throwCharge01);
                float requestedStaminaCost = Mathf.Lerp(minThrowStaminaCost, maxThrowStaminaCost, throwCharge01);

                float availableStamina = resources != null ? resources.CurrentStamina : requestedStaminaCost;
                float staminaFactor = resources != null && requestedStaminaCost > 0f
                    ? Mathf.Clamp01(availableStamina / requestedStaminaCost)
                    : 1f;

                float finalThrowForce = Mathf.Lerp(minThrowForce, requestedThrowForce, staminaFactor);

                if (resources != null)
                {
                    float staminaToSpend = Mathf.Min(requestedStaminaCost, resources.CurrentStamina);
                    resources.SpendStamina(staminaToSpend);
                }

                Transform aimTransform = playerCamera != null ? playerCamera.transform : transform;
                Vector3 throwDirection = aimTransform.forward;
                Vector3 throwVelocity = throwDirection * finalThrowForce;

                bool didThrow = heldBall.Throw(throwVelocity, actor);
                if (didThrow)
                {
                    heldBall = null;
                }

                isChargingThrow = false;
                throwCharge01 = 0f;
            }

            if (resources != null)
            {
                resources.SetStaminaRegenSuppressed(isChargingThrow);
            }
        }

        private void TryPickupBall()
        {
            Transform aimTransform = playerCamera != null ? playerCamera.transform : transform;
            Ray ray = new Ray(aimTransform.position, aimTransform.forward);
            if (!Physics.SphereCast(ray, pickupRadius, out RaycastHit hit, pickupRange, pickupMask, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            Dodgeball ball = hit.collider.GetComponentInParent<Dodgeball>();
            if (ball == null)
            {
                return;
            }

            bool pickedUp = ball.TryPickup(holdPoint, actor);
            if (pickedUp)
            {
                heldBall = ball;
            }
        }
    }
}
