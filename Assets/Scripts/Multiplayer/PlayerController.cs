using FishNet.Component.Prediction;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using FishNet.Utility.Template;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : TickNetworkBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Look")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    public bool canRotateCamera;

    [Header("Interact")]
    [SerializeField] private float interactDistance = 5f;

    private CharacterController controller;
    private PlayerInputHandler inputHandler;

    public GetPlayerInfo itsOwnInfo;

    private Vector3 velocity;
    private float cameraPitch;
    private float yaw;
    private bool canMove;
    private bool jumpRequested;

    public EventBinding<PauseMenuState> OnPauseMenuEventBinding;

    public struct ReplicateData : IReplicateData
    {
        public Vector2 MoveInput;
        public float LookX;
        public bool Jump;

        private uint _tick;

        public ReplicateData(
            Vector2 moveInput,
            float lookX,
            bool jump)
        {
            MoveInput = moveInput;
            LookX = lookX;
            Jump = jump;
            _tick = 0;
        }

        public void Dispose()
        {
        }

        public uint GetTick() => _tick;

        public void SetTick(uint value)
        {
            _tick = value;
        }
    }

    public struct ReconcileData : IReconcileData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;

        private uint _tick;

        public ReconcileData(
            Vector3 position,
            Quaternion rotation,
            Vector3 velocity)
        {
            Position = position;
            Rotation = rotation;
            Velocity = velocity;
            _tick = 0;
        }

        public void Dispose()
        {
        }

        public uint GetTick() => _tick;

        public void SetTick(uint value)
        {
            _tick = value;
        }
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();

        SetTickCallbacks(
            TickCallback.Tick |
            TickCallback.PostTick
        );
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
        {
            if (cameraTransform != null)
                cameraTransform.gameObject.SetActive(false);

            return;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cameraTransform != null)
            cameraTransform.gameObject.SetActive(true);

        OnPauseMenuEventBinding =
            new EventBinding<PauseMenuState>(OnPauseMenu);

        EventBus<PauseMenuState>.Register(
            OnPauseMenuEventBinding
        );
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        yaw = transform.eulerAngles.y;
        canMove = true;
    }

    [Client]
    public void OnPauseMenu(PauseMenuState pauseMenuState)
    {
        canRotateCamera = !pauseMenuState.state;
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        HandleJump();
        HandleLocalCamera();
    }

    private void HandleJump()
    {
        if (!canMove)
            return;

        if (inputHandler.GetJumpInput())
            jumpRequested = true;
    }

    private void HandleLocalCamera()
    {
        if (!canRotateCamera)
            return;

        Vector2 lookInput = inputHandler.GetLookValue();

        cameraPitch -= lookInput.y * lookSensitivity;

        cameraPitch = Mathf.Clamp(
            cameraPitch,
            minPitch,
            maxPitch
        );
        yaw += lookInput.x * lookSensitivity;

        // if (cameraTransform != null)
        // {
        //     cameraTransform.localRotation =
        //         Quaternion.Euler(
        //             cameraPitch,
        //             yaw,
        //             0f
        //         );
        // }
    }

    protected override void TimeManager_OnTick()
    {
        PerformReplicate(BuildMoveData());
    }

    private ReplicateData BuildMoveData()
    {
        if (!IsOwner)
            return default;

        Vector2 moveInput = Vector2.zero;
        float lookX = 0f;

        if (canMove)
        {
            moveInput = inputHandler.MoveInput;
            lookX = inputHandler.GetLookValue().x;
        }

        bool jump = jumpRequested;

        jumpRequested = false;

        return new ReplicateData(
            moveInput,
            lookX,
            jump
        );
    }

    
    [Replicate]
    private void PerformReplicate(
        ReplicateData data,
        ReplicateState state = ReplicateState.Invalid,
        Channel channel = Channel.Unreliable)
    {
        if (!canMove)
            return;

        float delta = (float)TimeManager.TickDelta;

        Vector2 input = Vector2.ClampMagnitude(data.MoveInput, 1f);

        yaw += data.LookX * lookSensitivity;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);

        Vector3 movement =
            transform.right * input.x +
            transform.forward * input.y;

        movement = Vector3.ClampMagnitude(movement, 1f);

        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        if (data.Jump && controller.isGrounded)
            velocity.y = Mathf.Sqrt(
                jumpHeight * -2f * gravity
            );

        velocity.y += gravity * delta;

        Vector3 finalMovement = movement * moveSpeed;
        finalMovement.y = velocity.y;

        controller.Move(finalMovement * delta);

       
    }

    protected override void TimeManager_OnPostTick()
    {
        if (!canMove)
            return;
        CreateReconcile();
    }

    public override void CreateReconcile()
    {
        ReconcileData data =
            new ReconcileData(
                transform.position,
                transform.rotation,
                velocity
            );

        PerformReconcile(data);
    }

    [Reconcile]
    private void PerformReconcile(
        ReconcileData data,
        Channel channel = Channel.Unreliable)
    {
        if (!canMove)
            return;
        velocity = data.Velocity;

        controller.enabled = false;

        transform.position = data.Position;
        transform.rotation = data.Rotation;

        controller.enabled = true;

        yaw = transform.eulerAngles.y;
    }

    public CharacterController GetCharacterController()
    {
        return controller;
    }
    
    public void SetCanMove(bool state)
    {
        canMove = state;
    }

    public void SetCanRotateCamera(bool state)
    {
        canRotateCamera = state;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (IsOwner)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (OnPauseMenuEventBinding != null)
            {
                EventBus<PauseMenuState>.Deregister(
                    OnPauseMenuEventBinding
                );
            }
        }
    }
}