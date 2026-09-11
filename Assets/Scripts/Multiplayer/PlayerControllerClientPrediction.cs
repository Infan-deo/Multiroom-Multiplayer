using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using GameKit.Dependencies.Utilities;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerControllerClientPrediction : NetworkBehaviour
{
    [Header("Movement")] [SerializeField] private float moveSpeed = 5f;

    [SerializeField] private float jumpHeight = 2f;

    [Header("Ground Check")] [SerializeField] private Vector3 feetOffset;
    [SerializeField] private float feetRadius = 0.3f;
    [SerializeField] private LayerMask groundLayers;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;

    // NOTE: GetLookValue() returns the Look action's raw ReadValue<Vector2>(), which for a
    // standard <Mouse>/delta binding is *pixels moved since last frame* (often 20-100+ per
    // frame), not a normalized -1..1 value. Sensitivities here are tuned for that raw-pixel
    // scale — start low (0.05-0.2) and tune up, rather than reusing "normalized stick" values
    // like 2f, which will blow straight through minPitch/maxPitch in a single frame.
    [SerializeField] private float pitchSensitivity = 0.1f;
    [SerializeField] private float yawSensitivity = 0.1f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    public bool canRotateCamera;
    private bool canMove;

    [Header("Interact")] [SerializeField] private float interactDistance = 5f;

    // All forces must be applied through PredictionRigidbody, never the Rigidbody directly,
    // so that FishNet can correctly resimulate them during reconciliation.
    public PredictionRigidbody PredictionRigidbody;
    private PlayerInputHandler inputHandler;

    public GetPlayerInfo itsOwnInfo;

    // Movement axes: sampled every rendered frame, only the latest value matters (held state).
    private float horizontalInput;
    private float verticalInput;
    private bool jumpQueued;

    // Yaw is a per-frame DELTA, not a held state, so it must be accumulated across every
    // Update() between ticks and reset after the tick consumes it — otherwise frames that
    // land between two ticks get silently dropped.
    private float yawAccumulated;

    // Local camera pitch only — purely cosmetic for the owner, not networked.
    private float cameraPitch;

    public EventBinding<PauseMenuState> OnPauseMenuEventBinding;

    private void Awake()
    {
        inputHandler = GetComponent<PlayerInputHandler>();

        Rigidbody rb = GetComponent<Rigidbody>();

        // Keep the capsule upright; yaw is driven manually in Replicate().
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        PredictionRigidbody = ObjectCaches<PredictionRigidbody>.Retrieve();
        PredictionRigidbody.Initialize(rb);
    }

    private void OnDestroy()
    {
        ObjectCaches<PredictionRigidbody>.StoreAndDefault(ref PredictionRigidbody);
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        TimeManager.OnTick += TimeManagerTickEventHandler;
        TimeManager.OnPostTick += TimeManagerPostTickEventHandler;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        TimeManager.OnTick -= TimeManagerTickEventHandler;
        TimeManager.OnPostTick -= TimeManagerPostTickEventHandler;
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // Only the owner controls this camera.
        if (!IsOwner)
        {
            cameraTransform.gameObject.SetActive(false);
            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraTransform.gameObject.SetActive(true);

        OnPauseMenuEventBinding = new EventBinding<PauseMenuState>(OnPauseMenu);
        EventBus<PauseMenuState>.Register(OnPauseMenuEventBinding);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        canMove = true;
    }

    [Client]
    public void OnPauseMenu(PauseMenuState pauseMenuState)
    {
        canRotateCamera = !pauseMenuState.state;
    }

    private void Update()
    {
        if (!IsOwner) return;

        SampleInput();
        HandleLocalCamera();
    }

    private void SampleInput()
    {
        Vector2 moveInput = inputHandler.MoveInput;
        Vector2 lookInput = inputHandler.GetLookValue();

        horizontalInput = moveInput.x;
        verticalInput = moveInput.y;

        // Accumulate — this frame's delta must not overwrite deltas from earlier frames
        // that haven't been consumed by a tick yet.
        yawAccumulated += lookInput.x;

        // Latch the press so it can't be missed between renders and the next network tick.
        if (inputHandler.GetJumpInput())
        {
            jumpQueued = true;
        }
    }

    private void HandleLocalCamera()
    {
        if (!canRotateCamera)
            return;

        Vector2 lookInput = inputHandler.GetLookValue();

        Vector3 cameraLocalEulerAngles = cameraTransform.localEulerAngles;

        float pitch = cameraLocalEulerAngles.x > 180f
            ? cameraLocalEulerAngles.x - 360f
            : cameraLocalEulerAngles.x;

        pitch = Mathf.Clamp(
            pitch - lookInput.y * pitchSensitivity,
            minPitch,
            maxPitch
        );

        cameraLocalEulerAngles.x =
            pitch < 0f ? pitch + 360f : pitch;

        cameraTransform.localEulerAngles = cameraLocalEulerAngles;
    }

    private bool IsGrounded()
    {
        return Physics.CheckSphere(PredictionRigidbody.Rigidbody.position + feetOffset, feetRadius, groundLayers);
    }

    private void TimeManagerTickEventHandler()
    {
        Replicate(CreateReplicateData());
    }

    private MovementData CreateReplicateData()
    {
        if (!IsOwner) return default;

        bool jump = jumpQueued && IsGrounded();
        jumpQueued = false;

        float horizontal = canMove ? horizontalInput : 0f;
        float vertical = canMove ? verticalInput : 0f;
        float yaw = canMove ? yawAccumulated : 0f;

        // Consumed — start accumulating fresh for the next tick.
        yawAccumulated = 0f;

        return new MovementData(horizontal, vertical, jump, yaw);
    }

    private void TimeManagerPostTickEventHandler()
    {
        // CreateReconcile must be called every tick regardless of client/server/owner —
        // FishNet decides internally whether a reconcile actually needs to be sent.
        CreateReconcile();
    }

    public override void CreateReconcile()
    {
        ReconciliationData reconciliationData = new(PredictionRigidbody);

        Reconcile(reconciliationData);
    }

    [Replicate]
    private void Replicate(
        MovementData movementData,
        ReplicateState replicateState = ReplicateState.Invalid,
        Channel channel = Channel.Unreliable)
    {
        Rigidbody rb = PredictionRigidbody.Rigidbody;

        // =========================
        // YAW
        // =========================

        rb.MoveRotation(
            rb.rotation *
            Quaternion.Euler(
                0f,
                movementData.Yaw * yawSensitivity,
                0f
            )
        );


        // =========================
        // MOVEMENT
        // =========================

        Vector3 right = rb.rotation * Vector3.right;
        Vector3 forward = rb.rotation * Vector3.forward;

        Vector3 moveDirection =
            Vector3.ClampMagnitude(
                right * movementData.Horizontal +
                forward * movementData.Vertical,
                1f
            );

        Vector3 velocity = rb.linearVelocity;

        velocity.x = moveDirection.x * moveSpeed;
        velocity.z = moveDirection.z * moveSpeed;

        PredictionRigidbody.Velocity(velocity);


        // =========================
        // JUMP
        // =========================

        if (movementData.Jump && IsGrounded())
        {
            velocity = rb.linearVelocity;

            velocity.y = Mathf.Sqrt(
                jumpHeight * -2f * Physics.gravity.y
            );

            PredictionRigidbody.Velocity(velocity);
        }


        // =========================
        // SIMULATE
        // =========================

        PredictionRigidbody.Simulate();
    }

    [Reconcile]
    private void Reconcile(ReconciliationData reconciliationData, Channel channel = Channel.Unreliable)
    {
        PredictionRigidbody.Reconcile(reconciliationData.PredictionRigidbody);
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void SetCanMove(bool state)
    {
        canMove = state;
    }

    public void SetCanRotateCamera(bool state)
    {
        canRotateCamera = state;
    }

    private struct MovementData : IReplicateData
    {
        private uint _tick;

        public readonly float Horizontal;
        public readonly float Vertical;
        public readonly bool Jump;
        public readonly float Yaw;

        public MovementData(float horizontal, float vertical, bool jump, float yaw)
        {
            _tick = 0u;
            Horizontal = horizontal;
            Vertical = vertical;
            Jump = jump;
            Yaw = yaw;
        }

        public readonly uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        public void Dispose()
        {
            // Used internally by Fish-Networking.
        }
    }

    private struct ReconciliationData : IReconcileData
    {
        private uint _tick;

        public readonly PredictionRigidbody PredictionRigidbody;

        public ReconciliationData(PredictionRigidbody predictionRigidbody)
        {
            _tick = 0u;
            PredictionRigidbody = predictionRigidbody;
        }

        public readonly uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
        public void Dispose()
        {
            // Used internally by Fish-Networking.
        }
    }
}