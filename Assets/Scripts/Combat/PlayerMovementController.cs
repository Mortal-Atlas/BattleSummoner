using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
    [Header("Movement Attributes")]
    [SerializeField] private float moveSpeed = 5.5f;
    [SerializeField] private float sprintSpeed = 9.5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = 20f;

    [Header("Jump Attributes")]
    [SerializeField] private float jumpForce = 8.5f;
    [SerializeField] private float fallMultiplier = 1.35f;

    [Header("Input Linkage")]
    [SerializeField] private FloatingVirtualStick virtualStick;

    [Header("Dash Parameters")]
    [SerializeField] private float dashSlashDistance = 4.5f;
    [SerializeField] private float dashSlashDuration = 0.16f;
    [SerializeField] private float dashCooldown = 0.35f;

    [Header("Mobility Tuning")]
    [SerializeField] private float acceleration = 48f;
    [SerializeField] private float deceleration = 65f;
    [SerializeField] private float airControl = 0.88f;
    [SerializeField] private float coyoteTime = 0.15f;
    [SerializeField] private float jumpBufferDuration = 0.12f;

    public bool IsSprinting => isSprinting;
    public bool IsDashing => isDashing;
    public bool IsGrounded => controller != null && (controller.isGrounded || coyoteTimer > 0f);

    public static event System.Action OnPlayerJump;
    public static event System.Action<Vector3> OnPlayerDash;

    private CharacterController controller;
    private Vector3 verticalVelocity = Vector3.zero;
    private Vector3 currentPlanarVelocity = Vector3.zero;
    private bool isDashing = false;
    private bool isSprinting = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private Vector3 dashDirection = Vector3.forward;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (virtualStick == null)
        {
            virtualStick = FindAnyObjectByType<FloatingVirtualStick>();
        }
    }

    private void Start()
    {
        BindJumpButton();
        SnapToGroundFloor();
    }

    /// <summary>
    /// Snaps character safely onto the nearest floor collider below to eliminate falling through geometry.
    /// </summary>
    public void SnapToGroundFloor()
    {
        Vector3 rayOrigin = new Vector3(transform.position.x, transform.position.y + 50f, transform.position.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 120f, ~0, QueryTriggerInteraction.Ignore))
        {
            if (controller != null) controller.enabled = false;
            transform.position = hit.point + Vector3.up * 0.05f;
            if (controller != null) controller.enabled = true;
            verticalVelocity = Vector3.zero;
        }
    }

    /// <summary>
    /// Safety recovery if character ever clips through floor into the void.
    /// </summary>
    public void RecoverFromVoidFall()
    {
        Debug.LogWarning("[PlayerMovementController] Player fell below overworld bounds! Teleporting to Citadel Plaza safe ground...");
        if (controller != null) controller.enabled = false;
        transform.position = new Vector3(0f, 18.05f, -10f);
        if (controller != null) controller.enabled = true;
        verticalVelocity = Vector3.zero;
        SnapToGroundFloor();
    }

    public void BindJumpButton()
    {
        var jumpBtn = GameObject.Find("JumpButton");
        if (jumpBtn != null)
        {
            var btn = jumpBtn.GetComponent<UnityEngine.UI.Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(QueueJump);
            }
        }
    }

    private void OnEnable()
    {
        PortraitGestureCanvas.OnBladeSlash += HandleBladeSlash;
        if (WeaponArtGestureController.Instance != null)
        {
            WeaponArtGestureController.Instance.OnDirectionalSlashExecuted += HandleBladeSlash;
        }
        FloatingVirtualStick.OnJoystickFlick += HandleJoystickFlick;
    }

    private void OnDisable()
    {
        PortraitGestureCanvas.OnBladeSlash -= HandleBladeSlash;
        if (WeaponArtGestureController.Instance != null)
        {
            WeaponArtGestureController.Instance.OnDirectionalSlashExecuted -= HandleBladeSlash;
        }
        FloatingVirtualStick.OnJoystickFlick -= HandleJoystickFlick;
    }

    private void Update()
    {
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }

        if (jumpBufferTimer > 0f)
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        // Void fall protection
        if (transform.position.y < -15f)
        {
            RecoverFromVoidFall();
            return;
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            controller.Move(dashDirection * ((dashSlashDistance / dashSlashDuration) * Time.deltaTime));
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
            return;
        }

        // 1. Gather Input
        Vector2 input = Vector2.zero;
        if (virtualStick != null)
        {
            input = virtualStick.MoveInput;
        }

        // Editor fallback: WASD / Arrow keys
        if (input.sqrMagnitude < 0.01f)
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            input = new Vector2(h, v).normalized;
        }

        // 2. Camera-Relative Movement Vectors
        Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
        camForward.y = 0f;
        if (camForward.sqrMagnitude > 0.001f) camForward.Normalize();
        else camForward = Vector3.forward;

        Vector3 camRight = Camera.main != null ? Camera.main.transform.right : Vector3.right;
        camRight.y = 0f;
        if (camRight.sqrMagnitude > 0.001f) camRight.Normalize();
        else camRight = Vector3.right;

        Vector3 desiredDirection = (camRight * input.x + camForward * input.y);
        if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();

        // 3. Grounded & Coyote Time
        if (controller.isGrounded)
        {
            coyoteTimer = coyoteTime;
            if (verticalVelocity.y < 0f)
            {
                verticalVelocity.y = -2f; // Downward stickiness
            }

            // Check buffered jump or immediate jump
            if (jumpBufferTimer > 0f || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J))
            {
                ExecuteJump();
            }
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
            float effectiveGrav = gravity * (verticalVelocity.y < 0f ? fallMultiplier : 1f);
            verticalVelocity.y -= effectiveGrav * Time.deltaTime;

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J))
            {
                if (coyoteTimer > 0f)
                {
                    ExecuteJump();
                }
                else
                {
                    jumpBufferTimer = jumpBufferDuration;
                }
            }
        }

        // 4. Sprint & ADS Evaluation
        bool isADS = WeaponScopeADSController.Instance != null && WeaponScopeADSController.Instance.IsAimingDownSights;
        bool wantSprint = !isADS && ((virtualStick != null && virtualStick.IsAtFarEdge) || Input.GetKey(KeyCode.LeftShift));
        isSprinting = wantSprint && desiredDirection.sqrMagnitude > 0.01f;

        // Editor shortcut for dash
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.E))
        {
            TriggerDash(input.sqrMagnitude > 0.01f ? input : Vector2.up);
        }

        // 5. Target Speed Calculation
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;
        if (InstrumentCatalyst.IsActivelyPlayingMelody) targetSpeed *= 0.80f;
        if (isADS) targetSpeed *= 0.45f;
        if (BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance != null)
        {
            targetSpeed *= BattleSummoner.DungeonDimension.DungeonRunBuffSystem.Instance.GetMoveSpeedMultiplier();
        }

        Vector3 targetPlanarVelocity = desiredDirection * targetSpeed;

        // 6. Smooth Acceleration & Deceleration with Air Control
        float accelRate = desiredDirection.sqrMagnitude > 0.01f ? acceleration : deceleration;
        if (!controller.isGrounded) accelRate *= airControl;

        currentPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, targetPlanarVelocity, accelRate * Time.deltaTime);

        // 7. Responsive Character Rotation
        if (desiredDirection.sqrMagnitude > 0.01f)
        {
            if (isADS)
            {
                float camYaw = Camera.main != null ? Camera.main.transform.eulerAngles.y : transform.eulerAngles.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, camYaw, 0f), Time.deltaTime * 24f);
            }
            else
            {
                float rotSpeed = isSprinting ? rotationSpeed * 1.5f : rotationSpeed;
                Quaternion targetRotation = Quaternion.LookRotation(desiredDirection, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotSpeed);
            }
        }

        // 8. Apply Combined Motion
        controller.Move((currentPlanarVelocity + verticalVelocity) * Time.deltaTime);
    }

    public void Jump()
    {
        QueueJump();
    }

    public void Dash()
    {
        TriggerDash(Vector2.zero);
    }

    public void QueueJump()
    {
        jumpBufferTimer = jumpBufferDuration;
        if (controller != null && (controller.isGrounded || coyoteTimer > 0f))
        {
            ExecuteJump();
        }
    }

    private void ExecuteJump()
    {
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        verticalVelocity.y = jumpForce;
        PlaySFX("sword_whoosh");
        OnPlayerJump?.Invoke();
    }

    private void HandleJoystickFlick(Vector2 flickDirection)
    {
        TriggerDash(flickDirection);
    }

    public void TriggerDash(Vector2 inputDirection)
    {
        if (isDashing || dashCooldownTimer > 0f) return;

        Vector3 camForward = Camera.main != null ? Camera.main.transform.forward : transform.forward;
        Vector3 camRight = Camera.main != null ? Camera.main.transform.right : transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        if (camForward.sqrMagnitude > 0.001f) camForward.Normalize();
        else camForward = transform.forward;
        if (camRight.sqrMagnitude > 0.001f) camRight.Normalize();
        else camRight = transform.right;

        Vector3 targetDash = (camRight * inputDirection.x + camForward * inputDirection.y);
        if (targetDash.sqrMagnitude < 0.01f) targetDash = transform.forward;
        else targetDash.Normalize();

        dashDirection = targetDash;
        transform.rotation = Quaternion.LookRotation(dashDirection, Vector3.up);
        isDashing = true;
        dashTimer = dashSlashDuration;
        dashCooldownTimer = dashCooldown;

        PlaySFX("sword_whoosh");
        OnPlayerDash?.Invoke(dashDirection);
    }

    private void HandleBladeSlash(Vector2 swipeDir, float velocity)
    {
        TriggerDash(swipeDir);
    }

    private void PlaySFX(string presetName)
    {
        if (BattleSummonerAudioMaster.Instance != null)
        {
            BattleSummonerAudioMaster.Instance.PlayPresetSFX(presetName, 0.85f);
        }
    }
}
