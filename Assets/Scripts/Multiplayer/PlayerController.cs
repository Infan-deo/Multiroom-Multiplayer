using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")] [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 2f;

    [Header("Look")] [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    public bool canRotateCamera;
    public SyncVar<bool> canMove = new();
    

    [Header("Interact")] [SerializeField] private float interactDistance = 5f;

    private CharacterController controller;
    private PlayerInputHandler inputHandler;


    private Vector2 serverMoveInput;
    private bool serverJumpRequested;
    private Vector3 velocity;

    // Local camera pitch.
    private float cameraPitch;
    private float yaw;

    public EventBinding<PauseMenuState> OnPauseMenuEventBinding;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        inputHandler = GetComponent<PlayerInputHandler>();
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
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        yaw = transform.eulerAngles.y;
        canMove.Value = true;

        OnPauseMenuEventBinding = new EventBinding<PauseMenuState>(OnPauseMenu);
        EventBus<PauseMenuState>.Register(OnPauseMenuEventBinding);
    }

    public void OnPauseMenu(PauseMenuState pauseMenuState)
    {
        canRotateCamera = !pauseMenuState.state;
    }

    private void Update()
    {
        if (IsServerStarted)
        {
            UpdateServerMovement();
        }

        if (IsOwner)
        {
            HandleInput();
            HandleLocalCamera();
            HandleJump();
        }
    }


    private void HandleJump()
    {
        if (inputHandler.GetJumpInput())
        {
            SendJumpServerRpc();
            print("jump");
        }
    }

    private void HandleInput()
    {
        if (canMove.Value)
        {
            Vector2 moveInput = inputHandler.MoveInput;
            Vector2 lookInput = inputHandler.GetLookValue();

            // Send movement input to server.
            SendMovementServerRpc(moveInput);

            // Send only horizontal mouse movement to server.
            SendLookServerRpc(lookInput.x);
        }
    }

    private void HandleLocalCamera()
    {
        if (canRotateCamera)
        {
            Vector2 lookInput = inputHandler.GetLookValue();

            cameraPitch -= lookInput.y * lookSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    [ServerRpc]
    private void SendMovementServerRpc(Vector2 input)
    {
        serverMoveInput = input;
    }

    [Server]
    private void UpdateServerMovement()
    {
        if (serverJumpRequested)
        {
            Jump();
            serverJumpRequested = false;
        }

        Vector3 movement =
            transform.right * serverMoveInput.x +
            transform.forward * serverMoveInput.y;

        movement = Vector3.ClampMagnitude(movement, 1f);

        controller.Move(
            movement * moveSpeed * Time.deltaTime
        );

        if (controller.isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;

        controller.Move(
            velocity * Time.deltaTime
        );
    }

    [ServerRpc]
    private void SendLookServerRpc(float mouseX)
    {
        RotatePlayer(mouseX);
    }

    [Server]
    private void RotatePlayer(float mouseX)
    {
        yaw += mouseX * lookSensitivity;

        transform.rotation = Quaternion.Euler(
            0f,
            yaw,
            0f
        );
    }

    [ServerRpc]
    private void SendJumpServerRpc()
    {
        serverJumpRequested = true;
    }

    [Server]
    private void Jump()
    {
        if (!controller.isGrounded)
        {
            return;
        }

        velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
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
}